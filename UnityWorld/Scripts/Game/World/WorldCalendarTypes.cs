namespace UnityWorld.Game.World;

/// <summary>年内时段；特殊周独立于十二个普通月份。</summary>
public enum WorldPeriodKind { NewYear, Regular, NewYearsEve }

/// <summary>十天干，按甲到癸的顺序排列。</summary>
public enum HeavenlyStem { Jia, Yi, Bing, Ding, Wu, Ji, Geng, Xin, Ren, Gui }

/// <summary>十二地支，按子到亥排列；普通一月从寅开始。</summary>
public enum EarthlyBranch { Zi, Chou, Yin, Mao, Chen, Si, Wu, Wei, Shen, You, Xu, Hai }

/// <summary>普通月四周依次对应的月相。</summary>
public enum MoonPhase { NewMoon, FirstQuarter, FullMoon, LastQuarter }

/// <summary>从立春开始的二十四节气，每项覆盖两个普通周。</summary>
public enum SolarTerm
{
    StartOfSpring, RainWater, AwakeningOfInsects, SpringEquinox, PureBrightness, GrainRain,
    StartOfSummer, GrainFull, GrainInEar, SummerSolstice, MinorHeat, MajorHeat,
    StartOfAutumn, EndOfHeat, WhiteDew, AutumnEquinox, ColdDew, FrostDescent,
    StartOfWinter, MinorSnow, MajorSnow, WinterSolstice, MinorCold, MajorCold
}

/// <summary>历法名称表；不把中文界面文本当作可变时间数据保存。</summary>
public static class WorldCalendarNames
{
    private static readonly string[] StemNames = ["甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸"];
    private static readonly string[] BranchNames = ["子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"];
    private static readonly string[] MoonNames = ["新月", "上弦", "满月", "下弦"];
    private static readonly string[] SolarTermNames =
    [
        "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满", "芒种", "夏至", "小暑", "大暑",
        "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至", "小寒", "大寒"
    ];

    public static string GetDisplayName(this HeavenlyStem value) => Lookup(StemNames, (int)value);
    public static string GetDisplayName(this EarthlyBranch value) => Lookup(BranchNames, (int)value);
    public static string GetDisplayName(this MoonPhase value) => Lookup(MoonNames, (int)value);
    public static string GetDisplayName(this SolarTerm value) => Lookup(SolarTermNames, (int)value);

    private static string Lookup(string[] names, int index)
    {
        if ((uint)index >= (uint)names.Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "未知的历法标识。");
        return names[index];
    }
}
