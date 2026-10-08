# 像素飞机大战 · PlaneWar

基于 Unity 开发的竖屏像素飞机空战 Demo，主要面向 **Android 手机端**。通过触屏拖动操控飞机，自动向上射击，躲避敌机和炮弹，拾取道具并挑战高分。

仓库包含完整 Unity 工程、原始制作要求和可安装的 Android APK。

## 下载与安装

**[下载 PlaneWar.apk](https://github.com/git-hdw/PlaneWar/raw/refs/heads/main/PlaneWar.apk)**（约 72.41 MiB）。也可在仓库根目录打开 `PlaneWar.apk`，点击 **Download raw file** 下载。

1. 将 APK 下载或传输到 Android 手机。
2. 打开文件，按系统提示允许当前浏览器或文件管理器安装应用。
3. 安装后启动游戏，点击“开始”，按住屏幕拖动飞机即可游玩。

开启 USB 调试后，也可通过 ADB 安装：

```sh
adb install -r PlaneWar.apk
```

| 项目 | APK 信息 |
| --- | --- |
| 版本 | 1.0 / versionCode 1 |
| 包名 | `com.planewar.demo` |
| 最低 Android API | 25（Android 7.1） |
| 目标 Android API | 36 |
| CPU 架构 | `arm64-v8a`（64 位 ARM） |
| 屏幕方向 | 竖屏（Portrait） |

以上信息来自随附 APK 的 Manifest 和原生库目录。最低系统要求对应关系见 [Android 官方文档](https://developer.android.com/guide/topics/manifest/uses-sdk-element)。当前 APK 面向支持 ARM64 应用的 Android 设备，实际运行效果需结合手机测试。

APK 的 SHA-256 校验值见 [APK-SHA256.txt](docs/APK-SHA256.txt)。

## 操作说明

| 平台 | 操作 |
| --- | --- |
| Android 手机 | 按住屏幕并拖动，飞机跟随触点移动；自动射击 |
| Unity 编辑器 / 桌面调试 | 鼠标左键拖动，或使用 WASD / 方向键移动 |
| 暂停与继续 | 点击右上角“暂停”，再点击“继续” |
| 重新开始 | 在暂停界面或结束界面点击“重开” |
| 键盘辅助操作 | Esc 切换暂停 / 继续 |

## 游戏内容

- **主角飞机**：初始 3 条生命，自动向上发射子弹；受伤后短暂无敌并闪烁。
- **飞机敌人**：从上方进入，向下移动并摆动，向下发射子弹；2 点生命，击败得 100 分。
- **坦克敌人**：固定位置的重型单位，朝玩家发射炮弹；6 点生命，击败得 300 分。
- **生命道具**：拾取后生命 +1，最多 6 条。
- **护盾道具**：拾取后无敌 3 秒，显示剩余时间和护盾效果。
- **炸弹道具**：拾取后消灭当前敌人，并清除敌方子弹。
- **完整流程**：开始 → 游戏进行 → 暂停 / 继续 → 游戏结束 → 重开。
- **计分**：实时显示生命和分数，结束时显示最终分数与本地保存的最高分。
- **表现**：像素素材、滚动背景、受击闪烁、缩放动画、爆炸、镜头震动、飘分和程序生成音效。
- **难度变化**：随着存活时间增加，提高敌人的生成和攻击节奏。

功能说明依据当前源码整理；制作要求原文见 [《像素飞机大战》制作要求](docs/《像素飞机大战》制作要求.md)。

## 手机端适配

- 使用 Unity Input System 读取触屏输入，支持单指拖动。
- UI 使用 `CanvasScaler / Scale With Screen Size`，参考分辨率为 **1080 × 1920**，按屏幕高度缩放。
- 正交相机根据屏幕宽高比计算可移动区域，飞机移动受边界限制。
- 随附 APK 的主 Activity 使用竖屏方向。
- 使用 Point 过滤的像素素材，关闭 Mipmap，保持像素边缘清晰。

当前 UI 未单独实现 `Screen.safeArea` 布局；刘海屏、挖孔屏、特殊宽高比及系统导航区域需要真机确认。适配说明描述已有实现，不代表所有机型均已验证。

## 打开 Unity 工程

1. 安装 **Unity 6000.3.23f1**（工程记录的编辑器版本）。
2. 通过 Unity Hub 添加仓库根目录，等待资源导入和包依赖解析。
3. 打开 `Assets/Scenes/SampleScene.unity`。
4. 点击 Play 运行；如需模拟手机尺寸，在 Game 视图选择竖屏比例，或使用 Device Simulator。

游戏通过 `RuntimeInitializeOnLoadMethod` 在场景加载后自动创建运行入口，UI、敌人、子弹和道具由代码生成，无需手动向场景添加 `Game` 组件。

## Android 打包

1. 在 Unity Hub 为对应编辑器安装 **Android Build Support、Android SDK & NDK Tools、OpenJDK**。
2. 打开 **File → Build Profiles**，创建或选择 Android 配置，并切换平台。
3. 确保构建场景包含 `Assets/Scenes/SampleScene.unity`。
4. 在 Player Settings 中确认当前配置：包名 `com.planewar.demo`、IL2CPP、ARM64、Minimum API Level 25、竖屏。
5. 选择 Build 输出 APK，或连接设备使用 Build And Run。

仓库中的 APK 是原项目提供的安装包，本次整理未重新打包。重新构建需要本机 Unity 和 Android 构建环境。

## 工程结构

```text
PlaneWar/
├── Assets/
│   ├── Scripts/Game.cs             # 游戏入口、UI、玩家、敌人、子弹和道具
│   ├── Editor/PixelArtImporter.cs  # 像素素材导入设置
│   ├── Resources/Art/             # 飞机、敌人、子弹、道具、地图和 UI 素材
│   ├── Scenes/SampleScene.unity    # 构建入口场景
│   └── Settings/                  # 渲染和输入相关资源
├── Packages/                     # 包清单及依赖锁定文件
├── ProjectSettings/              # Unity 工程与 Android 配置
├── docs/                         # 制作要求、验收清单及 APK 校验值
├── PlaneWar.apk                  # Android 安装包
├── .gitignore                    # 忽略 Unity 缓存及本机文件
└── README.md
```

运行时代码集中在 `Game.cs`：`Game` 管理状态、UI 和生成逻辑，`PlayerShip` 管理输入及生命效果，`EnemyCraft` 管理两类敌人，`Shot` 管理子弹，`Drop` 管理道具。

## 验证记录

提交整理时已检查：

- `Assets`、`Packages`、`ProjectSettings` 的副本与原文件逐个 SHA-256 比对一致。
- 所有资源及目录的 `.meta` 文件完整。
- 代码中 28 个唯一的 `Resources.Load` 美术路径均存在。
- APK ZIP 内容通过 CRC 检查，副本与原 APK 的 SHA-256 一致。
- 已核对 APK 的包名、版本、API 要求、CPU 架构和屏幕方向。

本次整理未运行 Unity 构建或 Android 真机测试。可按 [验收清单](docs/验收清单.md) 补充实际测试结果。

仓库不包含 `Library`、`Temp`、`Logs`、`UserSettings`、IDE 缓存、自动生成的工程文件或 Burst 调试符号；打开工程时由 Unity 重新生成。
