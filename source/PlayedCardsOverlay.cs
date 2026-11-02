using System;
using System.Text;
using FairyGUI;
using party.model;
using Tools;
using UI;
using UnityEngine;

namespace BattleCardPointsMod
{
    public sealed class PlayedCardsOverlay : IDisposable
    {
        private const float CardW = 86f;
        private const float CardH = 120f;
        private const float Gap = 12f;

        private GComponent _atkRoot;
        private GComponent _defRoot;
        private string _lastAtkSig;
        private string _lastDefSig;
        private object _lastPane;

        public void Update(UIFightWindow pane, BattleRole attacker, BattleRole defender,
            int attackerDice, int defenderDice,
            bool enabled, int fontSize, float offsetX, float offsetY)
        {
            if (pane == null || !enabled)
            {
                Hide();
                return;
            }

            EnsureRoots(pane);

            // Update attacker (left side)
            float paneW = pane.width > 0 ? pane.width : 1920f;
            float panelWidth = Math.Min(Math.Max(400f, fontSize * 18f), paneW * 0.26f);
            UpdateSide(_atkRoot, attacker, isAtk: true, attackerDice, fontSize, panelWidth, ref _lastAtkSig);

            // Update defender (right side)
            UpdateSide(_defRoot, defender, isAtk: false, defenderDice, fontSize, panelWidth, ref _lastDefSig);

            // Position containers on screen
            PositionRoots(pane, offsetX, offsetY);
        }

        private void EnsureRoots(UIFightWindow pane)
        {
            if (_atkRoot != null && (!ReferenceEquals(pane, _lastPane) || _atkRoot.isDisposed || _atkRoot.parent != pane))
            {
                DisposeRoots();
            }

            if (_atkRoot == null)
            {
                _atkRoot = new GComponent { name = "battle_played_atk", touchable = false, sortingOrder = 9500 };
                pane.AddChild(_atkRoot);
                _lastAtkSig = null;
            }

            if (_defRoot == null)
            {
                _defRoot = new GComponent { name = "battle_played_def", touchable = false, sortingOrder = 9500 };
                pane.AddChild(_defRoot);
                _lastDefSig = null;
            }

            _lastPane = pane;
            _atkRoot.visible = true;
            _defRoot.visible = true;
        }

        private void PositionRoots(UIFightWindow pane, float offsetX, float offsetY)
        {
            float paneW = pane.width > 0 ? pane.width : 1920f;
            float paneH = pane.height > 0 ? pane.height : 1080f;

            // Keep both panels below the actors/HP and outside the central hand
            // area. The inset on the right also leaves room for the back button.
            float y = Math.Max(16f, Math.Min(paneH - 80f, paneH * 0.70f + offsetY));
            float availableH = Math.Max(48f, paneH - y - 24f);
            float maxH = Math.Max(_atkRoot?.height ?? 0f, _defRoot?.height ?? 0f);
            float maxW = Math.Max(_atkRoot?.width ?? 0f, _defRoot?.width ?? 0f);
            float scale = Math.Min(1f, Math.Min(availableH / Math.Max(1f, maxH),
                paneW * 0.27f / Math.Max(1f, maxW)));
            PositionSide(_atkRoot, paneW * 0.225f + offsetX, y, scale, paneW);
            PositionSide(_defRoot, paneW * 0.775f + offsetX, y, scale, paneW);
        }

        private static void PositionSide(GComponent root, float centerX, float y, float scale, float paneW)
        {
            if (root == null || !root.visible) return;
            root.SetScale(scale, scale);
            float width = root.width * scale;
            float x = Math.Max(16f, Math.Min(paneW - width - 16f, centerX - width * 0.5f));
            root.SetXY(x, y);
        }

