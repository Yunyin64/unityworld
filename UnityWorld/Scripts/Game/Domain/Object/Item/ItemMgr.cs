using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Item 运行时管理器：扁平全局表（cardId → Item），
    /// 负责所有 Item 实例的 Add/Remove/Get/全局遍历。
    /// </summary>
    public class ItemMgr : DomainMgrBase<Item>, ISoulBase
    {
        /// <summary>单例</summary>
        public static ItemMgr Instance { get; private set; }

        public SoulData Soul { get; set; }

        public ItemMgr(int seed = 12345)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        // ── 生命周期 ────────────────────────────────────────

        public override void Init() { }

        public override void Begin() { }

        public override void Tick(float deltaTime) { }

        public override void Update() { }

        public override void Render(float dt) { }

        public override void End()
        {
            _allEntities.Clear();
            Instance = null;
        }

        public override IEnumerator Save() { yield break; }

        public override IEnumerator Load() { yield break; }

        /// <summary>
        /// 从 ItemDefine 创建运行时实例并注册
        /// </summary>
        public Item InstantiateFromDefine(int id, ItemDefine define)
        {
            var item = Item.FromDefine(id, define);
            Add(item.Id, item);
            return item;
        }

        /// <summary>日志输出</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[ItemMgr] 物品运行时管理器 | Item={0}", Count);
        }
    }
}
