# 倭瓜射手大冒险

## 项目入口

- Unity 版本：`6000.3.18f1`
- 启动场景：`Assets/Scenes/Game.unity`
- 工程资源：项目中的场景、预制体和配置均为已生成资源，编辑后直接使用
- Windows 构建：`Tools/Wogua Shooter/Build Windows Player`

游戏所需的场景、图片、配置、Lua 默认脚本都会打进安装包。没有网络，或者本机更新服务器没有启动时，游戏仍会正常进入。

## 参数配置

玩法参数位于 `Assets/Configs/GameConfig.asset`，可在 Inspector 中调整：

- 普通豌豆、豌豆机枪、玉米大炮的射击间隔和子弹速度
- 玉米炮弹爆炸范围
- 背景初始滚动速度和加速度
- 敌人初始生成间隔、生成加速度、最小生成间隔

## 音频资源

项目内置一套原创程序合成音频，位于 `Assets/Resources/Audio`：

- `WoguaAdventure_BGM.wav`：可循环的冒险战斗背景音乐
- `SFX_PeaShot.wav`：普通豌豆射击
- `SFX_CornShot.wav`：玉米炮发射
- `SFX_EnemyHit.wav`：敌人命中
- `SFX_CornExplosion.wav`：玉米炮爆炸

`AudioManager` 会在运行时自动从 `Resources/Audio` 加载这些文件。需要重新生成或调整音色时，可运行：

```powershell
python Tools/GenerateAudioAssets.py
```

音频连接点已经写入武器发射、敌人击杀和玉米炮爆炸流程，不需要额外修改预制体引用。

你提供的 `sparkelcatta-lang/XSXB-Band` 地址目前返回 404，项目没有把它作为运行时依赖；如果之后恢复公开，可以将生成的 BGM 交给它做二次编曲或母带处理，再替换同名 WAV 文件即可。

## 更新配置

更新配置位于 `Assets/Configs/UpdateConfig.asset`。`Manifest Url` 要填写服务器电脑的局域网地址，例如：

```text
http://192.168.110.141:8000/version.json
```

电脑的局域网 IP 变化后，需要修改这里并重新构建游戏。

## 地图与碰撞体编辑

重新生成项目后，以下对象会成为独立预制件：

- `Assets/Generated/Prefabs/Map/GroundCollider.prefab`：地面 `BoxCollider2D`
- `Assets/Generated/Prefabs/Characters/Player.prefab`：主角、刚体、主角碰撞体、`GroundCheck`
- `Assets/Generated/Prefabs/EnemyGhost.prefab`：幽灵碰撞体
- `Assets/Generated/Prefabs/EnemySkeleton.prefab`：骷髅碰撞体
- `Assets/Generated/Prefabs/MachineGunPickup.prefab`：机枪道具触发器
- `Assets/Generated/Prefabs/CornPickup.prefab`：玉米道具触发器

要结合背景图片调地面，打开 `Assets/Generated/Prefabs/GameRoot.prefab` 的 Prefab Mode，在 Hierarchy 中选择 `GroundCollider`。此时背景图片和碰撞体会同时显示，打开 Inspector 的 Gizmos 后可以拖动 `BoxCollider2D` 的边缘。主角和敌人也可以直接打开各自预制件调节 `CapsuleCollider2D` 或 `BoxCollider2D`。

碰撞体编辑后执行 `Tools > Wogua Shooter > Build Addressables`，运行场景才会使用最新预制件。项目资源已完成生成，当前不提供会覆盖场景和预制体的重建入口；如需重新搭建工程，应从版本控制或备份恢复。

`GameConfig.asset` 中的 `Ground Y` 和 `Ground Height` 只用于首次生成地面预制件的默认值。之后建议直接在 `GroundCollider.prefab` 中编辑，这样可以对照背景精确调整。

## 本机更新服务器

项目使用电脑现有的 Python 3，不需要安装额外服务器软件。在 PowerShell 中运行：

```powershell
cd "D:\Users\unity\倭瓜射手大冒险"
powershell -ExecutionPolicy Bypass -File .\Tools\StartUpdateServer.ps1
```

停止服务器：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\StopUpdateServer.ps1
```

同一局域网中的其他设备需要能访问脚本输出的 `LAN` 地址。若访问失败，请允许 Python 通过 Windows 防火墙的专用网络。

## 发布 Lua 热更新

1. 修改 `Server/game_rules.lua.txt` 中的业务逻辑。
2. 将 `Server/version.json` 的 `version` 改成更高版本，例如从 `1.0.0` 改成 `1.0.1`。
3. 启动本机更新服务器。
4. 玩家启动游戏后会看到更新弹窗。选择 `LATER` 会继续当前版本；选择 `UPDATE` 会下载并立即加载新 Lua。

下载后的补丁保存在 `Application.persistentDataPath/HotUpdate/game_rules.lua.txt`。脚本下载失败或校验失败时不会覆盖可用版本。

`catalogUrl` 目前留空。需要发布新的 Addressables 图片或预制件时，可以填入远程 Content Catalog 地址；基础资源仍保留在游戏包内，离线启动不会依赖远程 Catalog。
