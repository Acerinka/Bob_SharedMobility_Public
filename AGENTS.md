# AGENTS.md

本文件是 Bob Shared Mobility 项目的中文协作总纲，面向后续 AI Agent 和开发者。它不替代 `README.md` 或 `docs/`，而是把项目里最容易改错的设计逻辑、工程边界、调试入口和交付红线收束到一处。

如果你要修改这个仓库，先读本文件，再按需要继续读 `docs/RUNTIME_ARCHITECTURE.md`、`docs/UI_NAVIGATION_MODEL.md` 和 `docs/ENGINEERING_STANDARDS.md`。

## 项目定位

Bob Shared Mobility 是一个 Unity 车载 HMI / in-cabin assistant 原型。核心体验不是单个页面，而是 Bob 这个伴随式角色贯穿 onboarding、Dock 导航、地图、音量/应用面板、语音指令、车内场景提示和 lane-assist 交互。

维护时要把它当作“有状态的车载交互系统”，不是一堆可以随手开关的 UI GameObject。最重要的工程目标是：交互入口统一，状态可追踪，动画可取消，调试入口可审计，资源和密钥可交付。

## 技术栈与启动入口

- Unity 版本：`2022.3.53f1`。
- 渲染：Universal Render Pipeline `14.0.11`。
- 输入：Unity Input System `1.11.2`，同时保留部分项目输入封装。
- UI：TextMesh Pro `3.0.7`、UGUI、CanvasGroup 驱动的显示/隐藏。
- 动画：DOTween，Bob、地图、Dock、Liquid icon 都依赖 tween 生命周期管理。
- 主场景：`Assets/_Project/Scenes/BobSharedMobility.unity`。
- 项目拥有内容只放在 `Assets/_Project`。`Assets/Plugins`、`Assets/TextMesh Pro`、`Assets/Oculus` 等是外部/第三方或包内容。

## 优先阅读顺序

1. `README.md`：项目入口、Unity 版本、目录和交付状态。
2. `docs/RUNTIME_ARCHITECTURE.md`：运行时根对象、Bob 命令、地图状态和诊断入口。
3. `docs/UI_NAVIGATION_MODEL.md`：app-shell、route table、screen stack 的目标架构。
4. `docs/ENGINEERING_STANDARDS.md`：输入、指针、日志、UI、场景配置、Bob motion 的工程规则。
5. `docs/SETUP_AND_SECRETS.md`：OpenAI、本地麦克风、Wit/Meta token、GitHub Secrets。
6. `docs/DELIVERY_REGRESSION_CHECKLIST.md`：交付和回归检查。
7. `docs/PROJECT_INDUSTRIALIZATION_AUDIT.md`：工业化债务和后续推进方向。

## 目录职责

- `Assets/_Project/Source/Core`：跨功能基础设施，如输入、日志、相机查找、指针路由、Bob 总导演、onboarding、runtime diagnostics。
- `Assets/_Project/Source/Core/Navigation`：app-shell 导航、screen id、route table、modal/overlay、Dock 屏幕注册、CanvasGroup 展示工具。
- `Assets/_Project/Source/Modules/Bob`：Bob 自身的皮肤、飞行、动作、语音识别和运行时状态。
- `Assets/_Project/Source/Modules/Map`：地图 Small/Medium/Full 状态机、地图 surface、地图 fragment、手势输入、视图切换按钮。
- `Assets/_Project/Source/Modules/Dock`：Dock 按钮、面板、层级菜单、音量控制。
- `Assets/_Project/Source/Modules/LiquidMenu`：Liquid menu 的视觉和交互遗留模块。地图 surface 的最终运行时所有权不在这里。
- `Assets/_Project/Source/Modules/Visuals`：图标眼睛、Liquid icon、气候面板等视觉组件。
- `Assets/_Project/Source/UI`：可复用 UI 交互面，如 canvas interaction guard 和 lane control dialog。
- `Assets/_Project/Source/Editor`：项目治理、资源导入策略、UI 页面生产验证工具。
- `Assets/_Project/Art`：材质、Shader Graph、纹理和 RenderTexture。
- `Assets/_Project/Media`：语音音频和视频素材。
- `Assets/_Project/Settings`：URP 配置、profile、navigation route table。

