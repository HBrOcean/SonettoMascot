# SonettoMascot

一只住在桌面上的 Q 版桌面宠物。透明无边框、始终置顶、随意拖动，单击会播动作，支持多套皮肤切换和全局快捷键。

| | |
|---|---|
| **技术栈** | .NET 10 · Avalonia 11.3 · spine-csharp 4.2 · SkiaSharp · SharpHook |
| **平台** | Windows（完整功能）· macOS / Linux 可运行，窗口特效与开机自启仅 Windows 生效 |

## 下载

到 [Releases](https://github.com/HBrOcean/SonettoMascot/releases) 下载 `SonettoMascot-win-x64.zip`，解压后双击 `SonettoMascot.exe` 即可。

自包含单文件，**不需要另外安装 .NET 运行时**。

## 特性

- 30fps 实时渲染 Spine 动画，透明背景，不占任务栏
- 启动时先播一遍入场动画，再自然转入待机（换皮肤时不播 `born`，避免"飞出去再飞回来"）
- 窗口尺寸随动作幅度自动伸缩：动作大时长大，动作结束一秒后慢慢缩回
- 几套皮肤按「人物中轴 + 脚底」对齐，换皮肤时角色不会左右跳、也不会上下漂
- 拖动移动、位置记忆；**允许拖到屏幕外且不回弹**，靠托盘「重置位置」一键救回
- 单击播放动作（按配置或随机），播完自动回待机
- 定时随机动作：默认每 20 秒自己动一下，间隔 5~180 秒可调，可关闭；定时器与「点击动作」配置完全解耦，手动动作会重置计时
- 皮肤自动扫描，托盘与设置面板双入口切换，支持自定义显示名
- 全局快捷键播放指定动作，可自由绑定，格式错误会标红
- 单实例保护：重复启动会唤醒已有实例，不会开出第二只
- 完整的设置面板，改动即时生效

## 默认快捷键

全部为 `Ctrl + 键`，可在「设置 → 按键」里自由修改，支持 `Ctrl+Shift+A` 这类多修饰组合。

| 快捷键 | 动作 | 快捷键 | 动作 |
|---|---|---|---|
| `Ctrl + Y` | 打开设置面板 | `Ctrl + G` | giddy |
| `Ctrl + N` | 关闭设置面板 | `Ctrl + H` | hit |
| `Ctrl + 1` | skill1 | `Ctrl + I` | idle |
| `Ctrl + 2` | skill2 | `Ctrl + P` | posture |
| `Ctrl + D` | die | `Ctrl + S` | sleep |
| `Ctrl + Q` | unique | `Ctrl + B` | born |

> 全局钩子只监听、不拦截。所以在编辑器里按 `Ctrl+S` / `Ctrl+B` 会同时触发编辑器自身的功能，介意的话改成 `Ctrl+Shift+字母`。

## 皮肤

启动时扫描 `Assets/characters/` 下的每个子目录，哪个目录里凑齐了同名的 `.atlas` 和 `.skel`，就算一套可用皮肤。**没有任何硬编码的皮肤列表**：加目录就多一套，删目录就少一套。

预置四套十四行诗（Sonetto）的服装，来自 UTTU：

| ID | 名称 | 说明 |
|---|---|---|
| 302301 | 默认 | 菱形格纹发带 + 抱书（默认皮肤，永远排在第一位） |
| 302303 | 巡礼颂 | Parade Anthem |
| 302304 | 重叠的词句 | Emotion in Tranquility |
| 302306 | 诗的礼赞 | In Praise of Poetry |

### 显示名的三级兜底

卡片上显示什么名字，按这个顺序取：

1. **配置名** —— `config.json` 里的 `SkinNames`，设置 → 皮肤 → 「显示名」直接改，优先级最高
2. **display.txt** —— 皮肤目录里的单行文本，跟着模型走（分享给别人时一起带过去）
3. **目录名** —— 前两者都没有时的兜底

名字来自哪一级会写在卡片提示和「显示名」下方的说明里，不用猜。

### 添加自己的皮肤

1. 在 `Assets/characters/` 下新建子目录，目录名就是皮肤 ID（例如 `my_pet`）
2. 放入同名文件：`my_pet.atlas`、`my_pet.skel`，以及 atlas 里声明的图集
3. 想给它一个名字，放 `display.txt`（UTF-8，第一行即名字），或之后在设置面板里改
4. 打开设置 → **皮肤** → 点「刷新列表」

资源不齐的皮肤会显示成灰色并标注「资源缺失」，点不动，托盘菜单里同样置灰 —— 但不会从列表里消失，这样你能一眼看出少了什么。

设置面板里的预览图是运行时渲染出来的（图集本身是散件拼图，不能直接当缩略图）。首次渲染后缓存到配置目录，换模型后对不上时点「重建预览图」即可。

设置面板里的预览图是运行时渲染出来的（图集本身是散件拼图，不能直接当缩略图）。首次渲染后缓存到配置目录，换模型后对不上时点「重建预览图」即可。

## 配置

配置文件：`%AppData%\SonettoMascot\config.json`（macOS / Linux 为 `~/.config/SonettoMascot/`）

所有选项都能在设置面板里改，一般不需要手动编辑。改完重启生效。

日志同样在这个目录下的 `log.txt`，排查问题先看它。

## 从源码构建

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download)。

```bash
# Windows
build.bat

# macOS / Linux
chmod +x build.sh && ./build.sh

# 或者直接
dotnet run
```

打包成自包含单文件 exe：

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

推送到 GitHub 后，Actions 会自动在 `windows-latest` 上构建；打一个 `v*` 标签还会自动发布 Release：

```bash
git tag v0.1.0
git push origin v0.1.0
```

## 常见问题

**桌宠不见了**
按 `Ctrl + Y` 打开设置面板，或点托盘图标选「显示 / 隐藏桌宠」。如果是被拖到了屏幕外（程序允许这样，不会自动拉回），点托盘图标选「重置位置（拉回主屏右下角）」—— 托盘图标固定在任务栏，是唯一永远点得到的入口。

**快捷键没反应**
打开 `<AppData>/SonettoMascot/log.txt` 看是否有 `[Key] 收到 ...` 记录。没有记录说明按键被系统或其他软件拦截了；有记录但不触发，检查绑定格式是否为 `修饰键 + Vc键名`（如 `Ctrl+Vc1`），设置面板里格式错误会标红。

**换了皮肤，部分快捷键不动了**
不同角色的动画名不一定通用。按键绑定里失效的动作会变红，删掉重新选即可。

**开了鼠标穿透后拖不动了**
穿透模式下点击会落到下层窗口，角色本身收不到鼠标事件。关掉穿透用设置面板或托盘操作。

**想确认模型渲染是否正常**
在程序目录执行 `SonettoMascot.exe --dump`，会渲染一帧到系统临时目录并打印包围盒与锚点。`--dump 302306` 指定皮肤，`--dump-all` 逐个渲染全部可用皮肤。换肤后角色上下跳、左右跳时，先对比几套的 `Anchor` 值是否接近。

## 许可

代码部分为 MIT（见 [LICENSE](LICENSE)）。`Assets/` 下的角色素材不在 MIT 范围内，版权归原权利方所有，仅供个人非商业使用。

- Spine 运行时来自 [esotericsoftware/spine-runtimes](https://github.com/esotericsoftware/spine-runtimes)（spine-csharp 4.2），遵循 Spine Runtime License
- 角色素材来自 [UTTU Archive](https://uttu.merui.net/profiles/sonetto/) 的十四行诗（Sonetto）Q 版模型，版权归 Bluepoch / 《重返未来：1999》所有
