using System.Globalization;
using UnityWorld.Game.Data;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

/// <summary>独立历法契约测试：无需加载世界、Lua 或地图即可执行。</summary>
public static class WorldCalendarTests
{
    private static int _assertions;

    private static void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException("世界周历断言失败：" + message);
    }

    private static void Reject<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { _assertions++; return; }
        throw new InvalidOperationException("应该拒绝：" + message);
    }

    public static void Run()
    {
        _assertions = 0;
        Check(WorldMgr._domains.Count == 0 && WorldMgr._gameplays.Count == 0 && GameDataMgr.Instance == null,
            "独立入口没有启动世界、Lua 或地图加载");
        DateBoundaries();
        StemsBranchesAndMoons();
        TextAndReadOnlyQueries();
        SolarTermCoverage();
        AdvanceAndRanges();
        NotificationSequence();
        ObserverFailuresAndReentry();
        LegacyClockIsolation();
        InitializationAndSubscriptions();
        Check(WorldMgr._domains.Count == 0 && WorldMgr._gameplays.Count == 0 && GameDataMgr.Instance == null,
            "测试结束仍未加载世界数据或地图");
        Console.WriteLine($"WORLD_CALENDAR_OK assertions={_assertions}");
    }

    private static void DateBoundaries()
    {
        // 独立列出年界预期，避免用生产代码同一套换算公式生成答案。
        (long weeks, long year, int yearWeek, int? month, int? monthWeek, WorldPeriodKind kind)[] cases =
        [
            (0, 4000, 1, null, null, WorldPeriodKind.NewYear),
            (1, 4000, 2, 1, 1, WorldPeriodKind.Regular),
            (4, 4000, 5, 1, 4, WorldPeriodKind.Regular),
            (5, 4000, 6, 2, 1, WorldPeriodKind.Regular),
            (48, 4000, 49, 12, 4, WorldPeriodKind.Regular),
            (49, 4000, 50, null, null, WorldPeriodKind.NewYearsEve),
            (50, 4001, 1, null, null, WorldPeriodKind.NewYear),
            (51, 4001, 2, 1, 1, WorldPeriodKind.Regular)
        ];
        foreach (var c in cases)
        {
            var date = new WorldDate(c.weeks);
            Check(date.ElapsedWeeks == c.weeks && date.Year == c.year && date.WeekOfYear == c.yearWeek
                && date.Month == c.month && date.WeekOfMonth == c.monthWeek && date.PeriodKind == c.kind,
                $"已过 {c.weeks} 周的年月及年内/月内周位");
            if (c.kind != WorldPeriodKind.Regular)
                Check(date.MonthBranch == null && date.MoonPhase == null && date.SolarTerm == null,
                    "特殊周没有月份地支、月相或当前节气");
        }
        Reject<ArgumentOutOfRangeException>(() => new WorldDate(-1), "负数周数");
        var max = new WorldDate(long.MaxValue);
        Check(max.Year == 184467440737099516 && max.WeekOfYear == 8 && max.Month == 2 && max.WeekOfMonth == 3,
            "long 上界仍能投影日期，不溢出或变为负数");
    }

    private static void StemsBranchesAndMoons()
    {
        (long weeks, string stem)[] anchors = [(0, "庚"), (50, "辛"), (150, "癸"), (200, "甲"), (500, "庚")];
        foreach (var anchor in anchors)
            Check(new WorldDate(anchor.weeks).YearStem.GetDisplayName() == anchor.stem, $"天干锚点 {anchor.weeks}");

        string[] branches = ["寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥", "子", "丑"];
        string[] moons = ["新月", "上弦", "满月", "下弦"];
        int ordinal = 1;
        foreach (string branch in branches)
        {
            foreach (string moon in moons)
            {
                var date = new WorldDate(ordinal++);
                Check(date.MonthBranch.Value.GetDisplayName() == branch && date.MoonPhase.Value.GetDisplayName() == moon,
                    $"{branch}月的{moon}");
            }
        }
    }

    private static void TextAndReadOnlyQueries()
    {
        (long weeks, string text)[] cases =
        [
            (0, "4000（庚）年 新年"), (1, "4000（庚）年 1（寅）月 新月"),
            (3, "4000（庚）年 1（寅）月 满月"), (5, "4000（庚）年 2（卯）月 新月"),
            (48, "4000（庚）年 12（丑）月 下弦"), (49, "4000（庚）年 除夕"), (50, "4001（辛）年 新年")
        ];
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            foreach (var c in cases)
            {
                var date = new WorldDate(c.weeks);
                for (int batch = 0; batch < 7; batch++)
                {
                    Check(date.ToString() == c.text && date.ElapsedWeeks == c.weeks,
                        $"格式与查询不推进时间：{c.text}（批次 {batch}）");
                }
            }
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    private static void SolarTermCoverage()
    {
        string[] expected =
        [
            "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满", "芒种", "夏至", "小暑", "大暑",
            "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至", "小寒", "大寒"
        ];
        var observed = Enumerable.Range(1, 48).Select(w => new WorldDate(w).SolarTerm.Value.GetDisplayName()).ToArray();
        Check(observed.SequenceEqual(expected.SelectMany(name => new[] { name, name })), "全部节气按顺序各占两周");
        Check(observed.Distinct().Count() == 24, "一年普通周恰好覆盖二十四种节气");
        Check(new WorldDate(0).SolarTerm == null && new WorldDate(49).SolarTerm == null
            && new WorldDate(50).SolarTerm == null && new WorldDate(51).SolarTerm == SolarTerm.StartOfSpring,
            "特殊周为空，下一年从立春重新开始");
    }

    private static void AdvanceAndRanges()
    {
        var calendar = new WorldCalendar();
        var initial = calendar.CurrentDate;
        var first = calendar.AdvanceWeek();
        Check(first.Before == initial && first.After == calendar.CurrentDate && calendar.ElapsedWeeks == 1,
            "单次推进返回精确前后日期");
        Check(initial.ToString() == "4000（庚）年 新年" && first.After.ToString() == "4000（庚）年 1（寅）月 新月",
            "旧快照不会被推进改写");
        for (int batch = 0; batch < 30; batch++)
        {
            _ = calendar.CurrentDate.ToString();
            _ = calendar.CurrentDate.SolarTerm;
        }
        Check(calendar.ElapsedWeeks == 1, "只读批次不会自行推进周数");
        Reject<ArgumentOutOfRangeException>(() => new WorldCalendar(-1), "负数初始周数");

        var maximum = new WorldCalendar(long.MaxValue);
        var maxDate = maximum.CurrentDate;
        int notifications = 0;
        maximum.WeekAdvanced += _ => notifications++;
        maximum.SolarTermChanged += _ => notifications++;
        Reject<OverflowException>(() => maximum.AdvanceWeek(), "最大周数继续推进");
        Check(ReferenceEquals(maxDate, maximum.CurrentDate) && notifications == 0, "溢出不部分提交，不发成功通知");
    }

    private static void NotificationSequence()
    {
        var calendar = new WorldCalendar(1);
        var sequence = new List<string>();
        calendar.WeekAdvanced += change =>
        {
            Check(calendar.CurrentDate == change.After, "观察者读取已提交的新日期");
            sequence.Add("week:" + change.After.ElapsedWeeks);
        };
        calendar.SolarTermChanged += change => sequence.Add("term:" + change.After.SolarTerm.Value.GetDisplayName());
        var sameTerm = calendar.AdvanceWeek();
        var nextTerm = calendar.AdvanceWeek();
        Check(!sameTerm.SolarTermChanged && nextTerm.SolarTermChanged, "立春第二周不重复切换，第三周进入雨水");
        Check(sequence.SequenceEqual(new[] { "week:2", "week:3", "term:雨水" }), "周通知先于节气通知且次数准确");

        var yearEnd = new WorldCalendar(48);
        var changes = new List<(SolarTerm? before, SolarTerm? after)>();
        int weekCount = 0;
        yearEnd.WeekAdvanced += _ => weekCount++;
        yearEnd.SolarTermChanged += c => changes.Add((c.Before.SolarTerm, c.After.SolarTerm));
        yearEnd.AdvanceWeek(); yearEnd.AdvanceWeek(); yearEnd.AdvanceWeek();
        Check(weekCount == 3 && changes.SequenceEqual(new (SolarTerm?, SolarTerm?)[]
            { (SolarTerm.MajorCold, null), (null, SolarTerm.StartOfSpring) }), "大寒到空、空到空、空到立春的年界通知");

        var wholeYear = new WorldCalendar();
        var entries = new List<string>();
        wholeYear.SolarTermChanged += c =>
        {
            if (c.After.SolarTerm is SolarTerm term) entries.Add(term.GetDisplayName());
        };
        for (int i = 0; i < 50; i++) wholeYear.AdvanceWeek();
        Check(entries.Count == 24 && entries.First() == "立春" && entries.Last() == "大寒"
            && entries.Distinct().Count() == 24, "整年二十四节气各进入一次");
    }

    private static void ObserverFailuresAndReentry()
    {
        var calendar = new WorldCalendar();
        var sequence = new List<string>();
        using var diagnostics = new StringWriter();
        var originalError = Console.Error;
        try
        {
            Console.SetError(diagnostics);
            calendar.WeekAdvanced += _ =>
            {
                Reject<InvalidOperationException>(() => calendar.AdvanceWeek(), "周通知重入");
                throw new InvalidDataException("expected-week-observer");
            };
            calendar.WeekAdvanced += _ => sequence.Add("week:survived");
            calendar.SolarTermChanged += _ =>
            {
                Reject<InvalidOperationException>(() => calendar.AdvanceWeek(), "节气通知重入");
                throw new InvalidDataException("expected-term-observer");
            };
            calendar.SolarTermChanged += _ => sequence.Add("term:survived");
            var result = calendar.AdvanceWeek();
            Check(result.After.ElapsedWeeks == 1 && calendar.ElapsedWeeks == 1, "观察者异常仍成功返回，重入未多推进");
            Check(sequence.SequenceEqual(new[] { "week:survived", "term:survived" }), "异常不阻止后续两种通知");
            Check(diagnostics.ToString().Contains("expected-week-observer")
                && diagnostics.ToString().Contains("expected-term-observer"), "观察者异常被记录");
        }
        finally { Console.SetError(originalError); }
    }

    private static void LegacyClockIsolation()
    {
        WorldTime.Initialize(731);
        var calendar = WorldTime.Calendar;
        int weekNotifications = 0;
        calendar.WeekAdvanced += _ => weekNotifications++;
        WorldTime.Advance(0.25f);
        Check(WorldTime.CurrentTick == 1 && WorldTime.TotalTime == .25f && WorldTime.DeltaTime == .25f
            && WorldTime.CurrentDate.ElapsedWeeks == 0 && weekNotifications == 0, "旧秒接口不推进或通知周历");

        // 同时监测两类管理器，证明新增入口没有暗中调用整个世界的 Tick。
        var probe = new TickProbe();
        WorldMgr._domains.Add(probe);
        WorldMgr._gameplays.Add(probe);
        try
        {
            var result = WorldTime.AdvanceWeek();
            Check(result.After == WorldTime.CurrentDate && calendar.ElapsedWeeks == 1 && weekNotifications == 1,
                "WorldTime 周门面使用同一周历和通知");
            Check(probe.Ticks == 0 && WorldTime.CurrentTick == 1 && WorldTime.TotalTime == .25f
                && WorldTime.DeltaTime == .25f && WorldTime.RandomSeed == 731, "周推进不遍历管理器、不改旧秒数或种子");
        }
        finally
        {
            WorldMgr._domains.Remove(probe);
            WorldMgr._gameplays.Remove(probe);
        }
    }

    private static void InitializationAndSubscriptions()
    {
        var oldCalendar = WorldTime.Calendar;
        int oldEvents = 0;
        oldCalendar.WeekAdvanced += _ => oldEvents++;
        WorldTime.AdvanceWeek();
        Check(oldEvents == 1 && oldCalendar.ElapsedWeeks == 2, "旧世界订阅正常接收周变化");
        WorldTime.Initialize(912);
        Check(!ReferenceEquals(oldCalendar, WorldTime.Calendar) && WorldTime.CurrentDate.ElapsedWeeks == 0
            && WorldTime.CurrentDate.ToString() == "4000（庚）年 新年" && oldEvents == 1,
            "初始化重置周历、替换实例且不发推进通知");
        Check(WorldTime.CurrentTick == 0 && WorldTime.TotalTime == 0 && WorldTime.DeltaTime == 0
            && WorldTime.RandomSeed == 912, "旧秒初始化和随机种子仍有效");
        WorldTime.AdvanceWeek();
        Check(oldEvents == 1 && oldCalendar.ElapsedWeeks == 2 && WorldTime.Calendar.ElapsedWeeks == 1,
            "新世界不会触发旧订阅或修改旧日期");
    }

    private sealed class TickProbe : IDomainMgrBase, IGameplayMgrBase
    {
        public int Ticks { get; private set; }
        public string Name => "CalendarIsolationProbe";
        public string Desc => "只用于检测意外的世界 Tick";
        public void Tick(float deltaTime) => Ticks++;
        public void Init() { }
        public void Begin() { }
        public void End() { }
        public void Update() { }
        public void Render(float dt) { }
        public void Log() { }
        public System.Collections.IEnumerator Save() { yield break; }
        public System.Collections.IEnumerator Load() { yield break; }
    }
}
