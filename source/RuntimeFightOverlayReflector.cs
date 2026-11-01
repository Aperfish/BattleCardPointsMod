// Adapted from CesiumLoader CombatOddsMod, AGPL-3.0. See NOTICE.md.
using System;
using CesiumLoader.SDK.Logging;
using CesiumLoader.SDK.Runtime;
using Tools;   // SimpleSingletonProvider<>
using UI;      // UIManager / FightWindow
using UnityEngine;

namespace BattleCardPointsMod
{
    /// <summary>
    /// 真实 FairyGUI 反射实现: 通过 SimpleSingletonProvider&lt;UIManager&gt;.Fight 取窗口,
    /// 用 RuntimeAssemblyService 反射访问 FairyGUI 成员(不需要编译期引用 FairyGUI)。
    ///
    /// 注意: 本类无法离线验证——真机进游戏才能目视确认标签是否出现在 FightWindow 上。
    /// 所有操作全部包 try/catch, 任一步失败都降级(返回 null / no-op), 由控制器隐藏并让
    /// ModEntry 的控制台/通知输出兜底, 绝不因 UI 反射失败影响计算与日志。
    /// </summary>
    public sealed class RuntimeFightOverlayReflector : IFightOverlayReflector
    {
        // 布局参数(由 ModEntry 按配置在启动时写入; 有默认值, 便于单独使用)。
        internal static int BaseFontSize = 28;          // 正文基准字号(比旧版 22 更大)
        internal static float LabelWidth = 660f;        // 面板宽(容纳更大字号)
        internal static float LabelHeight = 480f;
        internal static string Anchor = "right-center";  // left-center / top-left / top-center / top-right / right-center
        internal static float OffsetX = 0f;
        internal static float OffsetY = 0f;

        // FightWindow 实例(强类型可取, 但其 FairyGUI 成员一律反射)。
        private object GetFightWindow()
        {
            try { return UIManager.inst?.Fight; }
            catch { return null; }
        }

        public bool IsFightShowing()
        {
            var win = GetFightWindow();
            if (win == null) return false;
            try { return RuntimeAssemblyService.SafeGetProperty<bool>(win, "isShowing", false, "BattleCardPoints.overlay"); }
            catch { return false; }
        }

        public object GetFightPane()
        {
            var win = GetFightWindow();
            if (win == null) return null;
            try { return RuntimeAssemblyService.SafeGetProperty(win, "contentPane", "BattleCardPoints.overlay"); }
            catch { return null; }
        }

