# Break & Drop — 糖果风完整 UI 设计交付 V1

日期：2026-10-06  
状态：当前原型范围的视觉设计交付；未接入 Unity。  
基准：已确认首页 / Gameplay 糖果风初稿；docs/GDD.md；BreakDropHud.cs 与 BreakDropGameManager.cs。

## 1. 文件与覆盖范围

| 文件 | 内容 |
|---|---|
| 01-home-gameplay.png | 已确认首页与游戏页，沿用原稿 |
| 02-level-select.png | 20 关选择页，4 列 × 5 行，当前关卡高亮 |
| 03-core-modals.png | 首页设置、游戏暂停、成功结算 |
| 04-failure-modals-corrected.png | 越界、超时、无可拆积木三类失败；最终采用此版 |
| 05-controls.png | 主次按钮、四态、图标、HUD、星星、关卡格、面板、遮罩和提示条 |
| 06-gameplay-states.png | 正常待操作、提示高亮、物理结算、低时间警告 |
| prompts.md | 原始提示词、参考路径及修正提示词 |

04-failure-modals.png 为修正前留档，不用于交付接入。修正仅统一超时页面顶部和弹窗内部的 00:00。

这些文件是高保真视觉画板，不是已切图的生产 Sprite、可编辑 Figma 文件或 Unity Prefab。未改动运行时代码、关卡 JSON、物理参数及旧美术；不生成 EXE、Development Build 或 PDF。整图不可直接代替可交互的游戏界面。

## 2. 视觉规范

- 主材质：草莓果冻主按钮、香草糖霜饼干次按钮、薄荷平台、青色六边形白星。
- 背景：糖果山丘与奶油云；游戏区减少装饰密度，必要时对背景单独降饱和、降对比，不能降低积木本身可读性。
- 建议落地色值：主粉 #FF3977；奶油 #FFF2D4；焦糖 #D88B40；正文可可 #4B210F；青色 #28D5E8；薄荷 #86DDB9；金黄 #FFC942；禁用灰紫 #B8ABBE。这是实现用色彩建议，不是生成图精确采样值。
- 图标始终有独立清晰轮廓；交互区域不得依赖柔光或阴影确定。
- 正文字体优先使用项目已有 Outfit Regular / Bold。标识可以烘焙为图片；按钮、数字与状态文案必须独立可编辑，不烘焙进底图。
- 所有用户界面文案为英文；中文仅用于本交付说明。
- 文字与背景需要实际接入后检查对比度，不能以美术稿代替小屏可读性验证。

## 3. 尺寸与布局（逻辑单位）

沿用项目 390 × 844 逻辑画布，以 Screen.safeArea 等比适配；本规范数值为接入目标，不是生成图像素测量。

| 项目 | 规格 |
|---|---|
| 页面左右边距 | 24；复杂面板内边距 20–24 |
| 顶部导航触区 | 48 × 48，图标 24–28 |
| 首页主按钮 | 326 × 64，次按钮 260 × 52，间距 16 |
| Gameplay HUD | 标题 156 × 38；计时 142 × 38；步数 100 × 24 |
| Gameplay 顶部区域 | y=20–128；不与世界可点击积木重叠 |
| Gameplay 世界区 | 建议 x=40–350，y=150–630，按现有关卡包围盒统一适配 |
| 操作提示条 | x=24，y≈670，宽342，高40，可容纳两行 |
| 底部工具 | 三个56 × 56按钮；中心x=68/195/322，y≈766；标签在按钮下 |
| 关卡选择 | 4列×5行；每格约72 × 64；行列间距10–12；20格同屏可见 |
| 设置/暂停面板 | 宽342，高约330，垂直居中；底部留安全区 |
| 成败面板 | 宽342，高约500–560；原因文本允许两行；三按钮不挤压 |
| 文字 | 页面标题24–28；弹窗标题28–32；按钮18–22；状态14–16；小标签12–14 |
| 最小点击范围 | 44 × 44；可见图标小于触区时扩大透明触区 |

页面图中夸张装饰与比例应按上述约束适配；不直接改变物理世界尺寸来迎合糖果装饰。现有 BlocksWorldInput 使用 y<96 / y>672 排除范围，接入扩高 HUD 后必须同步更新输入拦截；当前未修改。

## 4. 页面与动作映射

