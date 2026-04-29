using System.Collections;
using UnityWorld.Core;
/// <summary>
/// 运行时数据封装
/// </summary>
public abstract class GameEntityBase
{
    public StatBlock Stats = null;
    public abstract void LogAllInfo();
    public abstract override string ToString();
}
