using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.Domain.Tag;
using UnityWorld.Game.World;

// ═══════════════════════════════════════════════════════════
//  UnityWorld —— NPC系统实时Tick测试
//  规则：现实时间 1秒 = 游戏世界 1个月
//        每过1游戏年（12个月）打印一次状态
// ═══════════════════════════════════════════════════════════

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ① 初始化世界
WorldMgr.Initialize(DateTime.Now.Second);
WorldMgr.Start();

CombatTestRunner.RunBasicTest();