| 页面/弹窗 | 显示文案/入口 | 行为 |
|---|---|---|
| Home | PLAY | 现有 LoadSavedLevel |
| Home | LEVELS | 打开关卡选择 |
| Home | 齿轮 | 打开 SETTINGS |
| Level Select | 01–20 | 加载相应关卡；全部可进入，不增加锁关 |
| Level Select | X | 关闭选择页，返回原页面 |
| Gameplay | 左箭头 | 沿用立即返回首页，不新增确认弹窗 |
| Gameplay | 暂停图标 | 打开 PAUSED，冻结物理与倒计时 |
| SETTINGS | CONTINUE | 关闭设置并返回首页；保持现有菜单语义 |
| SETTINGS | BACK TO HOME | 关闭设置回首页，保留现有入口 |
| PAUSED | CONTINUE | 关闭弹窗，恢复物理与倒计时 |
| PAUSED | BACK TO HOME | 结束当前局并回首页 |
| YOU WIN! | NEXT LEVEL | 下一关；第20关沿用当前循环到第1关 |
| YOU WIN! | UNDO LAST MOVE | 有快照时允许回退，按现有能力保留 |
| TRY AGAIN | RETRY | 重开当前关卡，时间重置 |
| TRY AGAIN | UNDO LAST MOVE | 有快照且非超时失败时可用；超时置灰为本稿拟议调整 |
| 任意结算 | BACK TO HOME | 返回首页 |

不新增总通关奖励页、登录、音量滑块、震动开关、广告复活、货币、体力、商城或全屏教程。当前设置页没有真实音效设置能力，不能画出看似可用但没有实现的开关。Loading 为短暂内部状态，不单独增加加载页面或伪百分比进度条；Ready 初始化期间先禁用拆块，稳定后切换正常操作提示。

## 5. 状态与交互规范

| 状态 | 拆块 | Undo | Hint | Restart | 提示与显示 |
|---|---|---|---|---|---|
| Ready 初始化 | 禁用 | 按快照 | 禁用 | 可用 | 场景构建后等待稳定，不显示伪可操作指示 |
| Playing，未操作 | 可用 | 禁用 | 可用 | 可用 | Tap blocks. Land safely. |
| Playing，有快照 | 可用 | 可用 | 可用 | 可用 | 显示当前步数及倒计时 |
| 提示高亮 | 仍按Playing | 按快照 | 可用 | 可用 | Try the flashing block |
| Resolving | 禁用 | 按快照 | 禁用 | 可用 | Watch the blocks fall... |
| 长时间未稳定 | 仍禁用 | 按快照 | 禁用 | 可用 | Waiting for the tower to settle / Undo or restart |
| 无有效提示 | 可用 | 按快照 | 可用 | 可用 | Study the remaining supports, or undo and retry |
| 撤销成功 | 等Ready稳定 | 无下一次快照则禁用 | 按状态 | 可用 | Last move restored |
| 倒计时≤10秒 | 按当前状态 | 按当前状态 | 按当前状态 | 可用 | 草莓红计时框＋加粗边框＋时钟图标，不能只靠颜色 |
| Paused | 遮罩拦截 | 遮罩拦截 | 遮罩拦截 | 遮罩拦截 | 倒计时与物理冻结；仅弹窗按钮响应 |
| Won/Failed | 禁用 | 结算内按条件 | 不提供 | 失败用Retry | 遮罩拦截背景全部点击 |

超时禁用 Undo 是明确的待实现设计差异：当前 CanUndo 只检查快照和 Loading，没有排除时间归零。本设计不返还时间，也不引入复活规则；代码仍保持原样。

三星按现有下降高度反馈；通关为3星；失败显示当时0–3星，不依据步数另算。示意图中的1星/2星仅展示组件状态，不固定绑定失败原因。

## 6. 文案与结果

| ID | 英文 |
|---|---|
| title.home | BREAK & DROP |
| title.select | LEVEL SELECT |
| subtitle.select | 20 PHYSICS PUZZLES |
| title.settings | SETTINGS |
| title.paused | PAUSED |
| body.menu | Study the supports. Choose the right order. |
| title.win | YOU WIN! |
| body.win | SAFE LANDING |
| title.fail | TRY AGAIN |
| reason.bounds | The hex left the safe area |
| reason.timeout | Time's up! |
| reason.empty | No removable blocks remain |
| hud.level | LEVEL {level:00} |
| hud.moves | MOVES {moves:00} |
| hud.time | {minutes:00}:{seconds:00} |

