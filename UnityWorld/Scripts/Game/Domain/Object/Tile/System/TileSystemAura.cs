using System.Collections.Generic;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块元气系统：每 Tick 将挂载在 Tile 上的 TileModifier 累积到 CurrentAura。
    /// 
    /// 驱动顺序（由 WorldMgr 保证）：
    ///   TileSystemAura.Tick → AuraDaoMgr.OnTick
    /// 即先更新元气，再让天道感知最新状态。
    /// </summary>
    public class TileSystemAura
    {
        /// <summary>
        /// 对指定位面的所有地块执行元气累积与 Modifier 清理。
        /// </summary>
        /// <param name="plane">目标位面</param>
        /// <param name="deltaTime">本帧经过的游戏时间（秒，已乘位面时间流速）</param>
        public void Tick(Plane plane, float deltaTime)
        {
            // 收集本 Tick 过期的 Modifier，遍历结束后统一移除，避免遍历中修改集合
            List<(Tile tile, TileModifier modifier)>? toRemove = null;

            foreach (var (_, tile) in plane.Tiles)
            {
                if (tile.Modifiers.Count == 0) continue;

                foreach (var modifier in tile.Modifiers)
                {
                    // 将 AuraData（每秒变化量）按 deltaTime 比例累积到 CurrentAura
                    tile.CurrentAura.Jing  += modifier.AuraData.Jing  * deltaTime;
                    tile.CurrentAura.Mu    += modifier.AuraData.Mu    * deltaTime;
                    tile.CurrentAura.Shui  += modifier.AuraData.Shui  * deltaTime;
                    tile.CurrentAura.Huo   += modifier.AuraData.Huo   * deltaTime;
                    tile.CurrentAura.Tu    += modifier.AuraData.Tu    * deltaTime;

                    // 递减有限时长 Modifier 的剩余时间
                    if (modifier.Duration >= 0f)
                        modifier.RemainingTime -= deltaTime;

                    // 标记过期
                    if (modifier.IsExpired)
                    {
                        toRemove ??= new List<(Tile, TileModifier)>();
                        toRemove.Add((tile, modifier));
                    }
                }
            }

            // 统一移除过期 Modifier
            if (toRemove != null)
                foreach (var (tile, modifier) in toRemove)
                    tile.RemoveModifier(modifier);
        }
    }
}
