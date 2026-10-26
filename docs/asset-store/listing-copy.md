# Break & Drop — Unity Asset Store Listing Copy

> Recommended submission language: English. A Chinese reference version is included below.
>
> Revision: 2026-10-21. Submission scope: the earlier BreakDrop prototype, as confirmed by the publisher. This is not the later candy UI / WeChat project. The uploaded archive has not been inspected in this revision; verify its contents against the dependency checklist in `resubmission/README.md` before resubmitting.

## Package title

**Break & Drop — 2D Physics Puzzle Starter**

## Overview (10–200 characters)

Break & Drop is a ready-to-extend Unity 2D physics puzzle starter featuring one-tap block removal, JSON-driven levels, undo, hints, countdowns, and 20 validated puzzles.

## Description

Break & Drop is a Unity 2D physics puzzle starter for portrait mobile games. Tap supports to guide a hexagonal target onto a safe platform. Blocks fall and rotate through real physics; input waits for the structure to settle.

### Included

- Home, gameplay, pause, win/fail and next-level flow.
- Undo, restart, hints, countdown, move tracking and height-based star progress.
- 20 editable JSON levels, rectangle/L-shaped blocks and 11 block art variants.
- Full C# source, fixed-seed level generation and editor checks for layouts, solution routes and repeated physics outcomes.
- Replaceable artwork and configurable physics, level layouts and hint sequences.

### Requirements and dependencies

- Tested with Unity 2022.3.13f1c1 and the Built-in Render Pipeline. Other editor versions, URP and HDRP are unverified.
- Requires Unity's built-in Physics 2D, IMGUI, text rendering, legacy input and JSON serialization modules. Keep these enabled. No separate Asset Store purchase or third-party gameplay SDK is required.
- Set Active Input Handling to Input Manager (Old) or Both. The new Input System alone is unsupported.
- Keep supplied assets under Assets/BreakDrop/Resources. Included Outfit and Noto Sans SC fonts use SIL Open Font License 1.1; licenses are in Resources/Fonts.
- Editor play needs no mobile build tools. Device builds require the relevant Unity build-support module and platform toolchain; iOS also requires macOS and Xcode. Signing and store submission require separate setup.

### Scope

This is a local gameplay prototype, not a complete commercial release. Audio, advertising, analytics, accounts, currencies, online services, advanced hazards and WeChat export integration are excluded. Core gameplay requires no API key, subscription or CDN.

## Technical details

- Unity version: **2022.3.13f1c1**
- Rendering: Unity built-in 2D workflow; no URP dependency
- Physics: Unity Physics 2D (`Rigidbody2D`, `BoxCollider2D`, compound L-shape colliders)
- Input: mouse click and single tap
- Target layout: portrait mobile presentation, authored around 390 × 844
- Runtime scene: `Assets/BreakDrop/Scenes/Prototype.unity`
- Runtime source: `Assets/BreakDrop/Scripts/Runtime`
- Editor tools: `Assets/BreakDrop/Editor`
- Level data: 20 JSON files under `Assets/BreakDrop/Resources/Levels`
- Art: 11 block variants plus Hex, Goal, Home, and UI controls
- Level sizes: Levels 1–5 use 10 blocks; Levels 6–15 use 18; Levels 16–20 use 21
- Supported block shapes: 1×1, 2×1, 3×1, 5×1, L2, and L3
- Authored rotations: 0°, 90°, 270°, and 360°
- Gameplay systems: state machine, timer, restart, undo snapshot, hint order, win/fail detection, next level, and local best-move persistence
- Editor generation: fixed seeds, occupancy masks, overlap checks, cavity checks, layout signatures, adjacent-level difference checks, and a maximum of 50 candidates per level
- Physics validation: each accepted hint route is simulated 10 times in Unity 2D before level JSON is written
- Dependencies: built-in Physics 2D, IMGUI, text rendering, legacy input and JSON serialization modules; supplied local artwork and fonts. See Requirements and dependencies above.
- Third-party font licenses: Outfit and Noto Sans SC, SIL Open Font License 1.1; license files supplied in `Resources/Fonts`
- Render pipeline: Built-in only; URP and HDRP compatibility has not been verified
- Source included: yes
- Audio included: no
- Tested editor version: Unity 2022.3.13f1c1

## AI Description

This package contains content generated in whole or in part with artificial intelligence (AI). OpenAI Codex was used to generate and assist with portions of the C# gameplay code, Unity Editor tools, and included documentation. OpenAI ChatGPT image generation was used to create portions of the included 2D artwork and UI graphics. Subsequent project-specific work included code integration and debugging, sprite import configuration, UI integration, level authoring, and physics and gameplay testing. This disclosure covers functional content included in the package, not only images used in marketing materials.

## Keywords (maximum 15)

1. physics puzzle
2. 2D puzzle
3. mobile game
4. casual game
5. game template
6. puzzle starter
7. Unity 2D
8. tap game
9. JSON levels
10. level generator
11. undo system
12. hint system
13. deterministic physics
14. source code
15. prototype

