namespace UnityWorld.Game.Domain
{
    /// <summary>保留旧数据与入口用于诊断，不恢复资源池、旧费用上下限或战斗换装。</summary>
    public static class LegacyCombatCompatibility
    {
        /// <summary>拒绝已废弃的灵元费用上下限和费用调整属性</summary>
        public static void RequireSupportedStat(string statId)
        {
            if (string.IsNullOrEmpty(statId)) return;
            if (statId.Equals("Min", StringComparison.OrdinalIgnoreCase) || statId.Equals("Max", StringComparison.OrdinalIgnoreCase) || statId.StartsWith("Min_", StringComparison.OrdinalIgnoreCase) ||
                statId.StartsWith("Max_", StringComparison.OrdinalIgnoreCase) || statId.StartsWith("ManaAdj", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"Deprecated: {statId} 已废弃并禁用；不能调整背包点亮费用。");
        }

        /// <summary>以异常拒绝执行待定的背包操作</summary>
        public static void RejectPending(string operation) =>
            throw new NotSupportedException($"Pending: 战斗 {operation} 属于待定神修背包操作，当前禁止执行。");
    }
}