        private void UpdateSide(GComponent root, BattleRole role, bool isAtk, int dice, int fontSize, float panelWidth, ref string lastSig)
        {
            if (root == null) return;
            if (role == null)
            {
                root.visible = false;
                lastSig = null;
                return;
            }

            string sig = BuildSignature(role, dice, fontSize) + ";" + panelWidth;
            if (sig == lastSig) return;
            lastSig = sig;

            root.RemoveChildren(0, -1, true);

            var bonusList = role.CardCombatBonus;
            var useCards = role.UseCards;
            int cardCount = Math.Max(bonusList.Count, useCards.Count);

            float totalW = panelWidth;
            int columns = Math.Max(1, (int)((totalW + Gap) / (CardW + Gap)));

            // 1. 最终数值标头（攻击/防御、判定骰、牌后数值）
            bool isDiceSettled = dice >= 0 || (!isAtk && role.Dodge);

            string headerText = FormatHeaderText(role, isAtk, dice);
            var headerLabel = new GTextField
            {
                touchable = false,
                singleLine = false,
                autoSize = AutoSizeType.Height,
                align = AlignType.Center,
                verticalAlign = VertAlignType.Middle,
                stroke = 0f,
                UBBEnabled = true
            };
            headerLabel.textFormat.color = Color.white;
            headerLabel.textFormat.size = Math.Max(20, fontSize - 4);
            headerLabel.textFormat.bold = true;
            headerLabel.SetSize(totalW - 24f, 0f);
            headerLabel.SetXY(12f, 8f);
            headerLabel.text = headerText;
            float headerH = Math.Max(isDiceSettled ? 46f : 76f, headerLabel.textHeight + 16f);

            var headerBox = new GComponent { touchable = false };
            headerBox.SetSize(totalW, headerH);
            var headerBg = new GGraph { touchable = false };
            headerBg.SetSize(totalW, headerH);
            Color bgColor = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            headerBg.DrawRoundRect(totalW, headerH, bgColor, new float[] { 6f, 6f, 6f, 6f });
            headerBox.AddChild(headerBg);
            headerBox.AddChild(headerLabel);

            root.AddChild(headerBox);

            // 2. 如果打出了卡牌，在标头下方横向展示卡牌及其具体点数
            if (cardCount > 0)
            {
                float cardsY = headerH + 38f;

                for (int i = 0; i < cardCount; i++)
                {
                    int cardId = 0;
                    int? bonus = null;

                    if (i < bonusList.Count)
                    {
                        cardId = bonusList[i].CardId;
                        bonus = bonusList[i].CardBonus;
                    }
                    else if (i < useCards.Count)
                    {
                        cardId = useCards[i];
                    }

                    var item = CreateCardItem(cardId, bonus, role.PlayerId, isAtk, fontSize);
                    int row = i / columns;
                    int column = i % columns;
                    int inRow = Math.Min(columns, cardCount - row * columns);
                    float rowWidth = inRow * CardW + (inRow - 1) * Gap;
                    item.SetXY((totalW - rowWidth) * 0.5f + column * (CardW + Gap),
                        cardsY + row * (CardH + 46f));
                    root.AddChild(item);
                }

                int rows = (cardCount + columns - 1) / columns;
                root.SetSize(totalW, cardsY + (rows - 1) * (CardH + 46f) + CardH + 10f);
            }
            else
            {
                root.SetSize(totalW, headerH);
            }

            root.visible = true;
        }