## Image upload map

| Store field | File | Size |
|---|---|---:|
| Icon image | `images/break-drop-icon-160x160.png` | 160 × 160 |
| Card image | `images/break-drop-card-420x280.png` | 420 × 280 |
| Cover image | `images/break-drop-cover-1950x1300.png` | 1950 × 1300 |
| Social media image | `images/break-drop-social-1200x630.png` | 1200 × 630 |

## Screenshot upload order and captions

1. `screenshots/01-home-1200px.png`  
   **Caption:** Clean portrait home screen with immediate access to the physics puzzle.
2. `screenshots/02-gameplay-1200px.png`  
   **Caption:** Tap colorful supports and plan a safe route for the hexagonal target.
3. `screenshots/03-physics-fall-1200px.png`  
   **Caption:** Real Unity 2D collisions, rotation, and falling behavior after every move.
4. `screenshots/04-undo-restored-1200px.png`  
   **Caption:** Undo restores blocks, target position, velocities, and the pre-move state.

## 中文参考版

### 概括

Break & Drop 是一套可扩展的 Unity 2D 物理解谜原型，包含点击拆块、JSON 关卡、撤销、提示、倒计时以及 20 个经过物理验证的关卡。

### 描述

#### 使用要求与依赖

- 开发及测试版本为 Unity 2022.3.13f1c1，未验证其他编辑器版本的兼容性。
- 使用 Built-in Render Pipeline，以及 Unity 内置 Physics 2D、IMGUI、文本渲染、旧版输入和 JSON 序列化模块；这些属于 Unity 组件，不是需要另外购买的 Asset Store 产品。
- 使用旧版 Input Manager 处理鼠标和触摸输入。Active Input Handling 应设为 Input Manager (Old) 或 Both，不以仅启用新版 Input System 为支持配置。
- 此次上传的早期原型不依赖 URP、HDRP、TextMesh Pro、新版 Input System 或第三方玩法 SDK，也不包含或依赖后来增加的微信导出集成。
- 美术及字体由 `Assets/BreakDrop/Resources` 本地加载，导入时保留对应目录和名称。Outfit、Noto Sans SC 字体采用 SIL Open Font License 1.1，许可证在 `Resources/Fonts`。
- 核心本地玩法不需要在线账号、API 密钥、服务器、订阅或 CDN。
- 编辑器内试玩不需要移动端构建工具；制作设备安装包需相应 Unity 构建支持模块及平台工具链，iOS 构建还需 macOS 和 Xcode。设备构建、签名与商店发布需另行配置。

#### 玩法与内容

Break & Drop 是一套面向移动端物理解谜游戏的 Unity 2D 起步项目。玩家只需点击拆除彩色支撑积木，通过重力、碰撞和承重关系，引导带有星形标志的六边形目标安全落到终点平台。

软件包提供完整的首页、游戏、暂停、胜利和失败流程，包含点击拆块、物理结算输入锁定、重开、撤销、提示、下一关、倒计时、三星高度进度和最佳步数记录。20 个关卡全部由 JSON 数据驱动，覆盖矩形、L2、L3、11 种积木素材及多种旋转方向。

编辑器生成器采用固定种子，并检查积木数量、重叠、空腔、结构唯一性、相邻关差异和正确路线。候选关卡在写入 JSON 前，会按照提示路线使用 Unity 2D 物理重复模拟 10 次。

项目提供完整 C# 源码，可替换美术、调整物理参数、修改 JSON 关卡或继续扩展新的机关。当前版本专注于核心物理解谜体验，不包含音频、广告、账号、货币、联网服务和高级危险机关。

### 技术细节

- 使用 Unity 2022.3.13f1c1
- 使用 Unity 内置 2D 物理，无 URP 依赖
- 支持鼠标点击和单指触控
- 提供 20 个 JSON 关卡及完整 C# 源码
- 提供矩形、L2、L3 复合碰撞体
- 包含撤销、提示、倒计时、重开、下一关和胜负状态机
- 固定种子编辑器关卡生成器，单关最多筛选 50 个候选
- 正确路线写入前使用真实 Unity 2D 求解器重复验证 10 次
- 不依赖第三方运行时插件或在线服务
- 不包含音频、广告、分析 SDK 或商业化系统

### AI 描述（中文参考，英文填写 AI Description 字段）

本软件包的全部或部分内容由人工智能（AI）生成。OpenAI Codex 用于生成及辅助编写部分 C# 游戏代码、Unity 编辑器工具和随包文档；OpenAI ChatGPT 图像生成用于制作部分随包二维美术与 UI 图形。后续针对本项目进行了代码集成与调试、精灵导入配置、UI 集成、关卡制作，以及物理和玩法测试。本声明覆盖软件包中的功能性内容，而不只涉及营销材料中的图片。

