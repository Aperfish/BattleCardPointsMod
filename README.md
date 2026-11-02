# BattleCardPointsMod · 战斗牌点数显示

用于《吉星派对》（Astral Party）的 CesiumLoader 模组。在战斗过程中，直接在卡牌上方展示点数信息（手牌上方显示预估点数范围，打出后显示服务器已同步的实际点数），并在战斗窗口两侧直观展示双方最终攻击与最终防御数值。

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

- **手牌点数预估**：在战斗手牌未打出时，每张战斗牌上方实时显示其可能投出的点数范围（如 `+1～3`、`+1～6`、`+3`、`+5` 等），攻击牌以红字展示、防御牌以蓝字展示；悬停放大或拖拽出牌时自适应跟随卡面缩放与旋转。
- **已出牌实际点数**：双方打出战斗牌后，在战斗窗口两侧（攻方在左、防方在右）展示对应卡牌，并在每张卡牌上方展示服务器已同步的实际点数（如 `+4`）。
- **最终数值展示**：在两侧卡牌上方分别显示双方的最终数值标头：
  - 攻击方：投骰前显示预计攻击范围（如 `最终攻击：7～12 (未投骰)`），投骰后实时显示服务器确定的最终攻击力及判定骰点数（如 `最终攻击：10 (判定骰 4)`）。
  - 防守方：选择防御时显示最终防御力及判定骰，选择闪避时显示闪避判定结果及判定骰。
- **界面清爽**：无需右侧冗长文字面板，所有信息均附着于卡牌与对应战区直观呈现。
- **旁观兼容**：支持在旁观其他玩家战斗时同步展示出牌点数与最终攻防。

## 运行环境与配置

目标加载器：CesiumLoader 2.9.1。目标框架：.NET Standard 2.0。

编辑模组目录下的 `config.json`，或在 AstralParty.Toys 的模组配置页修改：

| 配置 | 默认值 | 作用 |
| --- | --- | --- |
| Enabled | true | 总开关 |
| ShowForOthers | true | 显示其他玩家战斗中的已出牌与最终攻防 |
| ShowHandPoints | true | 在手牌上方显示未打出的战斗牌点数范围 |
| ShowPlayedPoints | true | 在已打出的战斗牌上方显示服务器同步的实际点数及最终攻防 |
| FontSize | 26 | 点数字号，范围 16～48 |
| OffsetX / OffsetY | 0 | 战区数值展示区域的水平、垂直位置偏移 |

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

运行单元测试：

```powershell
dotnet test ./tests/BattleCardPointsMod.Tests/BattleCardPointsMod.Tests.csproj
```

产物在 `source\bin\Release\netstandard2.0\`，取 `BattleCardPointsMod.dll`、`BattleCardPointsMod.json`（以及可选的 `.pdb`）放进 `mods\BattleCardPointsMod\` 即可。自备游戏程序集时可用 `CESIUM_GAME_REFS` 环境变量或 `-p:GameRefsDir=` 指定目录。

## 发布

推送 `v*` 标签，或在 `main` 更新版本清单/发布工作流，都会触发 GitHub Actions：准备引用程序集、编译、打包成含 `BattleCardPointsMod\` 目录的 zip，并创建对应版本的 Release。主分支发布从 `BattleCardPointsMod.json` 读取版本并生成 `v<版本>` 标签；显式推送的标签版本必须与清单一致。安装包包含 DLL、PDB、模组清单、默认配置和许可证。手动运行工作流仅生成构建附件。

## 许可证与来源

采用 [GNU AGPL v3](LICENSE)。完整来源说明见 [NOTICE.md](NOTICE.md)。

## 免责声明

本项目为第三方非官方模组，与《吉星派对》开发商、发行商及运营团队无关联，亦未获得其授权或认可。模组会改变游戏信息的呈现方式，相关功能可能不符合官方规则，或影响对局公平性；能够运行不代表官方允许使用。
请在使用前自行了解并遵守最新的用户协议及游戏规则。若官方明确禁止相关功能，请停止使用。
本项目不对功能准确性、兼容性及账号安全作出保证。使用该模组说明你愿意承担所有责任与后果。
不允许大范围宣传，禁止在官方渠道跳脸，包括但不限于直播，官方qq频道，官方q群，官方推特下等
