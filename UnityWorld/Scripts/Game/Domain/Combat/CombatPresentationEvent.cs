#nullable enable
namespace UnityWorld.Game.Domain.Combat;

/// <summary>纯值表现事实；坐标均为核心零基坐标，所有集合在创建时复制。</summary>
public enum CombatPresentationKind
{
    /// <summary>灵元分配事件</summary>
    ManaAssigned,
    /// <summary>卡牌使用事件</summary>
    CardUsed,
    /// <summary>待发槽变化事件</summary>
    PendingChanged,
    /// <summary>拼点结算事件</summary>
    ContestResolved,
    /// <summary>伤害结算事件</summary>
    DamageResolved,
    /// <summary>卡牌加入背包事件</summary>
    CardAddedToBackpack,
    /// <summary>卡牌效果生效事件</summary>
    CardApplied
}
/// <summary>保存回放用的格坐标</summary>
/// <param name="X">横向格坐标</param>
/// <param name="Y">纵向格坐标</param>
public sealed record PresentationCell(int X, int Y);
/// <summary>记录一种元素的灵元需求</summary>
/// <param name="Element">需求元素</param>
/// <param name="Count">需要的灵元数量</param>
public sealed record PresentationManaCost(string Element, int Count);
/// <summary>记录盘面上的一个灵元格</summary>
/// <param name="Cell">灵元所在的格坐标</param>
/// <param name="Element">格内灵元的元素</param>
public sealed record PresentationManaCell(PresentationCell Cell, string Element);
/// <summary>记录可用于战斗回放的卡牌快照</summary>
/// <param name="Id">战斗卡实例编号</param>
/// <param name="DefineId">卡牌定义编号</param>
/// <param name="Name">卡牌显示名</param>
/// <param name="Size">卡牌体量</param>
/// <param name="Keywords">卡牌拥有的关键词</param>
/// <param name="ManaCost">按元素列出的灵元需求</param>
/// <param name="AssignedElements">已分配给卡牌的灵元元素</param>
/// <param name="Cells">卡牌在背包中的绝对占格</param>
/// <param name="Shape">相对锚点的形状格</param>
public sealed record CombatPresentationCard(int Id, string DefineId, string Name, int Size,
    IReadOnlyList<string> Keywords, IReadOnlyList<PresentationManaCost> ManaCost,
    IReadOnlyList<string> AssignedElements, IReadOnlyList<PresentationCell> Cells, IReadOnlyList<PresentationCell> Shape)
{
    /// <summary>记录当时的阶段，入场回放可直接显示已激活的被动卡。</summary>
    public CombatCardPhase Phase { get; init; } = CombatCardPhase.Waiting;
}
/// <summary>记录一枚灵元从盘面分配给卡牌的变化</summary>
/// <param name="SourceCell">灵元分配前所在的格</param>
/// <param name="Element">被分配灵元的元素</param>
/// <param name="Before">分配前已充入卡牌的元素</param>
/// <param name="After">分配后已充入卡牌的元素</param>
public sealed record ManaAssignedPresentation(PresentationCell SourceCell, string Element,
    IReadOnlyList<string> Before, IReadOnlyList<string> After);
/// <summary>记录卡牌使用时灵元列表的变化</summary>
/// <param name="Before">使用前卡内的灵元元素</param>
/// <param name="After">使用后卡内的灵元元素</param>
public sealed record CardUsedPresentation(IReadOnlyList<string> Before, IReadOnlyList<string> After);
/// <summary>记录拼点请求进入或退出待发槽</summary>
/// <param name="Entered">是否进入待发槽</param>
/// <param name="Reason">待发状态变化原因</param>
/// <param name="ContestType">待发拼点类型</param>
/// <param name="Element">待发拼点元素</param>
/// <param name="Value">待发拼点点数</param>
public sealed record PendingPresentation(bool Entered, string Reason, string ContestType, string Element, int Value);
/// <summary>记录拼点一方的角色、来源和点数</summary>
/// <param name="NpcId">该侧角色编号</param>
/// <param name="Card">该侧来源卡牌</param>
/// <param name="ActionId">该侧发起动作编号</param>
/// <param name="ContestType">该侧拼点类型</param>
/// <param name="Element">该侧拼点元素</param>
/// <param name="Value">该侧拼点点数</param>
public sealed record ContestSidePresentation(int NpcId, CombatPresentationCard Card, long? ActionId,
    string ContestType, string Element, int Value);
