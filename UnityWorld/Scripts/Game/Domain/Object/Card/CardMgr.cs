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
        public static CardMgr Instance { get; private set; }

        // ── 子系统 ────────────────────────────────────────
        public CardSystemData DataSystem { get; } = new();
        public CardSystemGongFa GongFaSystem { get; } = new();
        public CardSystemEquip EquipSystem { get; } = new();

        public SoulData Soul { get; set; }      
        private readonly Rng _rng = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public CardMgr(int seed)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        // ── 从 Define 实例化 ──────────────────────────────────
        public Card InstantiateFromDefine(CardDefine cardDefine)
        {
            var cardid = Soul.NewId();
            var card = new Card
            {
                Id = cardid,
                DefineId = cardDefine.ID,
                DisplayName = cardDefine.DisplayName,
                ParentCardId = cardid,
                Stats = StatMgr.Instance.CreateBlock(cardid,GetType()),
            };
            DataSystem.Register(card, new CardBaseData
                {
                    Size = cardDefine.Size,
                    Cooldown = cardDefine.Cooldown,
                    ManaCost = ElementType.ToDic(cardDefine.ManaCost),
                    Keywords = cardDefine.Keywords,
                    Tags = cardDefine.Tags,
                });
            GongFaSystem.Register(card, new CardGongFaData { CardId = card.Id });
            EquipSystem.Register(card, new CardEquipData { CardId = card.Id });

            Add(card.Id,card);
            return card;
        }

        /// <summary>
        /// 从 CardDefine 实例化一张 Card，加入管理并返回。
        /// CardDefine → 遍历 EffectIds → 每个 EffectDefine 构造 EffectData → 拼 TagBag。
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

        /// <summary>
        /// 根据主题 TagBag 从卡池中匹配组合一套卡组
        /// </summary>
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

        public override void Init() { }

        public override void Begin() { }

        public override void Tick(float deltaTime) { }

        public override void Update() { }

        public override void Render(float dt) { }

        public override void End()
        {
            Instance = null;
        }

    public override IEnumerator Save()
        {
            yield break;
        }

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