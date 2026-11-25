using System.Collections;
using System.Text.Json;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Tag;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 管理器
    /// 负责持有运行时生成的所有 Card 实例，提供从 Define 实例化的入口，
    /// 以及卡组组合和动态生成功能
    /// </summary>
    public class CardMgr : DomainMgrBase<Card>,ISoulBase
    {
        /// <summary>当前卡牌管理器</summary>
        public static CardMgr Instance { get; private set; }

        // ── 子系统 ────────────────────────────────────────
        /// <summary>通用卡牌数据系统</summary>
        public CardSystemData DataSystem { get; } = new();
        /// <summary>功法卡附属数据系统</summary>
        public CardSystemGongFa GongFaSystem { get; } = new();
        /// <summary>装备卡附属数据系统</summary>
        public CardSystemEquip EquipSystem { get; } = new();
        /// <summary>物品卡附属数据系统</summary>
        public CardSystemItem ItemSystem { get; } = new();

        /// <summary>管理器的基础实体数据</summary>
        public SoulData Soul { get; set; }      
        /// <summary>管理器持有的随机源</summary>
        private readonly Rng _rng = new();

        /// <summary>卡牌数据的 JSON 序列化选项</summary>
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>按种子初始化实体数据并登记管理器</summary>
        public CardMgr(int seed)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        // ── 从 Define 实例化 ──────────────────────────────────
        /// <summary>校验卡牌定义，创建并注册卡牌及其运行数据</summary>
        public Card InstantiateFromDefine(CardDefine cardDefine)
        {
            var data = CreateBaseData(cardDefine);
            var cardid = Soul.NewId();
            var card = new Card
            {
                Id = cardid,
                DefineId = cardDefine.ID,
                DisplayName = cardDefine.DisplayName,
                ParentCardId = cardid,
                Stats = StatMgr.Instance.CreateBlock(cardid, GetType()),
            };
            DataSystem.Register(card, data);
            GongFaSystem.Register(card, new CardGongFaData { CardId = card.Id });
            EquipSystem.Register(card, new CardEquipData { CardId = card.Id });
            ItemSystem.Register(card, new CardItemData { CardId = card.Id });

            Add(card.Id, card);
            return card;
        }

        /// <summary>校验定义，生成独立的卡牌基础数据。</summary>
        internal static CardBaseData CreateBaseData(CardDefine cardDefine)
        {
            // 先校验角色再分配实体 ID，避免无效定义留下半注册卡牌。
            var main = CardKeywordRoles.RequireMain(cardDefine.Keywords, cardDefine.ID);
            var consumes = cardDefine.Keywords.Contains("Consume");
            if (cardDefine.Keywords.Contains("Amount") && cardDefine.AmountMax <= 0)
                throw new InvalidOperationException($"弹药卡 {cardDefine.ID} 的弹量上限必须为正数。");
            if (consumes && cardDefine.StackMax <= 0)
                throw new InvalidOperationException($"消耗卡 {cardDefine.ID} 的次数上限必须为正数。");
            if (main == CardMainKeyword.ZhaoShi && cardDefine.MaxUsesPerRound < 1)
                throw new InvalidOperationException($"招式 {cardDefine.ID} 的每轮发动上限必须为正数。");
            if (cardDefine.ManaCost.Keys.Any(k => k is "Min" or "Max"))
                throw new InvalidOperationException($"卡 {cardDefine.ID} 的 Min/Max 灵元需求已 Deprecated，禁止实例化以免变成免费或无效费用。");
            foreach (var key in cardDefine.ManaCost.Keys)
                if (!ElementType.IsKnownCostKey(key))
                    throw new InvalidOperationException($"卡 {cardDefine.ID} 的费用键 {key} 未注册。");
            return new CardBaseData
            {
                Size = cardDefine.Size,
                Shape = cardDefine.GetShape(),
                ScanActivatable = cardDefine.ScanActivatable,
                MaxUsesPerRound = cardDefine.MaxUsesPerRound,
                ProducesContest = cardDefine.ProducesContest,
                CompatibilityStatus = cardDefine.CompatibilityStatus,
                CompatibilityReason = cardDefine.CompatibilityReason,
                Cooldown = cardDefine.Cooldown,
                AmountMax = cardDefine.AmountMax,
                StackMax = cardDefine.StackMax,
                Stack = consumes ? cardDefine.StackMax : 0,
                ManaCost = ElementType.ToDic(cardDefine.ManaCost),
                Keywords = [.. cardDefine.Keywords],
                Tags = [.. cardDefine.Tags],
            };
        }

        /// <summary>注销指定卡实例的所有注册项与属性块。</summary>
        public void UnregisterCard(int cardId)
        {
            DataSystem.Unregister(cardId); GongFaSystem.Unregister(cardId);
            EquipSystem.Unregister(cardId); ItemSystem.Unregister(cardId);
            GongFaMgr.Instance?.Remove(cardId); EquipMgr.Instance?.Remove(cardId);
            StatMgr.Instance.RemoveBlock(cardId, GetType());
            Remove(cardId);
        }

        /// <summary>
        /// 从 CardDefine 实例化一张 Card，加入管理并返回。
        /// 找不到 CardDefine 时记录错误并返回 null。
        /// </summary>
        public Card InstantiateFromDefine(string cardDefineId)
        {
            var cardDefine = CardDefineMgr.Instance?.Get(cardDefineId);
            if (cardDefine == null)
            {
                LogMgr.Instance.Err("[CardMgr] 找不到 CardDefine：{0}", cardDefineId);
                return null;
            }
            return InstantiateFromDefine(cardDefine);
        }


        // ── 卡组组合（原 CardSystemDeck）────────────────────────

        /// <summary>保留按主题组合卡组的入口，当前返回空列表</summary>
        /// <param name="themeTags">卡组主题 TagBag（重复表示浓度）</param>
        /// <param name="matchType">匹配类型</param>
        /// <param name="matchDegree">匹配度</param>
        /// <param name="deckSize">卡组张数</param>
        public List<Card> BuildDeck(
            List<string> themeTags,
            TagMatchType matchType,
            float matchDegree,
            int deckSize)
        {
            // TODO: 实现 Deck 组合逻辑
            // 复用 TagMgr.Match，输入已有 Card 的 TagBag
            return [];
        }

        // ── 生命周期 ──────────────────────────────────────────

        /// <summary>保留管理器初始化入口</summary>
        public override void Init() { }

        /// <summary>保留世界启动回调</summary>
        public override void Begin() { }

        /// <summary>保留时步回调</summary>
        public override void Tick(float deltaTime) { }

        /// <summary>保留帧更新回调</summary>
        public override void Update() { }

        /// <summary>保留渲染回调</summary>
        public override void Render(float dt) { }

        /// <summary>释放当前卡牌管理器实例</summary>
        public override void End()
        {
            Instance = null;
        }

    /// <summary>结束存档流程，当前无写入</summary>
    public override IEnumerator Save()
        {
            yield break;
        }

    /// <summary>结束读档流程，当前无读取</summary>
    public override IEnumerator Load()
        {
            yield break;
        }

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[CardMgr] Card管理器 | Cards={0}", _allEntities.Count);
        }
    }

}
