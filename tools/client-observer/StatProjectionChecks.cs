using System.Globalization;
using System.Reflection;
using NCalc;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.World;

namespace Xianxia.Client.Battle;

/// <summary>新进程属性验证入口；反射只读审计内部状态，不修改正式数据或模拟状态。</summary>
public static class StatProjectionChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo StatsField = RequiredField(typeof(StatBlock), "_stats", PrivateInstance);
    private static readonly FieldInfo ExpressionsField = RequiredField(typeof(StatBlock), "_exprCache", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo DirtyField = RequiredField(typeof(StatEntry), "_isDirty", PrivateInstance);
    private static readonly FieldInfo OwnerField = RequiredField(typeof(StatEntry), "_ownerBlock", PrivateInstance);

    public static void Run(string dataRoot)
    {
        if (WorldMgr._domains.Count != 0 || WorldMgr._gameplays.Count != 0)
            throw new InvalidOperationException("只读属性验证必须在独立新进程内运行。");

        var assertions = 0;
        var scenarios = 0;
        var repeatedReads = 0;
        try
        {
            WorldMgr.Initialize(47031, dataRoot);

            void Check(bool condition, string message)
            {
                assertions++;
                if (!condition) throw new InvalidOperationException("STAT_PROJECTION_FAILED " + message);
            }

            void Equal(float expected, float actual, string message) =>
                Check(BitConverter.SingleToInt32Bits(expected) == BitConverter.SingleToInt32Bits(actual),
                    $"{message}: expected={expected:R}, actual={actual:R}");

            // 两份状态从同一操作序列构造，不能用 Snapshot 重建 clean/dirty 状态。
            void Pair(string label, string statId, Action<StatBlock> setup, float? explicitExpected = null,
                float fallback = 0f, bool initialize = true)
            {
                scenarios++;
                var observed = CreateBlock(initialize);
                var normal = CreateBlock(initialize);
                setup(observed);
                setup(normal);
                var expected = normal.Get(statId, fallback);
                if (explicitExpected.HasValue) Equal(explicitExpected.Value, expected, label + " fixture");
                var before = Capture(observed, normal);
                for (var repeat = 0; repeat < 64; repeat++)
                {
                    var actual = observed.GetReadOnly(statId, fallback);
                    repeatedReads++;
                    Equal(expected, actual, label + " readonly");
                    Check((int)actual == (int)expected, label + " integer truncation");
                }
                AssertUnchanged(before, Capture(observed, normal), label, Check);
                Equal(expected, observed.Get(statId, fallback), label + " subsequent normal Get");
                var clean = Capture(observed, normal);
                Equal(expected, observed.GetReadOnly(statId, fallback), label + " clean reread");
                AssertUnchanged(clean, Capture(observed, normal), label + " clean", Check);
            }

            Pair("missing definition", "__absent_projection_stat__", _ => { }, -123.75f, -123.75f);
            Pair("missing entry keeps lazy default", "HpMax", _ => { }, 0f, initialize: false);
            Pair("derived default without entry", "ManaDrawCost", _ => { }, 5f, initialize: false);
            Pair("primary initialized clean", "QiXue", _ => { }, 10f);
            Pair("formula initialized dirty", "HpMax", _ => { }, 20f);
            Pair("mana maximum", "MpMax", b => b.SetBase("QiGan", 12.5f), 37.5f);
            Pair("SP maximum differs from 25 cells", "SpMax", b => b.SetBase("ShenShi", 17f), 17f);
            Pair("formula fractional result", "HpMax", b => b.SetBase("QiXue", 12.375f), 24.75f);
            Pair("valid cache remains valid after source change", "HpMax", b =>
            {
                b.Get("HpMax");
                b.SetBase("QiXue", 40f);
            }, 20f);
            Pair("dirty dependency retains existing final cache", "HpMax", b =>
                b.GetEntry("QiXue").Add(30f), 20f);
            Pair("explicit dirty formula uses current dependency cache", "HpMax", b =>
            {
                b.Get("HpMax");
                b.SetBase("QiXue", 40f);
                b.GetEntry("HpMax").Add(0f);
            }, 80f);
            Pair("direct dirty modifier addition", "HpMax", b =>
                b.GetEntry("HpMax").AddModifier(StatModifier.Flat(5.5f, "test")), 25.5f);
            Pair("block modifier addition writes clean result", "HpMax", b =>
                b.AddModifier("HpMax", StatModifier.Flat(5.5f, "test")), 25.5f);
            Pair("modifier removal dirties cached result", "HpMax", b =>
            {
                b.AddModifier("HpMax", StatModifier.Flat(9f, "temporary"));
                b.RemoveModifiersBySource("temporary");
            }, 20f);
            Pair("modifier removal picks up changed dependency", "HpMax", b =>
            {
                b.AddModifier("HpMax", StatModifier.Flat(9f, "temporary"));
                b.SetBase("QiXue", 13f);
                b.RemoveModifiersBySource("temporary");
            }, 26f);
            Pair("flat percent additive ordering", "HpMax", b =>
            {
                var entry = b.GetEntry("HpMax");
                entry.AddModifier(StatModifier.Flat(2f));
                entry.AddModifier(StatModifier.Percent(.25f));
                entry.SetAdd(3f);
            }, 30.5f);
            Pair("last override wins then modifier clamp", "HpMax", b =>
            {
                var entry = b.GetEntry("HpMax");
                entry.AddModifier(StatModifier.Flat(500f));
                entry.AddModifier(StatModifier.Override(33.75f));
                entry.AddModifier(StatModifier.Override(40.75f));
                entry.AddModifier(StatModifier.ClampMax(35.25f));
                entry.SetAdd(100f);
            }, 35.25f);
            Pair("modifier minimum", "HpMax", b =>
                b.GetEntry("HpMax").AddModifier(StatModifier.ClampMin(30.75f)), 30.75f);
            Pair("modifier conflicting bounds preserve order", "HpMax", b =>
            {
                b.GetEntry("HpMax").AddModifier(StatModifier.ClampMin(40.5f));
                b.GetEntry("HpMax").AddModifier(StatModifier.ClampMax(30.25f));
            }, 40.5f);
            Pair("definition maximum after modifier minimum", "HpMax", b =>
                b.GetEntry("HpMax").AddModifier(StatModifier.ClampMin(10000.75f)), 9999f);
            Pair("definition minimum after override", "HpMax", b =>
                b.GetEntry("HpMax").AddModifier(StatModifier.Override(-12.75f)), 0f);
            Pair("definition lower clamp before integer cast", "ManaDrawCD", b =>
                b.GetOrCreateEntry("ManaDrawCD").SetAdd(-99f), .1f);
            Pair("definition upper clamp", "ManaDrawCD", b =>
                b.GetOrCreateEntry("ManaDrawCD").SetAdd(99f), 10f);
            Pair("zero maximum is a real result", "HpMax", b =>
                b.GetEntry("HpMax").AddModifier(StatModifier.Override(0f)), 0f);

            scenarios++;
            var first = CreateBlock();
            var second = CreateBlock();
            first.SetBase("QiXue", 11.25f);
            second.SetBase("QiXue", 43.5f);
            // 正常查询先将共享表达式 Parameters 指向另一个角色，形成交替污染的回归条件。
            second.Get("HpMax");
            second.GetEntry("HpMax").Add(0f);
            var alternatingBefore = Capture(first, second);
            for (var repeat = 0; repeat < 256; repeat++)
            {
                Equal(22.5f, first.GetReadOnly("HpMax"), "alternating first");
                Equal(87f, second.GetReadOnly("HpMax"), "alternating second");
                repeatedReads += 2;
            }
            AssertUnchanged(alternatingBefore, Capture(first, second), "alternating", Check);
            Equal(22.5f, first.Get("HpMax"), "alternating first subsequent Get");
            Equal(87f, second.Get("HpMax"), "alternating second subsequent Get");

            scenarios++;
            var invalid = new StatBlock();
            invalid.GetOrCreateEntry("HpMax"); // 公式参数不存在：错误不可冒充零值。
            var failureBefore = Capture(invalid);
            var readOnlyError = CaptureError(() => invalid.GetReadOnly("HpMax"));
            Check(readOnlyError != null, "missing formula parameter should fail");
            AssertUnchanged(failureBefore, Capture(invalid), "failed formula evaluation", Check);
            var normalError = CaptureError(() => invalid.Get("HpMax"));
            Check(readOnlyError?.GetType() == normalError?.GetType(), "formula error type matches normal Get");

            Console.WriteLine($"STAT_PROJECTION_OK scenarios={scenarios} assertions={assertions} repeatedReads={repeatedReads} sourceState=unchanged sharedParameters=unchanged");
        }
        finally
        {
            WorldMgr.End();
        }
    }

    private static StatBlock CreateBlock(bool initialize = true)
    {
        var block = new StatBlock();
        if (initialize) block.InitType("Npc");
        return block;
    }

    private static Exception? CaptureError(Action read)
    {
        try { read(); return null; }
        catch (Exception error) { return error; }
    }

    private sealed record AuditItem(string Path, object? Identity, string Value);

    // 包括容器/对象身份和原始标量；只读反射不调用属性求值，也不重建任何状态。
    private static List<AuditItem> Capture(params StatBlock[] blocks)
    {
        var result = new List<AuditItem>();
        void Add(string path, object? identity, string value = "") => result.Add(new(path, identity, value));
        void Dictionary<T>(string path, object identity, IEnumerable<KeyValuePair<string, T>> values)
        {
            Add(path, identity);
            foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal))
                Add(path + "/" + pair.Key, pair.Value, Scalar(pair.Value));
        }
        for (var i = 0; i < blocks.Length; i++)
        {
            var block = blocks[i];
            var path = "block/" + i;
            Add(path, block);
            var entries = (Dictionary<string, StatEntry>)StatsField.GetValue(block)!;
            Add(path + "/entries", entries);
            foreach (var pair in entries.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var entry = pair.Value;
                var entryPath = path + "/entry/" + pair.Key;
                Add(entryPath, entry, entry.StatId + "|" + Scalar(entry.AddValue) + "|" + DirtyField.GetValue(entry));
                Add(entryPath + "/owner", OwnerField.GetValue(entry));
                var modifiers = entry.GetModifiers();
                Add(entryPath + "/modifiers", modifiers, string.Join(";", modifiers.Select(m =>
                    $"{(int)m.Type}:{Scalar(m.Value)}:{m.SourceId.Length}:{m.SourceId}")));
            }
            Dictionary(path + "/final", block.GetFinalCache(), block.GetFinalCache());
        }
        var expressions = (Dictionary<string, Expression>)ExpressionsField.GetValue(null)!;
        Add("expressions", expressions);
        foreach (var pair in expressions.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            Add("expression/" + pair.Key, pair.Value);
            Dictionary("expression/" + pair.Key + "/parameters", pair.Value.Parameters, pair.Value.Parameters);
        }
        return result;
    }

    private static void AssertUnchanged(IReadOnlyList<AuditItem> before, IReadOnlyList<AuditItem> after,
        string label, Action<bool, string> check)
    {
        check(before.Count == after.Count, label + " audit item count");
        for (var i = 0; i < before.Count; i++)
            check(before[i].Path == after[i].Path && ReferenceEquals(before[i].Identity, after[i].Identity) &&
                  before[i].Value == after[i].Value, label + " mutation at " + before[i].Path);
    }

    private static string Scalar(object? value) => value switch
    {
        float number => "float:" + BitConverter.SingleToInt32Bits(number),
        double number => "double:" + BitConverter.DoubleToInt64Bits(number),
        IFormattable formatted => value.GetType().FullName + ":" + formatted.ToString(null, CultureInfo.InvariantCulture),
        null => "null",
        _ => value.GetType().FullName + ":" + value
    };

    private static FieldInfo RequiredField(Type type, string name, BindingFlags flags) =>
        type.GetField(name, flags) ?? throw new InvalidOperationException($"属性审计字段已变化：{type.Name}.{name}");
}
