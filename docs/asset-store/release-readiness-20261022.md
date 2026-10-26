# BreakDrop 上架就绪检查

检查日期：2026-10-22（Asia/Shanghai）。目标按上下文解释为 Unity Asset Store。

## 结论

**当前完整工程暂不具备直接送审条件。早期已上传原型也尚不能确认已满足重新提交条件。**

这是本地工程、提交材料、历史验证记录和公开规则的检查结果，不是发布后台审核结果。未操作发布后台、未上传、未重新发布微信包，网站保持暂停。

## 必须处理的事项

| 优先级 | 发现 | 依据与影响 | 完成条件 |
|---|---|---|---|
| 高 | 当前工程与早期提交文案不一致 | `resubmission/description.en.txt` 描述 IMGUI、无音频及无微信集成；当前 `CandyUguiView.cs` 引用 Unity UI，`CandyFeedback.cs` 创建音乐和音效，`WeChatBuild.cs` 引用微信 SDK | 明确实际送审版本，并使描述、技术细节、截图与导出包一致。不能把早期版文案直接用于当前完整工程 |
| 高 | 微信构建脚本形成额外依赖 | `Assets/BreakDrop/Editor/WeChatBuild.cs` 第 5 行无条件引用 `WeChatWASM`，导出时还使用 SDK 的类型；只导出 `Assets/BreakDrop` 无法被视为独立、无第三方依赖的包 | 纯 Unity 模板导出应隔离微信专用脚本；如保留集成，则完整解决依赖、授权和平台要求，并进行干净导入验证 |
| 高 | 不能原样打包整个 Assets | SDK 目录内有 `convert.exe`、`brotli.exe`、纹理转换等可执行工具；提交规则 1.5.a 禁止提交包包含可执行文件 | 不将 SDK 工具目录直接随模板导出；同时解决上条脚本依赖，不能只删除工具文件后声称微信集成可用 |
| 高 | 核心资源目录缺少随包使用文档与第三方汇总声明 | `Assets/BreakDrop` 下文本类文件只找到字体 OFL 许可证；工程外 `docs` 目录不会因导出核心资源目录而自动随包提供 | 添加英文安装、依赖、场景入口、编辑扩展说明，以及 Third-Party Notices；字体许可证已存在，不需要重复购买字体 |
| 待验证 | 缺少针对最终包的本次编译与干净导入结果 | 本机 `C:/Program Files/Unity/Hub/Editor/2022.3.13f1c1` 目录存在，但递归检查未找到 `Unity.exe`。未切换到其他版本升级工程；没有找到待提交的 `.unitypackage` | 恢复目标编辑器后，用最终包在干净项目中检查导入、编译、Console 和核心玩法 |
| 待确认 | AI 字段与依赖字段是否已在后台保存未知 | 本地 AI Description、Description、Technical details 已准备，未读取本次 Publisher Portal 状态 | 在后台核对字段和实际提交版本；保留代码、文档及包内美术的真实 AI 披露 |

SDK 的 asmdef 还引用 `Unity.InstantGame.Editor` 和 GUID `39e0a8d734341a748a11d45f50641371`。在 Assets、Packages 与 Library/PackageCache 的元数据扫描中未定位到对应定义。这是需要编译验证的依赖线索，**不是本次已复现的编译错误**。

## 已有良好基础

以下均为读取历史报告，不是今天重新运行：

- 正式关卡：200 次记录，0 失败（`docs/validation/tower-physics.json`，2026-10-11）。
- 五关样板 Play 验证：50 次，0 失败（`docs/validation/support-samples/live-validation.json`，2026-10-11）。
- 落地判定：6 个场景，0 失败（`docs/validation/goal-landing/after.json`，2026-10-11）。
- 关卡工作台：45 项通过，0 失败（`docs/validation/level-workshop/acceptance.json`，2026-10-11）。
- UGUI 交互：95 条 PASS，0 条 FAIL（`docs/validation/ugui/checks.txt`，2026-10-09）。
- Prototype 场景、Canvas Prefab、EventSystem 存在；Built-in Render Pipeline 和旧版输入设置与本地配置一致。
- `Packages/manifest.json` 已列出 `com.unity.ugui` 1.0.0；Outfit、Noto Sans SC 的 OFL 文件存在。

不能把历史测试直接当成新导出包或当前上传包的通过证据。

## 最短完成路线

1. 若继续补正已上传的早期原型：核验那个实际导出包，沿用已准备的早期英文文案，补齐随包文档和第三方声明，完成干净导入与后台字段核对。
2. 若改为上架当前糖果版本：先建立纯 Unity 模板的明确导出范围、隔离微信构建依赖，再重写对应文案与截图；完成最终包的干净导入及核心回归后再提交。

当前请求仅为检查，本次没有自动执行上述整改，也没有修改游戏源码、关卡、依赖或既有发布文案。

如果“上线”指微信小游戏 / Android / iOS 正式发行，本报告不能替代平台上线验收。`docs/ROADMAP.md` 仍把真机安全区与设备测试、真实激励广告及奖励校验列为未完成；没有据此认定商业版本已经具备发行条件。

## 规则依据

[Unity Asset Store Submission Guidelines](https://assetstore.unity.com/publishing/submission-guidelines)：1.1.b–c（错误及依赖）、1.2.a（第三方声明）、1.5.a（可执行文件）、1.6.a–b（AI 披露）、2.3（使用文档）、3.1.a（描述准确性）。

[Asset Store Content Transparency](https://unity.com/legal/asset-store-content-transparency)：1.2.4（未标记的 AI 内容）。