动态值必须来自实时游戏状态；弹窗背景的关卡、倒计时、步数也同步实时状态，不能沿用画板示例的 LEVEL 08 / MOVES 00。失败背景保留失败发生时的真实布局，不重建示例塔。

## 7. 控件状态与动效

- Normal：糖霜上沿高光、下沿焦糖厚度、轻柔阴影。
- Pressed：UI按钮自身下移2单位，纵向视觉压缩至0.96，阴影变浅；松开回弹120–160ms。这里只作用于UI，不作用于物理积木。
- Hover（鼠标）：高光轻微增强，120ms；触屏不依赖Hover。
- Focus/Selected：外层奶油描边加可可细边，避免只靠颜色；关卡选中用粉色实心加描边。
- Disabled：灰紫/灰色去饱和，无亮光呼吸和按压反馈；保持可读，不响应点击。整套圆形和矩形按钮均复用同一规则。
- 弹窗：遮罩150ms；面板0.94→1.00缩放180ms，不添加连续抖动。开启即拦截背景操作，关闭后再恢复。
- 计时警告：边框轻呼吸，约1Hz，避免全屏红闪；数字不跳位。
- Hint：只给一个合法积木添加描边/光晕，不能缩放实体、改变碰撞体或自动移除。
- 拆块糖粒为非碰撞视觉碎片；控制数量与透明度，不能遮挡目标或改变承重。
- 暂停、成功、失败不可依靠Time.timeScale驱动界面动画，应使用不受缩放时间。
- 可读性优先，落地时应支持关闭高频闪动；不增加一个未实现的设置入口。

## 8. 组件和切图清单（下一阶段生产依据）

| 命名前缀 | 组件 | 输出原则 |
|---|---|---|
| bg_candy_home / bg_candy_game | 首页/游戏背景 | 无文字；中心低对比，装饰可延展 |
| logo_break_drop | 游戏标识 | 透明背景；独立于背景 |
| btn_jelly_primary | 主按钮底板 | normal/pressed/focus/disabled；不带字，可九宫格拉伸 |
| btn_cookie_secondary | 次按钮底板 | 同四态；文字独立 |
| btn_round_cookie / jelly | 圆形底板 | 顶部导航优先奶油；底部Undo粉、Hint黄、Restart青 |
| ic_back / close / settings / pause / play | 导航图标 | 透明独立符号，不混入背景 |
| ic_undo / hint / restart / home / clock | 功能图标 | 同统一描边粗细、光照方向 |
| panel_cookie_modal | 面板 | 无字；九宫格边框与面板分离 |
| scrim_cocoa | 模态遮罩 | 纯色可参数化，不必烘焙背景 |
| hud_level / timer / moves | 信息底板 | 数字独立；timer normal/warning |
| star_filled / star_empty / line_height | 高度进度 | 不遮挡积木；当前星数驱动 |
| tile_level | 关卡入口 | default/current/focus/pressed；无锁定态 |
| toast_cookie | 提示底板 | 支持一行和两行；无文字 |
| hex_candy / goal_mint | 六边形与目标平台 | 保留轮廓与碰撞关系，装饰不扩展可支撑面 |
| fx_hint_outline / fx_sugar / fx_win | 提示、拆除、通关 | 只做视觉，不参与物理 |

11种现有积木素材形状映射不变；糖果表面如果需要后续重绘，须按原素材实际占格蒙版制作。0/90/270/360度同时旋转美术与碰撞体，L形凹口透明，不能根据生成画板自动生成关卡碰撞。当前交付没有声称完成这些独立透明切图。

## 9. 检查结果与边界

- 已逐张目视检查：英文标题与关键按钮齐全；关卡01–20不缺号；三类失败原因分别显示；正常/按下/焦点/禁用四态可辨。
- 已修正超时画板背景顶部仍显示00:58的问题，最终版两处均00:00。
- UI kit导航粉色底为可选底板示意；实际页面顶部采用已确认的奶油导航样式，按上述组件规则统一。
- 生成图中的积木数量、塔形、下落轨迹不是已验证物理结果；不能直接复制为关卡数据。
- 当前为PNG设计画板和文字规范，未做Unity接入、切图、设备适配或可操作性验收。
- 无运行代码修改，因此未启动Unity编译，未打包EXE或Development Build。

## 10. 生成方式

使用 imagegen 技能，按已确认参考图统一材质、精确英文文案及状态约束，通过内置 image_gen 工具生成并进行一次定点修图。未使用CLI/API回退。提示词见 prompts.md。
