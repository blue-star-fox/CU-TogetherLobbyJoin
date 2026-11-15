<h1 align="center">TogetherLobbyJoin</h1>

<p align="center">
  <strong>按房间ID加入 Together（CasualtiesMP）多人联机房间</strong><br>
  在联机面板补上「房间ID 输入框 + 粘贴 + 加入房间」，加入前先看清房间信息再决定。
</p>

<p align="center">
  <img alt="BepInEx" src="https://img.shields.io/badge/BepInEx-5.4.x-2ea44f">
  <img alt="Game" src="https://img.shields.io/badge/Casualties%20Unknown-1.0.1-4c8eda">
  <img alt="Together" src="https://img.shields.io/badge/Together-5.5.0-e08b3c">
  <img alt="License" src="https://img.shields.io/badge/license-MPL--2.0-blueviolet">
</p>

<p align="center">
  <a href="https://store.steampowered.com/app/4576490/"><b>未知伤亡 · Steam 商店</b></a>
  &nbsp;·&nbsp;
  <a href="https://github.com/creaturefeaturelarry/casualties-together"><b>Together 多人联机 MOD</b></a>
</p>

> [!WARNING]
> **这不是安全绕过程序**
> 它只做两件事：在联机面板加一行输入框、加入前读一次 Steam 大厅的公开元数据（房间名 / 人数 / 版本 / 是否有密码）。
> 没有磁盘遍历、没有联网上传、没有额外进程。看到 “DLL” 就担心扫盘的问题，不用再问了。

> [!NOTE]
> **Steam 大厅按 AppID 隔离**：正式版 `4576490` 与 试玩版 `4576510` 的大厅互不可见。
> 两边 AppID 不同时，Steam 只会回答 `Lobby does not exist` —— 这是 Steam 的规则，与本插件无关。

---

## 快速开始

| 步骤 | 做什么 |
| :--: | --- |
| 1 | 装好 **BepInEx 5.4.x** 和 **Together (CasualtiesMP) 5.5.0** |
| 2 | 把 `TogetherLobbyJoin.dll` 放进 `BepInEx\plugins\Together\` |
| 3 | 进游戏 → 多人联机 → Steam 页 → 填房间ID → **加入房间** |

> [!TIP]
> 想稳定联机：双方用**同一个游戏版本**（看日志里的 `steam appid=`）、同一个 MOD 版本；房主用**公开**大厅。

---

## 界面与流程

点「加入房间」后分三步，前两步失败会当场告诉你原因，不会让你进 0.1 秒又被踢回菜单：

```text
① 校验房间ID   → 格式 / 是否 Steam 大厅 / Steam 上是否真的存在
② 读取房间信息 → 房间名 · 人数 · 版本 · 是否需要密码 · 是否锁中途加入
③ 弹窗确认     → 需要密码就在这里填；你点「加入房间」才真正加入
```

```text
房间信息
  房间ID:   109775244491083111
  房间名:   测试房间
  人数:     3 / 8
  版本:     1.0.1_MPv5.5.0   ✔
  需要密码: 需要
  密码:     [**************]
  [加入房间]   [取消]                 by Bilibili UID:1951065665