## 运行时总架构

场景应围绕少数顶层根对象理解：

- `Project_Runtime`：运行时 profile、诊断、backdoor registry、导航服务。
- `AppNavigationService`：当前 screen、Dock panel、sub-panel、modal 和 world input block 状态。
- `RuntimeDiagnosticsHub`：所有开发快捷键、语音注入、状态日志、生产/开发开关的审计点。
- `System_OnboardingFlow`：冷启动 onboarding，负责首帧前隐藏 main app 和非当前 onboarding panel。
- `System_MainApp`：主 HMI surface 和功能模块。
- `System_VideoSources`：视频播放源。
- `Bob_Actor`：Bob 角色 rig 和材质/皮肤/飞行动画。
- `Main Camera` / `UI Camera`：渲染和 world-space canvas event camera。

新增功能时，先判断它属于全局导航、Bob 命令、地图状态、Dock 面板、UI 局部行为，还是诊断工具。不要把跨功能业务塞进一个 scene button 的 persistent call。

## Bob 交互原则

Bob 的目标请求统一走 `BobInteractionDirector.RequestTarget(...)`。`GoToTarget(...)` 只是兼容旧 boolean 调用的包装。新增功能如果需要 Bob 飞到某处、触发某个面板或执行 map target，应先创建/复用 `BobTarget`，然后交给 director。

必须遵守这些规则：

- 不要从 feature code 直接 `DOKill` Bob transform。
- 不要在 feature code 中直接移动 Bob 到业务位置。
- 不要绕过 `BobInteractionDirector` 自己写飞行队列。
- 不要复制 `"Map"`、`"Mapfull"` 这种裸字符串；代码引用走 `BobTargetIds`。
- Bob 被 icon、Dock 或 feature 暂时接管时，release 必须携带 interaction token。
- 被取消或打断的旧 feature callback 必须被 token 拦住，不允许把 Bob 拉回旧位置。
- Bob 的 home anchor 由 `BobController.HomeWorldPosition` 表示，remote workspace transition 不应重写它。

`Mapfull` 和 `Map` 是 runtime-critical target。`7 -> 8`、`4 -> 8`、`volume -> Mapfull` 这类链路必须能打断旧 Bob/icon/Dock 回调，不能让旧动画完成后反向污染当前状态。

## 导航原则

项目正在从 scene-local button wiring 迁向 app-shell 导航模型。新增导航必须优先走：

- `AppScreenId`：给页面或 panel 一个明确身份。
- `AppNavigationRouteTable`：声明 route、layer、presentation mode、production status。
- `AppNavigationService`：打开 screen、Dock panel、modal、overlay。
- `AppNavigationButton`：新通用 UI 按钮使用明确 command，而不是直接调用 feature method。
- `CanvasGroupPresenter`：统一移动 `alpha`、`blocksRaycasts`、`interactable`。

旧的 scene persistent calls 仍然存在，但它们只应是兼容入口。`DockPanelController.OpenSpecificLevel3`、`BackToLevel2`、`OpenLevel2Menu`、`CloseEntireApp` 等外部入口必须回到 `AppNavigationService` / `DockNavigationManager`，只有内部 `Apply...` 方法可以直接改变 Dock 状态。

Dock 有两个语义：

- ensure-open：确保某个 panel 打开。
- toggle：相同目标第二次触发时关闭回 shell。

底部 Dock 按钮和 Bob dock shortcut 应使用 toggle 语义；业务代码如果只是确保 panel 可见，使用 open 语义。

## 地图原则

`MapViewController` 是地图状态机的唯一运行时所有者。它拥有：

- `Small_Icon`
- `Medium_Screen`
- `Full_Screen`
- requested state：`currentState`
- settled state：`SettledState`
- transition flag：`IsTransitioning`
- visible surface debug：`VisibleSurfaceDebug`
- queued state debug：`QueuedStateDebug`

