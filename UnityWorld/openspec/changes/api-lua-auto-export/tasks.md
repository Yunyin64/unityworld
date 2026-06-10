## 1. 核心生成逻辑

- [ ] 1.1 在 APIMgr.cs 中新增 `ExportLua()` 方法：遍历 `_apis`，按 APIType 分组，为每组调用 `GenerateLuaFile()`
- [ ] 1.2 实现 `GenerateLuaFile(APIType, List<API>)`：按设计模板拼接 Lua 字符串，写入 `Data/LuaScripts/{APIType}.lua`
- [ ] 1.3 实现类型转换逻辑：Int/Float → `tonumber(x)`，其他 → 直接传
- [ ] 1.4 实现可选参数守卫：`?` 前缀参数生成 `if x ~= nil then ... end`
- [ ] 1.5 Condition 特殊处理：剔除 "Result" 参数，末尾追加 `return ctx:GetObject("Result")`

## 2. 集成调用

- [ ] 2.1 在 `APIMgr.Init()` 末尾调用 `ExportLua()`
- [ ] 2.2 更新 `Data/LuaScripts/Init.lua` 的 require 列表，加入 `require("Contest")`（如有新文件）

## 3. 验证

- [ ] 3.1 运行游戏，确认生成的 Action.lua / Condition.lua / Contest.lua 内容与现有手写版等价
- [ ] 3.2 确认现有卡牌脚本（card_xxx.lua）调用不报错
- [ ] 3.3 删除旧的手写 Scope.lua require（已 deprecated）
