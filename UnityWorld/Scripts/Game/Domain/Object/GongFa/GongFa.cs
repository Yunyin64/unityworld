using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 修炼槽位：NPC 持有的功法实例
    /// </summary>
    public class GongFa:IFormDefine<CultivationDefine>
    {
        public string DisplayName { get; set; } = "";

        /// <summary>功法定义 ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>当前修炼点数</summary>
        public int CurrentPoint { get; set; } = 0;
        /// <summary>
        /// 获取已解锁的修炼节点
        /// </summary>
        public List<CultivationPointDefine> GetUnlockedPoints()
        {
            var define = CultivationDefineMgr.Instance?.Get(DefineId);
            if (define == null) return [];

            return define.Points
                .Where(p => p.Threshold <= CurrentPoint)
                .ToList();
        }

        /// <summary>
        /// 获取下一个待解锁的修炼节点
        /// </summary>
        public CultivationPointDefine GetNextPoint()
        {
            var define = CultivationDefineMgr.Instance?.Get(DefineId);
            if (define == null) return null;

            return define.Points
                .Where(p => p.Threshold > CurrentPoint)
                .OrderBy(p => p.Threshold)
                .FirstOrDefault();
        }

        /// <summary>
        /// 功法是否已修炼完成
        /// </summary>
        public bool IsComplete()
        {
            var define = CultivationDefineMgr.Instance?.Get(DefineId);
            if (define == null) return false;
            return CurrentPoint >= define.MaxPoint;
        }


        public override string ToString() => $"({DefineId})";

    }

}