        public object CreateLabel()
        {
            try
            {
                var gtfType = RuntimeAssemblyService.FindTypeBySimpleName("GTextField");
                if (gtfType == null) return null;
                var label = Activator.CreateInstance(gtfType);
                if (label == null) return null;

                // 尺寸: 更大面板容纳放大字号; 位置在 AddChildToPane 里按锚点摆放(需要 pane 宽度)。
                RuntimeAssemblyService.SafeInvoke(label, "SetSize", new object[] { LabelWidth, LabelHeight }, "BattleCardPoints.overlay");
                RuntimeAssemblyService.SafeInvoke(label, "SetXY", new object[] { OffsetX, OffsetY }, "BattleCardPoints.overlay");

                // Keep a fixed column width, wrap long names and grow vertically.
                RuntimeAssemblyService.SafeSetProperty(label, "singleLine", false, "BattleCardPoints.overlay");
                RuntimeAssemblyService.SafeSetProperty(label, "wordWrap", true, "BattleCardPoints.overlay");
                var autoSize = gtfType.GetProperty("autoSize");
                if (autoSize != null && autoSize.CanWrite && autoSize.PropertyType.IsEnum)
                    autoSize.SetValue(label, Enum.Parse(autoSize.PropertyType, "Height"), null);

                RuntimeAssemblyService.SafeSetProperty(label, "touchable", false, "BattleCardPoints.overlay");
                RuntimeAssemblyService.SafeSetProperty(label, "sortingOrder", 9000, "BattleCardPoints.overlay");
                // 开启 UBB, 让 [color]/[size] 富文本标签被 UBBParser 解析(否则会原样显示 "[color...]")。
                // 注意: 属性名是 UBBEnabled(大写 UBB); 同时直接写 protected 字段 _ubbEnabled 兜底。
                RuntimeAssemblyService.SafeSetProperty(label, "UBBEnabled", true, "BattleCardPoints.overlay");
                RuntimeAssemblyService.SafeSetField(label, "_ubbEnabled", true, "BattleCardPoints.overlay");
                bool ubbOk = RuntimeAssemblyService.SafeGetProperty<bool>(label, "UBBEnabled", false, "BattleCardPoints.overlay");
                _ubbOk = ubbOk;
                SdkLog.Info("BattleCardPoints", "[overlay] UBBEnabled=" + ubbOk + " (true 才会渲染彩色, 否则自动降级纯文本)");

                // 文本格式: 白字加粗描边, 保证深色战斗背景上可读。
                var tf = RuntimeAssemblyService.SafeGetProperty(label, "textFormat", "BattleCardPoints.overlay");
                if (tf != null)
                {
                    RuntimeAssemblyService.SafeSetField(tf, "size", BaseFontSize, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeSetProperty(tf, "size", BaseFontSize, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeSetField(tf, "color", Color.white, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeSetProperty(tf, "color", Color.white, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeSetField(tf, "bold", true, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeSetField(tf, "outline", 2f, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeSetField(tf, "outlineColor", Color.black, "BattleCardPoints.overlay");
                    RuntimeAssemblyService.SafeInvoke(label, "ApplyFormat", null, "BattleCardPoints.overlay");
                    // 有的版本用 textFormat setter 生效。
                    RuntimeAssemblyService.SafeSetProperty(label, "textFormat", tf, "BattleCardPoints.overlay");
                }

                SdkLog.Info("BattleCardPoints", "已创建 FightWindow 覆盖层标签(等待进游戏目视确认位置)");
                return label;
            }
            catch (Exception e)
            {
                SdkLog.Warn("BattleCardPoints", "创建覆盖层标签失败(降级到控制台): " + e.Message);
                return null;
            }
        }

        public bool LabelInPane(object label, object pane)
        {
            if (label == null || pane == null) return false;
            try
            {
                var parent = RuntimeAssemblyService.SafeGetProperty(label, "parent", "BattleCardPoints.overlay");
                return ReferenceEquals(parent, pane);
            }
            catch { return false; }
        }

        public void AddChildToPane(object pane, object label)
        {
            if (pane == null || label == null) return;
            try
            {
                RuntimeAssemblyService.SafeInvoke(pane, "AddChild", new object[] { label }, "BattleCardPoints.overlay");
                PositionByAnchor(pane, label);
                SdkLog.Info("BattleCardPoints", "[overlay] AddChild → paneId=" + Id(pane) + " labelId=" + Id(label));
            }
            catch (Exception e) { SdkLog.Warn("BattleCardPoints", "覆盖层挂载失败: " + e.Message); }
        }

        /// <summary>按锚点把面板摆到位置(靠左中间/左上/居中/右上等), 并叠加配置微调偏移。</summary>
        private void PositionByAnchor(object pane, object label)
        {
            try
            {
                float paneW = RuntimeAssemblyService.SafeGetProperty<float>(pane, "width", 0f, "BattleCardPoints.overlay");
                float paneH = RuntimeAssemblyService.SafeGetProperty<float>(pane, "height", 0f, "BattleCardPoints.overlay");
                float x = OffsetX;
                float y = OffsetY;
                string a = (Anchor ?? "left-center").Trim().ToLowerInvariant();

                // 水平
                if (paneW > 0f)
                {
                    if (a == "top-center") x = (paneW - LabelWidth) * 0.5f + OffsetX;
                    else if (a == "top-right") x = paneW - LabelWidth - 24f + OffsetX;
                    else x = 24f + OffsetX;   // left-center / top-left / 其它 → 靠左
                }
                // 垂直: 带 -center(靠左中间)时垂直居中, 否则从顶部 OffsetY 起算
                if ((a == "left-center" || a == "right-center") && paneH > 0f)
                    y = Math.Max(12f, (paneH - RuntimeAssemblyService.SafeGetProperty<float>(label, "height", LabelHeight, "BattleCardPoints.overlay")) * 0.5f) + OffsetY;
                if (a == "right-center" && paneW > 0f)
                    x = paneW - LabelWidth - 24f + OffsetX;

                RuntimeAssemblyService.SafeInvoke(label, "SetXY", new object[] { x, y }, "BattleCardPoints.overlay");
                // 顶部居中锚点时面板内文本也居中; 枚举设置失败无妨, 单独包一层不影响上面的定位。
                if (a == "top-center")
                {
                    try { RuntimeAssemblyService.SafeSetProperty(label, "align", 1, "BattleCardPoints.overlay"); } catch { }
                }
            }
            catch { /* 位置失败无所谓, 保底用创建时的 OffsetX/Y */ }
        }

        public void SetText(object label, string text)
        {
            if (label == null) return;
            try
            {
                // UBB 未成功开启时(极少数 FairyGUI 变体), 主动去掉标签, 绝不让 "[color...]" 原样显示。
                string payload = _ubbOk ? (text ?? string.Empty) : StripUbb(text ?? string.Empty);
                RuntimeAssemblyService.SafeSetProperty(label, "text", payload, "BattleCardPoints.overlay");
                // 诊断: 仅在"是否还挂在容器上/可见性"变化时打一行(不再每 tick 刷屏, 避免日志噪声与卡顿)。
                var parent = RuntimeAssemblyService.SafeGetProperty(label, "parent", "BattleCardPoints.overlay");
                if (parent != null) PositionByAnchor(parent, label);
                var vis = RuntimeAssemblyService.SafeGetProperty<bool>(label, "visible", false, "BattleCardPoints.overlay");
                string sig = Id(parent) + "/" + vis;
                if (sig != _lastDiag)
                {
                    _lastDiag = sig;
                    SdkLog.Info("BattleCardPoints", "[overlay] labelId=" + Id(label) + " parentId=" + Id(parent) + " visible=" + vis);
                }
            }
            catch { /* 降级: 控制台已输出 */ }
        }

        private bool _ubbOk;
        private string _lastDiag;

        private static readonly System.Text.RegularExpressions.Regex _ubbRe =
            new System.Text.RegularExpressions.Regex(@"\[/?(color|size|b|i|u)(=[^\]]*)?\]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static string StripUbb(string s) =>
            string.IsNullOrEmpty(s) ? s : _ubbRe.Replace(s, string.Empty);

        public void SetVisible(object label, bool visible)
        {
            if (label == null) return;
            try { RuntimeAssemblyService.SafeSetProperty(label, "visible", visible, "BattleCardPoints.overlay"); }
            catch { }
        }

        // 稳定的对象身份(不持有引用), 便于在日志里判断"标签是否还是原来那个/还挂在原容器上"。
        private static string Id(object o)
        {
            if (o == null) return "null";
            try { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o).ToString("x"); }
            catch { return "?"; }
        }
    }
}

