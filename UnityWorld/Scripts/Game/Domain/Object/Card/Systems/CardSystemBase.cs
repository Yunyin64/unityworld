using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 子系统基类：以 cardId 为键管理某类数据
    /// </summary>
    public abstract class CardSystemBase<T> : ISystemBase<Card>
    {
        /// <summary>以 cardId 为键的数据表</summary>
        protected abstract Dictionary<int, T> _dataTable { get; set; }
        /// <summary>已登记的卡牌数据条目数</summary>
        public int RegisteredCount => _dataTable.Count;

        /// <summary>注册一张 Card 的数据</summary>
        public virtual void Register(Card card, T data)
        {
            _dataTable[card.Id] = data;
        }

        /// <summary>获取指定 Card 的数据，不存在返回 default</summary>
        public virtual T GetData(int cardId)
        {
            return _dataTable.TryGetValue(cardId, out var data) ? data : default;
        }

        /// <summary>移除指定 Card 的数据</summary>
        public virtual void Unregister(int cardId)
        {
            _dataTable.Remove(cardId);
        }

        /// <summary>推进指定卡牌在本系统中的时步</summary>
        public abstract void OnTick(Card card, float deltaTime);
    }
}
