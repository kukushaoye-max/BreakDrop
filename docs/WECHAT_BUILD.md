# 微信小游戏导出

## 当前配置（2026-10-10）

- AppID：`wx49aa8a94c17a8cca`
- Unity：`2022.3.13f1c1`，竖屏，Release 导出。
- CDN：`https://a.unity.cn/client_api/v1/buckets/d486ff62-b391-46e7-8d89-f27e7a16e78f/content/`
- 微信开发者工具导入目录：`E:\Project\BreakDrop\Builds\WeChat\minigame`
- CDN 资源所在目录：`E:\Project\BreakDrop\Builds\WeChat\webgl`

SDK 配置与导出 game.js 均已使用上述 HTTPS 地址，不再依赖 localhost:8123。旧的 Start-LocalPreview.ps1 不适用于当前 CDN 配置。

## 本次验证

- Unity SDK 导出成功，日志包含 `[Converter] All done!`。
- 构建后编辑器状态：compiling=False，Console errors=0、warnings=0。
- AppID、CDN 与资源加载方式已核对；资源从 CDN 下载。
- 计时器兼容检查 26 项通过，屏幕适配器检查 10 项通过，game.js 语法检查通过。
- 此前 CDN 首资源包 HTTP HEAD 检查返回 **404**；本次背景修复后资源 hash 已更新，需上传本次新资源并确认下列 URL 可访问。尚未验证新版资源的 CDN 下载。
- 尚未进行本次微信开发者工具运行验收、真机测试或微信平台上传/审核/发布。

## CDN 资源

首资源包：`5deb169333ef5174.webgl.data.unityweb.bin.br`，66,100,952 字节。

预期下载地址：

`https://a.unity.cn/client_api/v1/buckets/d486ff62-b391-46e7-8d89-f27e7a16e78f/content/5deb169333ef5174.webgl.data.unityweb.bin.br`

代码压缩文件：`0eab506162124941.webgl.wasm.code.unityweb.wasm.br`，3,739,347 字节。

上传所需资源时保留相对于 webgl 目录的路径，包括所需 StreamingAssets。若云端给出的文件下载链接采用其他路径格式，应先核对其与 SDK 拼接的地址是否一致。HTTP 404 只说明当前地址不可用，不能据此认定只是缺少上传。

资源可访问后，在微信开发者工具重新编译 minigame 目录并验证；正式使用还需正确配置微信平台域名。当前导出保留本地调试设置 urlCheck:false，这不能替代平台域名配置。

## 再次构建

Unity 菜单：`Tools > Break & Drop > Build WeChat Mini Game`。

入口：`Assets/BreakDrop/Editor/WeChatBuild.cs`。构建保留 SDK 面板中的 CDN 配置，使用上述 AppID 与竖屏设置，覆盖 `Builds/WeChat` 输出；不会自动上传资源或发布小游戏。

SDK 配置：`Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset`。

## 手机屏幕适配

微信导出包保留胶囊、安全区与动态窗口尺寸同步修复。手机验收步骤见 `docs/validation/wechat-viewport/MOBILE_ACCEPTANCE.md`。计时器修复验证见 `docs/validation/wechat-timers/README.md`。

## 2026-10-10 背景直角修正（按用户澄清）

撤销外围补图方案，Canvas 中的四个 Background Edge 对象已移除。保留原图矩形范围与安全区布局，仅通过 SquareCornerArtwork 材质修正原背景四角的圆弧采样；不扩展或镜像页面，不移动按钮、不裁剪主体。PNG 源文件未重绘。材质已保存并绑定到 Canvas 预制体，编辑模式与运行模式一致。

屏幕比例与设计比例不一致时仍保留原有留边；本次仅修改背景四角，不用附加风景填充留边。

运行截图：`validation/candy-fidelity/home-after.png`、`validation/candy-fidelity/game-after.png`。真机仍待验证。

