# Tasks

## 1. 定义与导出

- [x] 1.1 增加向后兼容的地标图片字段，并校验逻辑资源地址。
- [x] 1.2 实现新定义和 PNG 的受校验导出，覆盖冲突和失败时保留原文件。

## 2. 编辑器与探索

- [x] 2.1 在测试首页新增同级入口，实现编辑、复制、新建、图片预览和状态反馈。
- [x] 2.2 从真实 Tile 传递地标图片到 2D 地图和地点缩略图，复用纹理缓存并保留旧回退。

## 3. 十种内置地标

- [x] 3.1 制作十张可用的透明 2D 图片并接入地标定义。
- [x] 3.2 把十个定义配置到默认世界既有必出地区的合法地块。

## 4. 验收

- [x] 4.1 验证 OpenSpec、构建和后端地图回归。
- [x] 4.2 用 Godot 核对首页导航、字段编辑、导出后重开、十张图在实际 2D 探索的绘制及旧资源回退。

## Verification

- `openspec validate add-landmark-editor-and-2d-art --strict` 通过。
- `dotnet build Xianxia.Client.csproj --no-restore` 通过，零错误。
- `dotnet run --project Console/UnityWorld.Console.csproj --no-restore -- --map-generation-tests --data-root .` 在 `UnityWorld` 下通过。地图运行时测试通过 11,247 条断言。
- 以 Godot Windows 图形模式运行 `-- --check-landmarks`。从测试首页打开编辑器，复制定义并导入图片，验证拒绝未知修正与重复 ID，再从首页打开真实 2D 探索。十个内置地标和新导出地标均通过地图格与地点缩略图的像素对照，旧地标保留通用标记。
