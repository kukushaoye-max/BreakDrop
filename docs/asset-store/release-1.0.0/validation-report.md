# BreakDrop 1.0.0 发布准备报告

日期：2026-10-22。范围：当前糖果版 Unity Asset Store 模板；不代表 Android、iOS 或微信消费者版本已上线。

## 结论

指定版本 Unity 2022.3.13f1c1 已恢复并完成编译、空白工程导入及核心/UI 回归。已生成独立提交包与完整源码工程压缩包。Android 和 iOS 真机验收按要求跳过，未记录为通过。尚未上传 Publisher Portal，尚未获 Unity 审核通过。

## 交付位置

根目录：`E:/Project/BreakDrop/Builds/AssetStoreRelease-20261022`

- `BreakDrop-1.0.0.unitypackage`：仅包含 `Assets/BreakDrop`，171 个资产文件及相应元数据。
- `BreakDrop-1.0.0-SourceProject.zip`：Assets/BreakDrop、Packages、ProjectSettings；不含 Library、SDK、验证辅助脚本和可执行程序。
- `SHA256SUMS.txt`：上述两份交付的 SHA-256。
- `Evidence/package-inventory.json`：包内路径、大小、文件校验值及正式关卡一致性检查。
- `Evidence/clean-validation`：从最终包导入后产生的验收结果。

用于继续发布的工程应从 SourceProject.zip 解压；`Project` 和 `CleanImport` 目录保留了内部验收工具，不应将其整个 Assets 目录上传。

## 发布版修改

1. 仅在隔离发布副本中修复 HomeUiSlicing 的过时 Sprite 导入 API，使用 2D Sprite 的公开数据接口；切图前后六个 Sprite 的 GUID/Local ID 保持一致。
2. 移出调用 Unity Editor 内部反射接口的 CandyVisualAudit 和 SupportSampleCapture；UguiAuthoring 保留 Canvas 生成功能，移除内部 Console 操作，状态查询改用公开 API。
3. 排除微信 SDK、WeChatBuild 导出集成、Asset Store 上传工具及其可执行程序。
4. 补充英文安装、玩法、编辑扩展、存档、限制、依赖及 AI 披露说明；附字体许可和 Third-Party Notices。
5. 准备与当前糖果版匹配的商店文案，明确中文界面、部分文字/按钮已烘焙到美术、广告仅为占位、无云服务及平台验证边界。此前早期原型文案仍保留，不能与本包混用。

原工程运行脚本、美术、微信集成和正式关卡未被这些发布修订覆盖。发布包中的 23 个关卡 JSON 与原工程逐字节一致，其中 20 个 towerLevel 关卡构成正式目录，另 3 个是历史测试数据。

## 验证结果

编辑器：`D:/Program Files/Unity/Hub/Editor/2022.3.13f1c1/Editor/Unity.exe`，版本 `2022.3.13f1c1_d214d547af8b`。

| 检查 | 结果 | 证据（相对交付根目录） |
|---|---|---|
| 全新工程导入最终 unitypackage | 通过，退出码 0；无 C# 编译错误/警告 | Evidence/clean-import.log |
| 核心及关卡工作台 | 46 项通过，0 失败 | Evidence/clean-core.log、clean-validation/level-workshop/acceptance.json |
| 20 正式关卡参考路线 | 每关重建并重复 10 次，200/200 通关 | 同上，production reference x10 条目 |
| 固定支撑、铰链示例 | 6 组变体各重复 10 次，通过 | 同上，mechanism example x10 条目 |
| 落地判胜与误判保护 | 中央、两侧稳定悬边、出界、平台下方、悬空共 6 项通过 | Evidence/clean-validation/goal-landing/release.json |
| UGUI 行为 | 95 项通过，涵盖弹窗拦截、拆除/等待、撤销、提示、重开、结算等 | Evidence/clean-ui.log、clean-validation/ugui/checks.txt |
| 屏幕适配计算 | 11 类视口、Canvas 边界、比例及指针映射通过 | Evidence/clean-validation/wechat-viewport/unity-checks.txt |
| Sprite 引用 | 切图后原有六个引用保持一致 | Evidence/clean-core.log 中 RELEASE_SPRITE_REFERENCES_PASS |
| Windows x64 开发预览 | 构建成功；运行自动流程，无异常日志；撤销后目标位置差约 0.000149 世界单位 | Evidence/desktop-build-final.log、desktop-player-final.log、screenshots/visual-capture.txt |
| 离屏画面检查 | 390×844 首页和第 20 关彩色画面正常，背景铺满，主要控件及文字可见；人工查看两张输出图 | Evidence/offscreen-screenshots/home.png、gameplay.png |
| 包内容核验 | 包内文件与发布源文件一致；关卡与原工程一致；无 SDK/EXE/内部 Editor 反射调用 | Evidence/package-inventory.json |
| 源码 ZIP 完整性 | ZIP CRC 校验通过；包含依赖 manifest；排除 QA 工具 | Evidence/finalize_release.py、SHA256SUMS.txt |

视口报告沿用历史 `wechat-viewport` 目录名，仅验证布局计算，并不表示本次验证了微信部署。

## 测试过程中的问题及处置

- 隔离工程首次更新编译缓存时出现旧路径的 CS2001（已移除的测试包/已移动的辅助脚本）；随后重新生成编译图成功。最终空白工程导入与回归日志没有这些编译错误。
- 第一轮 UGUI 测试通过后，Unity 在立即退出时出现后台指标线程断言。验收辅助工具增加退出 Play 后 5 秒等待，第二轮及空白导入轮通过且未复现。未修改游戏逻辑来掩盖问题。
- Unity 本机许可客户端日志包含签名校验/令牌提示，但编辑器成功完成许可、编译、构建和退出；未将这些环境提示误计为游戏脚本错误。
- 隐藏窗口的直接屏幕截图为黑色，`Evidence/screenshots` 仅保留流程诊断，不能作为画面验收或营销图片。另行采用公开 Camera.Render 接口进行离屏画面检查，记录在 `Evidence/offscreen-screenshots`；离屏检查不替代真机画面验收。

## 使用与验证边界

Unity UI 1.0.0 和 2D Sprite 1.0.0 已声明于随附工程 manifest；导入 unitypackage 到其他工程前，按 README 安装这些依赖并启用旧输入系统。使用 Built-in Render Pipeline。其他 Unity 版本、URP/HDRP、WebGL 部署及 Android/iOS 真机未在本次验收范围内。

源工程使用独立的 Company/Product 标识以隔离验收存档，正式发布者可在 Player Settings 中修改。更改标识会影响 PlayerPrefs 存储位置。Windows 验证程序与 QA 辅助脚本仅留在交付工作目录，未包含于两份客户包。

## 提交前仍需由发布者完成

1. 在 Publisher Portal 上传本报告对应的包，并填写同目录的 description、technical-details 和 ai-description 文本；亲自复核实际产品信息和措辞后提交。
2. 确认发布者邮箱与现有网站有效，使用当前糖果版画面，不能继续使用早期原型截图。网站制作仍按要求暂停。
3. 不将本次测试写成移动设备认证、全平台适配或已获审核批准。

本次重点核对了官方指南的依赖/描述要求（1.1.c、3.1.a）、AI 披露（1.6）、第三方许可（1.2.a）、内部接口限制（2.5.g）及弃用 API 警告（2.5.i）。最终是否接受仍由 Unity 审核决定：[Asset Store Submission Guidelines](https://assetstore.unity.com/publishing/submission-guidelines)。
