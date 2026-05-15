using System.Collections;
using UnityWorld.Core;
/// <summary>
/// 运行时数据封装
/// </summary>
public abstract class GameEntityBase
{
    public StatBlock Stats = null;

    /// <summary>
    /// 获取属性最终值。默认返回 Stats.Get(statId)（裸值）。
    /// 战斗子类 override 此方法以叠加 Modifier hook 贡献。
    /// </summary>
    public virtual float GetStat(string statId)
    {
        return Stats != null ? Stats.Get(statId) : 0f;
    }

    public abstract void LogAllInfo();
    public abstract override string ToString();
}