/// <summary>记录双方拼点及结算结果</summary>
/// <param name="Left">左侧拼点数据</param>
/// <param name="Right">右侧拼点数据</param>
/// <param name="WinnerNpcId">胜方编号，平局时为空</param>
/// <param name="Difference">双方最终点数差</param>
public sealed record ContestPresentation(ContestSidePresentation Left, ContestSidePresentation Right,
    int? WinnerNpcId, int Difference);
/// <summary>记录一次伤害对护盾和生命的实际影响</summary>
/// <param name="SourceNpcId">伤害来源角色编号</param>
/// <param name="SourceCard">伤害来源卡快照</param>
/// <param name="TargetNpcId">承伤角色编号</param>
/// <param name="HpBefore">受伤前生命值</param>
/// <param name="HpAfter">受伤后生命值</param>
/// <param name="HpDamage">扣除的生命值</param>
/// <param name="ShieldAbsorbed">护盾吸收的伤害</param>
/// <param name="DamageType">伤害类型</param>
/// <param name="Element">伤害元素</param>
public sealed record DamagePresentation(int? SourceNpcId, CombatPresentationCard? SourceCard, int TargetNpcId,
    int HpBefore, int HpAfter, int HpDamage, int ShieldAbsorbed, string DamageType, string Element);
/// <summary>记录卡牌入场时覆盖的盘面灵元</summary>
/// <param name="ReplacedMana">被入场卡覆盖的灵元格</param>
public sealed record CardAddedPresentation(IReadOnlyList<PresentationManaCell> ReplacedMana);
/// <summary>记录一次卡牌生效的回放信息</summary>
/// <param name="Origin">效果的触发来源</param>
/// <param name="IsPassive">是否由被动效果生效</param>
public sealed record CardAppliedPresentation(string Origin, bool IsPassive);

/// <summary>序号与关联 ID 仅属于观察通道，不复用玩法事件计数或随机数。</summary>
/// <param name="Tick">事件发生的战斗时步</param>
/// <param name="Sequence">回放事件的单调序号</param>
/// <param name="IsInitialization">是否在战斗初始化期间</param>
/// <param name="Kind">回放事件类型</param>
/// <param name="NpcId">事件主体角色编号</param>
/// <param name="Card">相关卡牌快照</param>
/// <param name="ActionId">关联动作编号</param>
/// <param name="ParentActionId">上游动作编号</param>
/// <param name="ContestId">关联拼点编号</param>
/// <param name="EffectId">关联效果编号</param>
/// <param name="ManaAssigned">灵元分配详情</param>
/// <param name="CardUsed">卡牌使用详情</param>
/// <param name="Pending">待发槽变化详情</param>
/// <param name="Contest">拼点结算详情</param>
/// <param name="Damage">伤害结算详情</param>
/// <param name="CardAdded">入场覆盖灵元详情</param>
/// <param name="Applied">卡牌生效详情</param>
public sealed record CombatPresentationEvent(
    int Tick, long Sequence, bool IsInitialization, CombatPresentationKind Kind, int NpcId,
    CombatPresentationCard? Card, long? ActionId, long? ParentActionId, long? ContestId, long? EffectId,
    ManaAssignedPresentation? ManaAssigned = null, CardUsedPresentation? CardUsed = null,
    PendingPresentation? Pending = null, ContestPresentation? Contest = null, DamagePresentation? Damage = null,
    CardAddedPresentation? CardAdded = null, CardAppliedPresentation? Applied = null);

/// <summary>延迟请求只携带值关联；不改原有伤害来源或玩法上下文。</summary>
/// <param name="ActionId">延迟请求关联的动作编号</param>
/// <param name="ParentActionId">延迟请求关联的上游动作编号</param>
/// <param name="ContestId">延迟请求关联的拼点编号</param>
/// <param name="EffectId">延迟请求关联的效果编号</param>
internal sealed record PresentationOrigin(long? ActionId, long? ParentActionId, long? ContestId, long? EffectId);