        public static string FormatHeaderText(BattleRole role, bool isAtk, int dice)
        {
            if (role == null) return string.Empty;

            string label = isAtk ? "攻击" : (role.Dodge ? "闪避" : "防御");
            string mainColor = isAtk ? "#FFD56A" : "#5CD6FF";

            int baseVal = isAtk
                ? (role.InitAtk > 0 ? role.InitAtk : role.Atk)
                : (role.InitDef > 0 ? role.InitDef : role.Def);

            int cardBonusSum = 0;
            if (role.CardCombatBonus != null)
            {
                for (int i = 0; i < role.CardCombatBonus.Count; i++)
                    cardBonusSum += role.CardCombatBonus[i].CardBonus;
            }

            var sb = new StringBuilder();

            if (isAtk)
            {
                if (dice >= 0)
                {
                    int finalAtk = Math.Max(role.Atk, baseVal + cardBonusSum + dice);
                    sb.Append("[color=#FFFFFF]攻击：[/color][color=").Append(mainColor).Append("][b]").Append(finalAtk).Append("[/b][/color]")
                      .Append("  [color=#FFFFFF]判定骰：[/color][color=").Append(mainColor).Append("][b]").Append(dice).Append("[/b][/color]");
                }
                else
                {
                    sb.Append("[color=#FFFFFF]攻击：[/color][color=").Append(mainColor).Append("][b]").Append(baseVal).Append("[/b][/color]")
                      .Append("  [color=#FFFFFF]判定骰：未投[/color]");

                    int cardAtk = baseVal + cardBonusSum;
                    int displayAtk = cardAtk > 0 ? cardAtk : (role.MinAtk > 0 ? role.MinAtk : baseVal);
                    sb.Append("\n[color=#FFFFFF]牌后数值：[/color][color=").Append(mainColor).Append("][b]").Append(displayAtk).Append("[/b][/color][color=#CCCCCC]（未含判定骰）[/color]");
                }
            }
            else
            {
                if (role.Dodge)
                {
                    if (dice >= 0)
                    {
                        sb.Append("[color=#FFFFFF]闪避  判定骰：[/color][color=").Append(mainColor).Append("][b]").Append(dice).Append("[/b][/color]");
                    }
                    else
                    {
                        sb.Append("[color=#FFFFFF]闪避  判定骰：未投[/color]");
                    }
                }
                else
                {
                    if (dice >= 0)
                    {
                        int finalDef = Math.Max(role.Def, baseVal + cardBonusSum + dice);
                        sb.Append("[color=#FFFFFF]防御：[/color][color=").Append(mainColor).Append("][b]").Append(finalDef).Append("[/b][/color]")
                          .Append("  [color=#FFFFFF]判定骰：[/color][color=").Append(mainColor).Append("][b]").Append(dice).Append("[/b][/color]");
                    }
                    else
                    {
                        sb.Append("[color=#FFFFFF]防御：[/color][color=").Append(mainColor).Append("][b]").Append(baseVal).Append("[/b][/color]")
                          .Append("  [color=#FFFFFF]判定骰：未投[/color]");

                        int cardDef = baseVal + cardBonusSum;
                        int displayDef = cardDef > 0 ? cardDef : (role.MinDef > 0 ? role.MinDef : baseVal);
                        sb.Append("\n[color=#FFFFFF]牌后数值：[/color][color=").Append(mainColor).Append("][b]").Append(displayDef).Append("[/b][/color][color=#CCCCCC]（未含判定骰）[/color]");
                    }
                }
            }

            return sb.ToString();
        }

