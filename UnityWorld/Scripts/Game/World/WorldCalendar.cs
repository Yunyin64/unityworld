using UnityWorld.Core;

namespace UnityWorld.Game.World;

/// <summary>一次已提交周推进的不可变结果，空节气也参与变化判断。</summary>
public sealed record WorldCalendarTransition
{
    public WorldDate Before { get; }
    public WorldDate After { get; }
    public bool SolarTermChanged => Before.SolarTerm != After.SolarTerm;

    internal WorldCalendarTransition(WorldDate before, WorldDate after)
    {
        Before = before;
        After = after;
    }
}

/// <summary>
/// 单线程世界周历。显式提交一周只修改日期，不执行 NPC、元气、故事或其他世界结算。
/// </summary>
public sealed class WorldCalendar
{
    private bool _isAdvancing;

    // 日期内部只保存已过周数；不另存可与它发生分歧的年、月或累计秒数。
    public WorldDate CurrentDate { get; private set; }
    public long ElapsedWeeks => CurrentDate.ElapsedWeeks;

    /// <summary>日期提交后发送；观察者读取的新日期已经是 After。</summary>
    public event Action<WorldCalendarTransition> WeekAdvanced;

    /// <summary>在周通知之后发送；从节气进入特殊周也属于一次变化。</summary>
    public event Action<WorldCalendarTransition> SolarTermChanged;

    public WorldCalendar(long elapsedWeeks = 0)
    {
        CurrentDate = new WorldDate(elapsedWeeks);
    }

    /// <summary>
    /// 明确推进一周。范围检查在提交之前完成；通知异常不将成功推进伪报为失败。
    /// </summary>
    public WorldCalendarTransition AdvanceWeek()
    {
        if (_isAdvancing)
            throw new InvalidOperationException("周历通知期间不能重入推进时间。");

        _isAdvancing = true;
        try
        {
            var before = CurrentDate;
            var after = new WorldDate(checked(before.ElapsedWeeks + 1));
            var transition = new WorldCalendarTransition(before, after);

            // 本次两种通知使用同一份订阅快照，回调订阅变更从下一周开始生效。
            var weekObservers = WeekAdvanced;
            var termObservers = SolarTermChanged;
            CurrentDate = after;
            Notify(weekObservers, transition, nameof(WeekAdvanced));
            if (transition.SolarTermChanged)
                Notify(termObservers, transition, nameof(SolarTermChanged));
            return transition;
        }
        finally { _isAdvancing = false; }
    }

    private static void Notify(Action<WorldCalendarTransition> observers, WorldCalendarTransition transition, string name)
    {
        if (observers == null) return;
        foreach (Action<WorldCalendarTransition> observer in observers.GetInvocationList())
        {
            try { observer(transition); }
            catch (Exception ex) { ReportObserverFailure(name, ex); }
        }
    }

    private static void ReportObserverFailure(string name, Exception exception)
    {
        string message = $"[WorldCalendar] {name} 观察者异常；本周已提交，不应重试：{exception}";
        try
        {
            if (LogMgr.Instance != null) LogMgr.Instance.Warn("{0}", message);
            else Console.Error.WriteLine(message);
        }
        catch
        {
            // 日志本身也可能有抛异常的订阅者；诊断失败不能打断其他周历通知。
            try { Console.Error.WriteLine(message); }
            catch { /* 诊断通道全部失效时，仍保留已经提交的时间和正常成功返回。 */ }
        }
    }
}
