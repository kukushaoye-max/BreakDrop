# 设置面板设计稿

新增独立单张：01-settings-home.png。旧设置稿保留，不覆盖。

- 背景音乐、游戏音效、震动反馈独立开关；图示分别为开、开、关，不代表已实现默认配置。
- 绿色＋右滑块＋“开”；灰色＋左滑块＋“关”。灰色是关闭状态，不是不可点击。
- 切换即时生效并自动保存；完成和 X 都关闭面板，不撤销设置。以上为交互设计规格，未修改运行时代码。
- 首页背景沿用带体力入口的新版，压暗并轻微虚化。遮罩阻止点击底层；若以后用于局内，应保留关卡背景并暂停物理和计时。
- 不添加收费、充值、广告关闭或购买入口。

## 生成记录

采用 imagegen 技能与内置 image_gen 图像编辑。原有设置稿为风格参考，candy-stamina-pages-v1/01-home.png 为首页背景参考。

Use case: compositing / ui-mockup. Image 1 is the old settings dialog STYLE AND EDIT reference; replace its pause-like content with real settings. Image 2 is the exact current HOME BACKGROUND to use, preserving pink Chinese title 糖果拆拆乐, candy landscape, top-left heart 6/10 plus stamina capsule, top-right gear and bottom home buttons. Create ONE portrait full-screen candy-game settings dialog mockup, not a sheet. Dim and slightly blur the home background with a uniform dark plum translucent overlay, keeping top stamina visible. Center a bright vanilla-frosted biscuit modal at about 84% screen width and 58% screen height; caramel edges, restrained rainbow sprinkles, matching reference polish, plenty of breathing space. Title '设置' with small chocolate gear. Round cookie X close button at upper-right edge of modal. Three neatly aligned spacious horizontal rows with icons at left, exact Chinese labels at center-left, candy toggle switches at right: music-note icon '背景音乐' switch green with knob on RIGHT and text '开'; speaker icon '游戏音效' switch green with knob on RIGHT and text '开'; vibrating-phone icon '震动反馈' switch gray with knob on LEFT and text '关'. Small muted chocolate text under rows '设置自动保存'. Bottom large strawberry-pink jelly button exact '完成'. Remove old 观察支撑关系，选择拆除顺序, 继续 and 返回首页 from the dialog. No other rows, no purchases, no ad-off toggle, no currencies, no privacy claims, no phone frame, no watermark. All Chinese must be legible correct strokes. Single image and single dialog; background remains recognizable but subdued. Keep all three switch tracks consistent with clearly visible contrasting cream knobs.

