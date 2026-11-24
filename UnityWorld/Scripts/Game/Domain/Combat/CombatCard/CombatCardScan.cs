using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard
    {
        /// <summary>本次用卡的回放动作编号</summary>
        internal long? PresentationActionId { get; private set; }
        /// <summary>本次用卡的回放拼点编号</summary>
        internal long? PresentationContestId { get; set; }

        /// <summary>处理卡牌扫描末格，尝试发动或通知未能发动</summary>
        internal void ResolveScanEnd(ref bool rowActivationUsed)
        {
            // 充能可以在同一 Tick 扫多行。上次已结算的卡按实际蓄灵进入下一次扫描。
            ReLoad();
            var wasFrozen = IsFrozen;
            if (!wasFrozen && !rowActivationUsed && CanUseAtScanEnd())
            {
                CommitScanUse(ref rowActivationUsed);
                return;
            }

            // 本行额度已用、缺灵或冻结也算一次真实末格。主词先处理失败，再解冻。
            CallMainKeyword("OnScanEnd", CreateCtx(), false);
            if (wasFrozen) ConsumeFreezeAtScanEnd();
        }

        /// <summary>兼容旧调用。事件可驱动激活中的被动，主动发动只由扫描授权。</summary>
        public bool TryUseFromEvent(APIContext eventCtx) => false;

        /// <summary>检查场景、蓄灵和主关键词是否允许末格发动</summary>
        private bool CanUseAtScanEnd()
        {
            if (Scene.IsFinished || !Owner.IsActive || Scene.IsActivating(this) || !HasActivationResources()) return false;
            return CallMainKeyword("CanCommitScan") is true;
        }

        /// <summary>占用本行发动名额，支付灵元并提交用卡</summary>
        private void CommitScanUse(ref bool rowActivationUsed)
        {
            Scene.ThrowIfEventAborted();
            var paidMana = AssignedElements.ToArray();
            var resourcesBefore = Scene.PresentationRecordingEnabled
                ? CombatScene.PresentationCopy(paidMana.Select(e => e.ToString())) : null;
            // 先提交公共资源和次数。主词在表现记录和玩法事件之前提交自己的阶段及额度。
            rowActivationUsed = true;
            ClearAssignedMana();
            UseCount++;
            var parentOrigin = Scene.CapturePresentationOrigin();
            PresentationActionId = Scene.NextPresentationIdentity();
            PresentationContestId = null;
            using var presentationOrigin = Scene.EnterPresentationOrigin(new PresentationOrigin(PresentationActionId,
                parentOrigin?.ActionId, parentOrigin?.ContestId, null));
            using var activation = Scene.BeginActivation(this);
            CallMainKeyword("OnScanCommitted");
            Log("末格扫描提交发动");
            if (resourcesBefore != null)
                Scene.RecordPresentation(CombatPresentationKind.CardUsed, Owner, this,
                    origin: new PresentationOrigin(PresentationActionId, parentOrigin?.ActionId, parentOrigin?.ContestId, null),
                    used: new CardUsedPresentation(resourcesBefore, Array.AsReadOnly(Array.Empty<string>())));
            Use(paidMana);
        }
    }
}
