# BattleCardPointsMod · 战斗牌点数显示

用于《吉星派对》（Astral Party）的 CesiumLoader 模组，基于 CombatOddsMod 的战斗覆盖层实现。可与原战斗胜率助手同时使用。

## 安装

1. 下载本仓库 [Releases](https://github.com/Aperfish/BattleCardPointsMod/releases) 里的 `BattleCardPointsMod-<版本>.zip`（或固定地址 `BattleCardPointsMod.zip`）。
2. 解压到游戏 exe 目录下的 `AstralParty_ModLoader\mods\`，得到：

   ```text
   AstralParty_ModLoader\
     mods\
       BattleCardPointsMod\
         BattleCardPointsMod.dll
         BattleCardPointsMod.json
         BattleCardPointsMod.pdb   (可选，用于把日志里的堆栈还原成源码行号)
   ```

3. 启动游戏。已有旧版本时覆盖更新即可，保留原 `config.json` 可保留个人设置。

卸载时退出游戏，删除 `mods\BattleCardPointsMod\` 目录即可，不影响其他模组。

## 功能

- 战斗界面右侧显示双方实时攻防与已同步的判定骰点数。
- 显示服务器快照中每张战斗牌的具体点数，支持重复牌、0点和负数。
- 每张牌独占一行，长文本自动换行，文本区域随内容增高。
- 牌名去除 SDK 附加的数字编号，“最近使用”同样只保留牌名。
- 出牌后刷新，结算期间保留最后点数，战斗窗口关闭后隐藏。

单张卡牌点数读取自 `BattleRole.CardCombatBonus.CardBonus`。服务器尚未下发时显示“等待服务器同步”，不会把尚未结算的随机范围当作实际点数。

## 运行环境与配置

目标加载器：CesiumLoader 2.9.1。目标框架：.NET Standard 2.0。

编辑模组目录下的 `config.json`，或使用工具箱提供的模组配置表单。

| 配置 | 默认值 | 作用 |
| --- | --- | --- |
| Enabled | true | 总开关 |
| ShowForOthers | true | 显示旁观战斗 |
| ShowCardDetails | true | 显示每张牌的点数 |
| FontSize | 28 | 字号，14～60 |
| Anchor | right-center | 面板位置 |
| OffsetX / OffsetY | 0 | 水平、垂直位置偏移 |

Anchor 支持 `right-center`、`left-center`、`top-left`、`top-center`、`top-right`。

## 从源码构建

编译期引用的 `CesiumLoader.SDK.dll` 与游戏程序集不随仓库分发，需要先准备：

```powershell
pwsh -NoProfile -File tools\fetch-refs.ps1
```

脚本默认从 CesiumLoader 仓库与 Release 拉取，并按 `sources.json` 校验游戏程序集的 SHA256。已经在本机 clone 了 CesiumLoader 时，可以离线直接复制：

```powershell
pwsh -NoProfile -File tools\fetch-refs.ps1 -Source C:\path\to\CesiumLoader
```

随后编译：

```powershell
dotnet build ./source/BattleCardPointsMod.csproj -c Release
```

产物在 `source\bin\Release\netstandard2.0\`，取 `BattleCardPointsMod.dll`、`BattleCardPointsMod.json`（以及可选的 `.pdb`）放进 `mods\BattleCardPointsMod\` 即可。自备游戏程序集时可用 `CESIUM_GAME_REFS` 环境变量或 `-p:GameRefsDir=` 指定目录。

## 发布

推送 `v*` 标签会触发 GitHub Actions：准备引用程序集、编译、打包成含 `BattleCardPointsMod\` 目录的 zip，并创建对应版本的 Release。标签版本必须与 `BattleCardPointsMod.json` 的 `version` 一致。

## 许可证与来源

采用 [GNU AGPL v3](LICENSE)。覆盖层实现改编自 [CesiumLoader / CombatOddsMod](https://github.com/higashitaniyume/CesiumLoader)，完整来源说明见 [NOTICE.md](NOTICE.md)。
