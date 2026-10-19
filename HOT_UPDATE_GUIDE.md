# 热更新发布说明

当前首发安装包版本为 `1.0.0`。游戏会先使用安装包内的资源和 Lua，因此断网、服务器关闭、玩家拒绝更新时仍能正常游玩。

## 可热更新范围

- `Assets/Lua` 中的数值规则、教程文字和业务修复
- Addressables 管理的图片、音频、配置、预制件和 UI
- `WoguaHotfixConfig.cs` 白名单中的 Core、UI、排行榜、教程业务方法

启动器、下载器、补丁校验、Lua 运行时和原生插件属于稳定层。修改这些内容后需要重新打 APK。

## 只修改 Lua

1. 修改 `Assets/Lua` 中的脚本。
2. 把 `Server/content-version.txt` 改成更高版本，例如 `1.0.1`。
3. 在 Unity 中执行 `Tools > Wogua Shooter > Hot Update > Publish Lua Only`。
4. 打开 `Server/Public/channels/stable-v2/android.json`，确认版本号、文件 URL 和 SHA-256 已更新。
5. 用旧 APK 启动游戏，选择更新，日志应出现 `HOTUPDATE_ACTIVATED` 和 `XLUA_HOTFIX_APPLIED`。

## 修改 Addressables 资源

1. 修改已有 Addressables 资源，或者把新资源放入 `Remote Content` 分组。
2. 把 `Server/content-version.txt` 改成更高版本。
3. 执行 `Tools > Wogua Shooter > Hot Update > Build Content Update`。
4. 工具会使用当前 APK 的基线状态生成更新，把产物复制到 `Server/Public/addressables/Android`，并重写 v2 清单。
5. 使用旧 APK 完成一次真机更新，重新启动后再次确认更新仍有效。

工具构建期间会临时移动被修改的本地资源，完成后会放回原分组。构建异常也会执行恢复，避免破坏首包的离线资源配置。

## 发布新 APK 基线

1. 修改 C# 稳定层或新增需要注入的 C# 类型后，执行 `Tools > Wogua Shooter > Prepare xLua Hotfix`。
2. 等待 Unity 编译完成，再执行 `Tools > Wogua Shooter > Validate xLua Hotfix`。
3. 把 `Server/content-version.txt` 改成与新 APK 的 `Version` 相同。
4. 执行 `Tools > Wogua Shooter > Build Android APK`。
5. 构建会自动生成 Addressables、归档 `addressables_content_state.bin`、发布服务器文件，再生成 APK。

完整构建日志必须包含：

```text
Assembly-CSharp.dll inject finish!
ANDROID_BUILD_PASS
```

若出现 `inject Exception`、`error CS` 或 `Build Failed`，不要发布该 APK。

## 真机验收

1. 关闭本机服务器或手机断网，确认游戏能进入主界面并开始关卡。
2. 启动 `Tools/StartPublicUpdateServer.ps1`，确认 `https://update.xinyin.space/health.json` 返回成功。
3. 提升 `Server/content-version.txt`，发布一个能明显识别的 Lua 测试补丁。
4. 旧 APK 启动后应显示更新弹窗；选择暂不更新可以继续旧版本。
5. 再次启动并选择更新，确认新规则生效。
6. 删除或损坏测试补丁，确认客户端保留上一次可用版本，不会覆盖 last-good 内容。

本机关闭后公网地址仍存在，但源站无法响应。客户端会显示连接失败，然后继续使用本地版本。
