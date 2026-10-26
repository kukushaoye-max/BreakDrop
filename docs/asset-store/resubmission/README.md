# BreakDrop 审核补正：AI 披露与依赖说明

日期：2026-10-21

## 本次适用版本

用户已确认被驳回的上传包只包含较早的 BreakDrop 原型。本文案不描述当前工程后续新增的糖果 UGUI、音频、体力系统或微信 SDK。

本次修改的是本地提交文案，未操作 Publisher Portal、未重新上传包、未发送审核回复。没有找到当时的 `.unitypackage`，因此未将当前工程的 manifest 当作上传包依赖清单，也没有声称完成旧包的干净导入测试。

## 发布后台填写位置

1. **AI Description**：完整粘贴 `ai-description.en.txt`，不要仅放在普通 Description 或出版商网站。
2. **Description**：用 `description.en.txt` 替换英文描述。依赖说明位于开头，其余内容保留早期原型范围。
3. **Technical details**：同步 `technical-details.en.txt`。
4. 如后台有 AI 使用选择项，按实际 AI 辅助内容选择。这里披露的是代码、文档和包内美术，不是只披露营销图片。
5. 最终由发布者核对、编辑英文描述，使其与实际交付内容相符，然后保存草稿并重新提交。不要把本地文件的完成误认为后台审核问题已消除。

## 上传包核对

重新提交前核对真正上传的早期包，而不是当前工作目录：

- 核对编辑器版本、资源目录、字体及对应许可证均与描述一致。
- 确认旧 UI 为 IMGUI。若旧包实际含 `UnityEngine.UI` / `UnityEngine.EventSystems` 引用，则必须补充 Unity UI (`com.unity.ugui`) 的实际版本，并按包管理方式声明依赖。
- 核对旧包不包含 `WeChatBuild.cs`、`WX-WASM-SDK-V2` 等后来集成；如果包含，当前早期版文案不适用，需先重新确定交付范围。
- 在 Unity 2022.3.13f1c1 干净项目中导入实际上传包，确认编译、场景打开、点击拆除、胜负、撤销、提示及重开正常，并检查 Console。
- 如实际依赖 Unity Registry UPM 包，在提交使用的 manifest 中正确声明；如实际依赖其他 Asset Store 产品，在 Publisher Portal 中配置真实依赖。不因当前工程安装了测试框架或 Visual Studio 插件，就把它们列为客户运行所必需。
- 字体许可证已在当前本地 `Resources/Fonts` 找到；应确认旧上传包也带有对应许可证。不要只补一句授权说明而遗漏许可证文件。
- 现有营销截图和“20 个关卡”等描述也必须对应该旧包。不要以当前糖果 UI 截图替代旧版本截图。

## 本次证据与边界

- 早期范围：用户明确确认，以及既有 `docs/asset-store/listing-copy.md`。
- 编辑器基线：`ProjectSettings/ProjectVersion.txt` 与既有发布文案。
- 读取 `Builds/Preview/BreakDrop_Data/Managed/Assembly-CSharp.dll` 的程序集元数据（本地文件日期 2026-09-08），实际引用为 `netstandard`、`UnityEngine.CoreModule`、`UnityEngine.Physics2DModule`、`UnityEngine.TextRenderingModule`、`UnityEngine.IMGUIModule`、`UnityEngine.InputLegacyModule`、`UnityEngine.JSONSerializeModule`。该预览程序支持早期 IMGUI 依赖判断，但不等于对上传包的逐文件验证。
- 字体：`docs/validation/README.md`，以及 `Resources/Fonts/Outfit-OFL.txt`、`NotoSansSC-OFL.txt`。
- 早期版无音频声明保留；当前工程存在 `CandyFeedback.cs`，不能拿它反推旧包内容。
- 当前工程 `WeChatBuild.cs` 直接引用 WeChatWASM，因此“微信 SDK 可选且删除后不影响编译”不能用于当前完整工程。此依赖不因本次旧包文案补正而改变。

## 审核规则

- [Unity Submission Guidelines](https://assetstore.unity.com/publishing/submission-guidelines)：1.6.a–b 要求在 AI Description 披露具体工具、生成内容和后续工作；1.1.c 要求说明依赖并正确配置相关 manifest / Portal 依赖；3.1.a 要求描述准确。
- [Asset Store Content Transparency](https://unity.com/legal/asset-store-content-transparency)：1.2.4 涉及未标记的 AI 内容。

本次仅补正这两项反馈所需材料，不保证重新审核一定通过。
