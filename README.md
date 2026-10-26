# 糖果拆拆乐 / BreakDrop

Break & Drop 是一个面向手机与微信小游戏的 Unity 2D 物理解谜原型。玩家点击拆除支撑块，利用稳定、可预测的物理坠落，让六边形目标安全抵达底部平台。

当前仓库已经把共享对话中的产品分析整理成可持续开发的 Codex 项目，并落地第一阶段可玩原型：

- 单指点击拆块
- 2D 刚体坠落与碰撞
- 三星高度进度
- 成功、失败与下一关
- Retry、Undo、Hint
- JSON 数据化关卡
- 自动生成原型场景
- 20 个差异化、经过物理重复性验证的关卡

## 打开与运行

1. 使用 Unity `2022.3.13f1c1` 打开本目录。
2. 等待 Package Manager 完成依赖解析和脚本编译，使用 Built-in Render Pipeline 及旧输入系统（Input Manager 或 Both）。
3. 仓库包含 `Assets/BreakDrop/Scenes/Prototype.unity`。需要重建时使用 `Tools > Break & Drop > Build Prototype Scene`，重建前备份自定义场景。
4. 打开 `Prototype` 场景并进入 Play Mode。

操作：点击彩色块；底部按钮可撤销、提示或重开。胜利后选择下一关。

## 项目入口

- 产品与规则：[docs/GDD.md](docs/GDD.md)
- 开发路线：[docs/ROADMAP.md](docs/ROADMAP.md)
- Codex 协作约定：[AGENTS.md](AGENTS.md)
- 运行时代码：`Assets/BreakDrop/Scripts/Runtime`
- 关卡数据：`Assets/BreakDrop/Resources/Levels`

## 当前边界

当前工程包含糖果风格 UGUI、20 个正式关卡、关卡工作台、本地体力与三类道具库存、每日免费补给。没有金币系统。广告入口目前为不可用占位，不发放广告奖励。首页和广告弹窗内的按钮已分别统一尺寸。

本仓库保留微信小游戏 SDK 与导出脚本，微信构建说明见 [docs/WECHAT_BUILD.md](docs/WECHAT_BUILD.md)。这是开发工程，不是已通过平台审核的消费者发行包；Android/iOS 真机验收未完成。

Unity UI 等依赖由 `Packages/manifest.json` 声明。首次打开需要下载对应 Unity Registry 包。`Assets/WX-WASM-SDK-V2` 中随 SDK 分发的 Node 依赖和平台转换工具属于构建依赖，不是游戏安装包。

## 仓库范围

提交源码、美术、字体及许可、场景、Prefab、关卡数据、微信 SDK、Packages 和 ProjectSettings。Unity 缓存、构建产物、验证日志、个人行政文书、软著申报材料和暂停制作的网站不纳入版本控制。

最近一轮按钮修改已通过 Unity `2022.3.13f1c1` 编译与 95 项 UGUI 交互检查。历史发布包属于独立快照，不随源码变更自动更新。代码及部分美术使用过 AI 辅助生成，详见 [AI 声明](docs/asset-store/release-1.0.0/ai-description.en.txt)；第三方字体与 SDK 保留其各自许可。

## 来源

产品方向来自用户提供的 ChatGPT 共享对话：

<https://chatgpt.com/share/6a9e9e53-dbd4-83e8-a889-3a5518f26f46>
