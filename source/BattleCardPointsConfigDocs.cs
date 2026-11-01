using CesiumLoader.SDK.Configuration;

namespace BattleCardPointsMod
{
    public static class BattleCardPointsConfigDocs
    {
        public static bool Apply(ModConfig config)
        {
            if (config == null) return false;
            bool changed = false;
            changed |= config.Describe("Enabled", D(
                "总开关：关闭后不显示战斗牌点数。",
                "總開關：關閉後不顯示戰鬥牌點數。",
                "Master switch: disables battle card point displays when off.",
                "全体スイッチ：オフにすると戦闘カードの出目表示を無効にします。"));
            changed |= config.Describe("ShowForOthers", D(
                "显示其他玩家战斗中的已出牌点数。",
                "顯示其他玩家戰鬥中的已出牌點數。",
                "Show played battle card points for other players.",
                "他のプレイヤーの戦闘でも使用カードの出目を表示します。"));
            changed |= config.Describe("ShowHandPoints", D(
                "在手牌上方显示未打出的战斗牌点数范围。",
                "在手牌上方顯示未打出的戰鬥牌點數範圍。",
                "Show expected point range above unplayed hand cards.",
                "手札の戦闘カード上部に予想出目範囲を表示します。"));
            changed |= config.Describe("ShowPlayedPoints", D(
                "在已打出的战斗牌上方显示服务器同步的实际点数。",
                "在已打出的戰鬥牌上方顯示伺服器同步的實際點數。",
                "Show actual rolled points above played battle cards.",
                "使用した戦闘カード上部に確定した出目を表示します。"));
            changed |= config.Describe("FontSize", D(
                "点数字号，范围 16～48，默认 26。",
                "點數字號，範圍 16～48，預設 26。",
                "Point text font size (16–48, default 26).",
                "出目テキストのフォントサイズ（16～48、既定 26）。"));
            changed |= config.Describe("OffsetX", D(
                "已出牌显示区域水平偏移，正数向右。",
                "已出牌顯示區域水平偏移，正數向右。",
                "Horizontal offset for played cards display (positive = right).",
                "使用カード表示領域の水平オフセット（正数で右）。"));
            changed |= config.Describe("OffsetY", D(
                "已出牌显示区域垂直偏移，正数向下。",
                "已出牌顯示區域垂直偏移，正數向下。",
                "Vertical offset for played cards display (positive = down).",
                "使用カード表示領域の垂直オフセット（正数で下）。"));
            return changed;
        }

        private static ConfigDoc D(string zhHans, string zhHant, string en, string ja)
            => ConfigDoc.Of(zhHans, zhHant, en, ja);
    }
}
