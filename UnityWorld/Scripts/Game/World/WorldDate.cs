using System.Globalization;

namespace UnityWorld.Game.World;

/// <summary>
/// 不可变世界日期。只保存从 4000 年新年开始的已过周数，其他字段均为只读投影。
/// </summary>
public sealed record WorldDate
{
    public const long StartYear = 4000;
    public const int WeeksPerYear = 50;
    public const int WeeksPerMonth = 4;

    public long ElapsedWeeks { get; }

    public WorldDate(long elapsedWeeks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedWeeks);
        ElapsedWeeks = elapsedWeeks;
    }

    // 先除后加，long 范围内的周数也能安全得到年份。
    public long Year => checked(StartYear + ElapsedWeeks / WeeksPerYear);

    /// <summary>年内第 1 至 50 周，包含新年和除夕；不同于月内周数。</summary>
    public int WeekOfYear => (int)(ElapsedWeeks % WeeksPerYear) + 1;

    public WorldPeriodKind PeriodKind => WeekOfYear switch
    {
        1 => WorldPeriodKind.NewYear,
        WeeksPerYear => WorldPeriodKind.NewYearsEve,
        _ => WorldPeriodKind.Regular
    };

    /// <summary>按现实公元纪年的天干锚点推算：公元 4 年为甲。</summary>
    public HeavenlyStem YearStem => (HeavenlyStem)((Year - 4) % 10);

    // 普通周在本年中的零基位置；只能在 Regular 时使用，不计两个特殊周。
    private int RegularWeekIndex => WeekOfYear - 2;

    public int? Month => PeriodKind == WorldPeriodKind.Regular ? RegularWeekIndex / WeeksPerMonth + 1 : null;

    /// <summary>普通月内第 1 至 4 周；特殊周为空，不使用 0 或额外月份占位。</summary>
    public int? WeekOfMonth => PeriodKind == WorldPeriodKind.Regular ? RegularWeekIndex % WeeksPerMonth + 1 : null;

    public EarthlyBranch? MonthBranch => Month is int month ? (EarthlyBranch)((month + 1) % 12) : null;

    public MoonPhase? MoonPhase => WeekOfMonth is int week ? (MoonPhase)(week - 1) : null;

    /// <summary>普通月前两周、后两周各一个节气；两个特殊周没有当前节气。</summary>
    public SolarTerm? SolarTerm => PeriodKind == WorldPeriodKind.Regular ? (SolarTerm)(RegularWeekIndex / 2) : null;

    public override string ToString()
    {
        string year = Year.ToString(CultureInfo.InvariantCulture) + "（" + YearStem.GetDisplayName() + "）年 ";
        return PeriodKind switch
        {
            WorldPeriodKind.NewYear => year + "新年",
            WorldPeriodKind.NewYearsEve => year + "除夕",
            _ => year + Month.Value.ToString(CultureInfo.InvariantCulture) + "（" + MonthBranch.Value.GetDisplayName()
                + "）月 " + MoonPhase.Value.GetDisplayName()
        };
    }
}
