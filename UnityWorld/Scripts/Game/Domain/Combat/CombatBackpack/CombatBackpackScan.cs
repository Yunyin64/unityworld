using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public sealed partial class CombatBackpack
    {
        // 扫描时钟与行轮推进；保留原有加减速和充能规则。
        /// <summary>加速按真实 Tick 延时，不叠加倍率。</summary>
        public void AddHaste(int ticks)
        {
            if (ticks > 0) { HasteTicks = checked(HasteTicks + ticks); PresentationOwner?.Scene?.MarkPresentationEffect(); }
        }

        /// <summary>减速按真实 Tick 延时，不抵消加速时长。</summary>
        public void AddSlow(int ticks)
        {
            if (ticks > 0) { SlowTicks = checked(SlowTicks + ticks); PresentationOwner?.Scene?.MarkPresentationEffect(); }
        }

        /// <summary>充能增加正常速度折算的进度，不应用速度倍率或消耗持续时间。</summary>
        public void Charge(double baselineTicks)
        {
            if (!double.IsFinite(baselineTicks)) throw new ArgumentOutOfRangeException(nameof(baselineTicks));
            if (baselineTicks > 0) { _scanProgress += (decimal)baselineTicks; PresentationOwner?.Scene?.MarkPresentationEffect(); }
        }

        /// <summary>真实 Tick 开始时调用一次；随后新加的速度效果从下个自然 Tick 生效。</summary>
        public void AdvanceNaturalTick()
        {
            _scanProgress += (HasteTicks > 0 ? 2m : 1m) * (SlowTicks > 0 ? 0.5m : 1m);
            if (HasteTicks > 0) HasteTicks--;
            if (SlowTicks > 0) SlowTicks--;
        }

        /// <summary>消费一行进度，保留充能和自然推进的所有余量。</summary>
        public bool TryTakeRow(int ticksPerRow = 10)
        {
            if (ticksPerRow < 1) throw new ArgumentOutOfRangeException(nameof(ticksPerRow));
            if (_scanProgress < ticksPerRow) return false;
            _scanProgress -= ticksPerRow;
            return true;
        }

        /// <summary>单步时钟测试适配；实际场景使用自然 Tick 与逐批消费行分离的入口。</summary>
        public bool AdvanceTick(int ticksPerRow = 10)
        {
            if (ticksPerRow < 1) throw new ArgumentOutOfRangeException(nameof(ticksPerRow));
            AdvanceNaturalTick();
            return TryTakeRow(ticksPerRow);
        }

        /// <summary>完成当前行后调用；返回是否完成了本方一轮。</summary>
        public bool FinishRow()
        {
            Row++;
            if (Row < Height) return false;
            Row = 0;
            Round++;
            return true;
        }
    }
}