        private GComponent CreateCardItem(int cardId, int? bonus, long playerId, bool isAtk, int fontSize)
        {
            var item = new GComponent { touchable = false };
            item.SetSize(CardW, CardH);

            // 1. 尝试原生 UICom_Card 渲染，异常时降级纯色卡牌
            bool nativeRendered = false;
            try
            {
                if (cardId > 0 && StaticConfigure.Card.InfoDict != null && StaticConfigure.Card.InfoDict.TryGetValue(cardId, out var cfg))
                {
                    var card = UICom_Card.CreateInstance();
                    var view = cfg.GetBattlePlayerCardView(playerId);
                    string name = cfg.NameID.GetLocal(UIStringType.Card);
                    CommonUIManager.RendererCard(card, view, name, "", "", "", cfg.Cost, cfg.CardType, cfg.CardTargetType);
                    if (card.height > 0 && card.width > 0)
                    {
                        float scale = CardH / card.height;
                        card.SetScale(scale, scale);
                        card.touchable = false;
                        item.AddChild(card);
                        nativeRendered = true;
                    }
                }
            }
            catch { }

            if (!nativeRendered)
            {
                var bg = new GGraph { touchable = false };
                bg.SetSize(CardW, CardH);
                Color bgColor = isAtk ? new Color(0.35f, 0.12f, 0.12f, 0.92f) : new Color(0.12f, 0.22f, 0.38f, 0.92f);
                bg.DrawRoundRect(CardW, CardH, bgColor, new float[] { 6f, 6f, 6f, 6f });
                item.AddChild(bg);

                var nameLabel = new GTextField
                {
                    touchable = false,
                    autoSize = AutoSizeType.None,
                    align = AlignType.Center,
                    verticalAlign = VertAlignType.Middle,
                    stroke = 0f
                };
                nameLabel.textFormat.color = Color.white;
                nameLabel.textFormat.size = 14;
                nameLabel.textFormat.bold = true;
                nameLabel.text = CardPointHelper.CardName(cardId);
                nameLabel.SetSize(CardW - 6f, 50f);
                nameLabel.SetXY(3f, 32f);
                item.AddChild(nameLabel);
            }

            // 2. 卡牌上方的实际点数标签
            var badge = new GTextField
            {
                name = "badge",
                touchable = false,
                autoSize = AutoSizeType.None,
                align = AlignType.Center,
                verticalAlign = VertAlignType.Middle,
                stroke = 0f,
                UBBEnabled = true
            };
            badge.textFormat.color = Color.white;
            badge.textFormat.bold = true;
            badge.textFormat.size = Math.Max(26, fontSize);

            string color = isAtk ? "#FF5555" : "#4DA6FF";
            string text = bonus.HasValue
                ? (bonus.Value >= 0 ? $"+{bonus.Value}" : $"{bonus.Value}")
                : "...";
            badge.text = $"[color={color}][b]{text}[/b][/color]";

            badge.SetSize(CardW, 34f);
            badge.SetXY(0f, -34f);
            item.AddChild(badge);

            return item;
        }

        private static string BuildSignature(BattleRole role, int dice, int fontSize)
        {
            if (role == null) return string.Empty;
            var sb = new StringBuilder();
            sb.Append(role.PlayerId).Append(';')
              .Append(role.Atk).Append(';')
              .Append(role.Def).Append(';')
              .Append(role.InitAtk).Append(';')
              .Append(role.InitDef).Append(';')
              .Append(role.MinAtk).Append(';')
              .Append(role.MaxAtk).Append(';')
              .Append(role.MinDef).Append(';')
              .Append(role.MaxDef).Append(';')
              .Append(dice).Append(';')
              .Append(role.Dodge ? 1 : 0).Append(';')
              .Append(fontSize).Append(';');
            for (int i = 0; i < role.UseCards.Count; i++)
                sb.Append(role.UseCards[i]).Append(',');
            sb.Append('|');
            for (int i = 0; i < role.CardCombatBonus.Count; i++)
            {
                var b = role.CardCombatBonus[i];
                sb.Append(b.CardId).Append(':').Append(b.CardBonus).Append(',');
            }
            return sb.ToString();
        }

        public void Hide()
        {
            if (_atkRoot != null) _atkRoot.visible = false;
            if (_defRoot != null) _defRoot.visible = false;
            _lastAtkSig = null;
            _lastDefSig = null;
        }

        private void DisposeRoots()
        {
            if (_atkRoot != null)
            {
                try { _atkRoot.RemoveFromParent(); _atkRoot.Dispose(); } catch { }
                _atkRoot = null;
            }
            if (_defRoot != null)
            {
                try { _defRoot.RemoveFromParent(); _defRoot.Dispose(); } catch { }
                _defRoot = null;
            }
            _lastPane = null;
            _lastAtkSig = null;
            _lastDefSig = null;
        }

        public void Dispose()
        {
            DisposeRoots();
        }
    }
}
