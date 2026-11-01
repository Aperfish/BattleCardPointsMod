# BattleCardPointsMod · 战斗牌点数显示

用于《吉星派对》（Astral Party）的 CesiumLoader 模组，基于 CombatOddsMod 的战斗覆盖层实现。可与原战斗胜率助手同时使用。

## 下载与安装

下载仓库中的 [BattleCardPointsMod-v1.0.1.zip](BattleCardPointsMod-v1.0.1.zip)，解压后，将整个 `BattleCardPointsMod` 文件夹放入游戏目录的 `AstralParty_ModLoader/mods/`。

最终文件位置应为 `AstralParty_ModLoader/mods/BattleCardPointsMod/BattleCardPointsMod.dll`。关闭游戏后安装或更新，然后重新启动。

已有旧版本时，覆盖 DLL 和 `BattleCardPointsMod.json` 即可，保留原 `config.json` 可保留个人设置。

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

需要 .NET SDK、加载器中的 `sdk/CesiumLoader.SDK.dll`，以及对应版本的游戏引用程序集：`AstralParty.Runtime.dll`、`UnityEngine.CoreModule.dll`、`UniTask.dll`、`Google.Protobuf.Runtime.dll`。游戏引用程序集不包含在本仓库中。

```powershell
dotnet build ./source/BattleCardPointsMod.csproj -c Release -p:LoaderDir="加载器目录" -p:GameRefsDir="游戏引用程序集目录"
```

本版本已编译通过，未运行自动测试；v1.0.1 的显示调整尚未经过实机验证。

## 许可证与来源

采用 [GNU AGPL v3](LICENSE)。覆盖层实现改编自 [CesiumLoader / CombatOddsMod](https://github.com/higashitaniyume/CesiumLoader)，完整来源说明见 [NOTICE.md](NOTICE.md)。