地图改动的红线：

- 只允许一个 map surface 在状态切换中可见，除非明确关闭 `enforceExclusiveSurfaceOwnership` 并说明原因。
- Home/Work、route card、distance label 等 state-owned fragment 由 `MapFragmentVisibilityPresenter` 通过 `MapViewController` 管理。
- 不要让 fragment 自己随意 `SetActive`，否则会和 map transition 时间错位。
- 地图动画节奏从 `MapViewController > Visibility Timing` 调，关键字段包括 `configVisibilityTransitionDelay`、`configVisibilityFadeDuration`、`configHiddenScaleMultiplier`。
- 遗留 `LiquidMenuItem` 可以保留视觉和 authored shape，但地图 Small/Medium/Full 的最终状态所有权属于 `MapViewController`。

如果出现 Bob 消失、7/8 重叠、Mapfull 回退丢失、旧地图 surface 复活，优先检查：active tween 是否被 kill、queued state 是否清掉、LiquidMenu delayed auto-expand 是否被取消、Bob interaction token 是否过期。

## 输入、指针与诊断

产品输入入口：

- 键盘和鼠标封装走 `ProjectInput`。
- gamepad 映射走 `GamepadButtonReader`。
- UI 交互走 Unity `EventSystem` 和 `GraphicRaycaster`。
- world-space collider 交互走 `SceneWorldPointerRouter`。

不要在 feature script 里直接读 `Input.Get...`、`Keyboard.current`、`Gamepad.current`。不要为了世界点击随手给 camera 加大范围 `PhysicsRaycaster`。UI 优先级必须清楚，前景 UI 不应被背景 collider 穿透。

诊断入口统一放在 `RuntimeDiagnosticsHub`。新增 debug shortcut、语音注入、gamepad logging、backdoor 都必须进入 `Backdoor Registry`，让 Development / Production profile 可以统一启停。不要新增散落在 feature script 里的常驻调试后门。

常用诊断：

- `RuntimeDiagnosticsHub > Backdoor Registry`
- `RuntimeDiagnosticsHub > Voice Command Diagnostics`
- `Backdoor/Inject Voice Command`
- `Diagnostics/Validate Runtime Wiring`
- `Diagnostics/Log Runtime State`
- `BobInteractionDirector > Diagnostics/Validate Registered Targets`
- `BobInteractionDirector > Runtime Snapshot (Read Only)`
- `MapViewController > Diagnostics/Validate Map Runtime State`

## 语音、密钥与外部服务

基础 demo 不需要外部 API key。真实麦克风转写是可选能力。

- 语音脚本：`VoiceCommandRecognizer`。
- 场景对象：`System_VoiceCommand`。
- OpenAI key 优先使用本地环境变量 `OPENAI_API_KEY`。
- 一次性本地测试可以填 Inspector 的 `Api Key`，但提交前必须还原。
- 无网络或无 key 时，用 `RuntimeDiagnosticsHub` 的 voice command injection 调试 Bob command。
- `ProjectSettings/wit.config` 中 `serverToken` 必须保持空值。
- 不要提交 `.env`、Unity license、Wit/Meta token、真实 API key、debug WAV。

任何曾经进入公开 Git 历史的 token 都应视为暴露，需要在服务后台 rotate 或 revoke。

## 资源与命名

项目资产必须保持在 `Assets/_Project` 下，第三方和包内容不要搬进 `_Project`。

命名前缀：

- `PF_`：prefab。
- `MODEL_`：模型导入。
- `TEX_`：texture 或 sprite。
- `MAT_`：material。
- `SG_`：Shader Graph。
- `RT_`：RenderTexture。
- `VO_`：voice-over audio。
- `VID_`：video。
- `REF_`：design/reference image。

资源操作红线：

