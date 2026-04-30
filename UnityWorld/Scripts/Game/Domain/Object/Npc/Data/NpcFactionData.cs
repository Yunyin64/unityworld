namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 社会派系数据（TODO: 未来实现）
    /// 
    /// 预计字段：
    ///   - 势力归属（所属门派、家族、国家等）
    ///   - 职位/身份（掌门、长老、弟子、外门等）
    ///   - 地位等级（在势力内的声望、贡献度）
    ///   - 势力关系（友好/敌对势力列表）
    ///   - 个人声望（社会影响力）
    /// </summary>
    public class NpcFactionData : IDomainDataBase
    {
        public IDomainDataBase Clone()
        {
            throw new NotImplementedException();
        }

        // TODO: 待实现
        public void Log()
        {
            
        }
    }
}