```

- 弹窗用 MOD 自己的九宫格皮肤（`uiBlock` 9-slice）、同字号、同按钮，标题条可拖动；
- 需要密码却没填时，「加入房间」是灰的；确认后密码写入 `Multiplayer.INPUT_PASSWORD` 随握手包发出；
- 版本不一致 / 锁中途加入 / 满员 → 橙色警告，**加不加入由你决定**；
- 「取消」、右上角 `X`、`ESC`、或窗口放着 3 分钟不动，都会丢弃。

> [!NOTE]
> 输入行**只在“未连接任何房间”时出现**；房主开房 / 已连接 / 连接中，整行隐藏。

---

## 配置

`BepInEx\config\casualties.together.lobbyjoin.cfg`

| 键 | 默认 | 说明 |
| --- | :--: | --- |
| `EnableJoinRow` | `true` | 是否显示面板里的这一行 |
| `MaxIdLength` | `96` | 输入框最大字符数 |
| `LastRoomId` | `空` | 上次输入的房间ID（自动保存） |
| `ForceJoin` | `false` | 跳过加入前的版本 / 密码 / 人数预检 |
| `ToggleWindowKey` | `F8` | 备用输入小窗快捷键 |
| `FontScale` | `1` | 这一行的文字倍率（0.5 ~ 3） |
| `Language` | `Auto` | `Auto` 跟随 MOD 界面语言；也可 `zh-CN` / `en` |

---

## 兼容性与限制

| 项目 | 要求 |
| --- | --- |
| 游戏 | [未知伤亡 / Casualties Unknown](https://store.steampowered.com/app/4576490/) `1.0.1`（正式版）（**双方appid要一致**） |
| 联机 MOD | [Together (CasualtiesMP)](https://github.com/creaturefeaturelarry/casualties-together) 测试版本：`5.5.0` |
| 加载器 | [BepInEx](https://github.com/BepInEx/BepInEx/) 使用版本`5.4.x`|

> [!CAUTION]
> 插件用 Harmony 挂在 MOD 的**内部方法**上。MOD 大版本更新后可能失效——先跑 `tools\verify`（见下文），
> 通过说明接口没变，不通过就看日志里 `[JoinByRoomId]` 的提示。

> [!IMPORTANT]
> 大厅类型决定谁能进：`公开` 任何人凭 ID 可进 · `仅好友` 只有 Steam 好友可进 · `私人` 无法通过 ID 加入，~~需要好友发一遍邀请，但都发邀请了，还有必要吗？~~。
> 另外 MOD 会比对 `COOP_MOD_VERSION`，版本不同会被服务器踢，插件只能提前警告。

---

## 常见问题

| 现象 | 原因 / 处理 |
| --- | --- |
| 面板里没有这一行 | 看日志 `rowPatch=`；为 `False` 时插件会自动挂面板级兜底并打开 `F8` 小窗 |
| 提示“Steam 上找不到房间” | 房主已关房 / ID 是旧的 / 两边 AppID 不同 / 大厅是私人或仅好友 |
| 点了加入仍被踢 | 看状态栏或日志 `mod status: STEAM: Kicked: ...`：版本、密码、或房主开了“强制模组列表” |
| `Lobby does not exist` | 同上，Steam 层面看不到这个大厅；先比对两边日志的 `steam appid=` |
| 字太小 | 配置里把 `FontScale` 调到 `1.2 ~ 1.5`（只放大这一行） |
| MOD 更新后没反应 | 跑一次 `tools\verify`；接口变了日志会写清楚 |

---

## 工作原理

<details>
<summary>展开：挂点、排版、加入路径与兼容处理</summary>

<br>

**UI 挂点**　Harmony `Postfix` 挂在
`Together.UIMainMenu._GUI___DirectConnect_DoSteamInfoo(Rect, float, float, float)` 之后 —— 此处仍在面板的
`GUILayout` 竖向组里，追加一行就是原生排版；补丁按参数名注入，尺寸直接沿用 MOD 算好的列宽/行高。

**绝对排版**　向面板申请一整块矩形（`GUILayoutUtility.GetRect`），块内所有控件用绝对 `Rect` 画；
字号取 `UIBullshit.GetCurFontSize()` 与 `GUI.skin.label/button.fontSize` 的较大者，宽度用 `CalcSize` 实测，
面板太窄自动折成两行 —— 因此不会和 MOD 的控件重叠，也不会出现“字太小”。

**房间信息窗口**　无边框 `GUI.Window`（`GUIContent.none + GUIStyle.none`）承载，保证盖在 MOD 的 IMGUI 之上；
内容全部用 MOD 的 `UIBullshit._GUI_9SlicePanel(..., unscaled_uiBlockNano)` 与同一套 `label / textField / button` 绘制。

**加入路径**　`SteamMatchmaking.RequestLobbyData` 预读元数据 → 确认后 `TransportSteamworks.OnWantToJoinLobby(id)`，
与“Steam 好友邀请 / 启动参数 `+connect_lobby`”完全同一条原生路径（密码、版本、MOD 列表、语音、存档同步都不变）。

**不干扰兼容性**　Postfix `Multiplayer.GetBepinPlugins()` 与 `GetModGUIDs()`，把自己从“模组列表”里摘掉，
避免房主开“强制模组列表”时因为多了个纯 UI 插件而被踢。

**诊断**　Prefix `KSteam.LeaveLobbyAndResetNet` 打调用栈、Postfix `TransportSteamworks.CreateClientSocket` 打结果、
轮询 `Multiplayer.multiplayerStatusMessage` 记录每条状态；排查时看 `BepInEx\LogOutput.log` 里的 `[JoinByRoomId]` 即可。

</details>

---

## 构建

需要 **.NET SDK 8+** 和游戏本体（提供 `Assembly-CSharp.dll`、`UnityEngine.*`、`BepInEx\core`、`CasualtiesMP.dll`）。

```powershell
# 自动在 Steam 库里找游戏
pwsh -File .\build.ps1

# 指定目录 / 只编译不部署 / 跳过校验
pwsh -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Casualties Unknown" -NoDeploy -SkipVerify
```

产物：`dist\TogetherLobbyJoin.dll`。

> [!TIP]
> `build.ps1` 会顺带跑一次反射校验（`tools\verify`），不启动游戏就能确认 MOD 的内部成员与 Harmony 参数名是否还对得上。
> 单独运行：`dotnet run -c Release -- "<游戏目录>" "..\..\dist\TogetherLobbyJoin.dll"`，输出 `ALL CHECKS PASSED` 即兼容。

---

## 目录结构

```text
build.ps1                       一键编译（自动找游戏 / 校验 / 部署）
src/TogetherLobbyJoin/
  TogetherLobbyJoin.csproj      netstandard2.1 工程
  LobbyJoinPlugin.cs            BepInEx 入口、Harmony 补丁、配置
  LobbyJoinService.cs           预检 → 信息抓取 → 确认后加入
  LobbyJoinUi.cs                面板行 + 房间信息窗口（9-slice）
  LobbyIdParser.cs              房间ID / 邀请链接解析
  Loc.cs                        中英文案（跟随 MOD 语言）
tools/verify/                   反射校验器
docs/                           截图
```

---

## 许可

本项目以 **Mozilla Public License 2.0** 授权，全文见 [LICENSE](LICENSE)。

```text
Copyright (c) 2026 Bilibili UID:1951065665
https://space.bilibili.com/1951065665
```

<p align="center">
  <strong>作者</strong>　Bilibili UID <strong>1951065665</strong>　·　一只毛茸茸的小狐狸
</p>