- 移动、重命名、删除 Unity asset 时必须保留 `.meta`。
- 大 PNG 超过 8 MB 需要 review。
- 视频超过 50 MB 需要 encoding review 和 Unity playback validation。
- `VID_BobCompressed.mp4` 已经过 H.264 Constrained Baseline 方向治理。若再次出现 Unity timestamp/profile warning，按 `docs/MEDIA_ASSET_GOVERNANCE.md` 重新编码，不要忽略。
- baked PNG screen 和 prototype image 仍是迁移债务，生产化页面应逐步组件化。

## 代码与架构红线

请保持 C# 文件小而清楚。GitHub static governance 当前以 450 行作为强制 guardrail。超过这个规模时，优先拆 partial、拆 service、拆 presenter，或明确记录例外。

不要做这些事：

- 新增跨功能直接 `GameObject.SetActive` 导航。
- 让 scene persistent call 成为业务唯一入口。
- 在 feature script 中直接查找全局对象，除非它是明确的 core bootstrap/resolver。
- 复制 raw Bob target literal。
- 直接读 Unity 全局输入设备。
- 在多个脚本里各自实现 panel show/hide。
- 在多个脚本里各自管理地图 surface。
- 把 debug shortcut 写在功能脚本里绕过 `RuntimeDiagnosticsHub`。
- 把本地 credential、license、debug audio 或个人环境信息写进仓库。

应该优先做这些事：

- 用显式 serialized references。
- 用 `ProjectLog` 输出可行动 warning/error。
- 用 `AppNavigationService` 表达全局导航。
- 用 `CanvasGroupPresenter` 表达 panel 可见性。
- 用 `BobInteractionDirector` 表达 Bob 目标和命令结果。
- 用 context menu diagnostics 让现场可审计。

## 已知债务与正确方向

这个项目已经经过一轮工业化整理，但还不是“所有东西都完全组件化”的状态。后续继续推进时，优先方向是：

- 把更多 scene persistent calls 迁到 route table、service、`AppNavigationButton`。
- 把 baked texture UI 转为组件化 UI，避免截图式页面阻碍交互和适配。
- 把 map / Dock / onboarding 的业务状态继续集中到明确 owner。
- 为 Unity Play Mode 补可自动化的关键回归。
- 对 voice、media、navigation、Bob motion 继续增加可审计 diagnostics。

不要用“能跑就行”的方式扩大现有债务。若必须保留兼容入口，要把它包成 request wrapper，并确保最终状态仍由 service 或 controller 统一拥有。

## 验证清单

修改后至少做以下检查：

- `git diff --check`
- 确认没有新增真实 secret：OpenAI key、Wit/Meta token、Unity license、`.env`、debug WAV。
- 确认新增 `.cs` 有 `.meta`。
- 确认 C# 文件未超过项目 guardrail。
- 确认资产命名前缀符合 `docs/ASSET_NAMING.md`。
- Unity 打开 `Assets/_Project/Scenes/BobSharedMobility.unity`。
- Play Mode 跑 `docs/DELIVERY_REGRESSION_CHECKLIST.md`。
- Bob 关键回归：`7 -> 8`、`4 -> 8`、`8 -> 7`、重复 `7`、重复 `8`。
- Dock 关键回归：Volume/Apps/Settings 等首次打开，第二次同目标关闭。
- Map 关键回归：Small/Medium/Full 不重叠，fragment 动画和 map transition 同步。
- Voice 关键回归：无 key 时用 injection 可触发命令，有 key 时麦克风路径不把 key 写入 scene。

## 给后续 Agent 的工作方式

先读，再改。先找 owner，再动状态。先用已有 service，再新增抽象。每一次看似小的 UI 动画修改，都可能碰到 Bob、Dock、Map、Navigation 四条链路，必须按链路验证。

如果用户反馈“Bob 丢失”“7/8 重叠”“面板没有回退”“地图动画不一致”，不要只调一个 duration。先检查命令调度、interaction token、DOTween kill、map state、Dock toggle 和 runtime snapshot。这个项目的 bug 往往不是单个对象位置错了，而是旧 owner 的回调还活着。

如果用户要求交付或上 GitHub，交付口径必须包含：清晰 commit、README/CHANGELOG、回归清单、secret 规则、媒体治理、静态检查。散落在工作区的改动不算交付。
