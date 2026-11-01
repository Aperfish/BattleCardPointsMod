using System;
using System.Text;
using FairyGUI;
using Google.Protobuf.Collections;
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

        public void Update(UIFightWindow pane, BattleRole attacker, BattleRole defender, bool enabled, int fontSize, float offsetX, float offsetY)
        {
            if (pane == null || !enabled)
            {
                Hide();
                return;
            }

            EnsureRoots(pane);

            // Update attacker played cards
            UpdateSide(_atkRoot, attacker, isAtk: true, fontSize, ref _lastAtkSig);

            // Update defender played cards
            UpdateSide(_defRoot, defender, isAtk: false, fontSize, ref _lastDefSig);

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

            float y = paneH * 0.38f + offsetY;

            // Attacker on left side
            if (_atkRoot != null && _atkRoot.visible)
            {
                _atkRoot.SetXY(70f + offsetX, y);
            }

            // Defender on right side, right-aligned
            if (_defRoot != null && _defRoot.visible)
            {
                float defX = paneW - 70f - _defRoot.width + offsetX;
                _defRoot.SetXY(defX, y);
            }
        }

        private void UpdateSide(GComponent root, BattleRole role, bool isAtk, int fontSize, ref string lastSig)
        {
            if (root == null) return;
            if (role == null || (role.CardCombatBonus.Count == 0 && role.UseCards.Count == 0))
            {
                root.visible = false;
                lastSig = null;
                return;
            }

            string sig = BuildSignature(role, fontSize);
            if (sig == lastSig) return;
            lastSig = sig;

            root.RemoveChildren(0, -1, true);

            var bonusList = role.CardCombatBonus;
            var useCards = role.UseCards;
            int count = Math.Max(bonusList.Count, useCards.Count);
            if (count == 0)
            {
                root.visible = false;
                return;
            }

            float currentX = 0f;
            for (int i = 0; i < count; i++)
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
                item.SetXY(currentX, 0f);
                root.AddChild(item);

                currentX += CardW + Gap;
            }

            root.SetSize(Math.Max(CardW, currentX - Gap), CardH + 40f);
            root.visible = true;
        }

        private GComponent CreateCardItem(int cardId, int? bonus, long playerId, bool isAtk, int fontSize)
        {
            var item = new GComponent { touchable = false };
            item.SetSize(CardW, CardH);

            // 1. Try native UICom_Card, fallback to stylish block
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
                    stroke = 1.5f,
                    strokeColor = Color.black
                };
                nameLabel.textFormat.size = 14;
                nameLabel.textFormat.bold = true;
                nameLabel.text = CardPointHelper.CardName(cardId);
                nameLabel.SetSize(CardW - 6f, 50f);
                nameLabel.SetXY(3f, 32f);
                item.AddChild(nameLabel);
            }

            // 2. Point badge on top of the card
            var badge = new GTextField
            {
                name = "badge",
                touchable = false,
                autoSize = AutoSizeType.None,
                align = AlignType.Center,
                verticalAlign = VertAlignType.Middle,
                stroke = 2.5f,
                strokeColor = Color.black,
                UBBEnabled = true
            };
            badge.textFormat.bold = true;
            badge.textFormat.size = Math.Max(26, fontSize);

            string color = isAtk ? "#FF5555" : "#4DA6FF";
            string text = bonus.HasValue
                ? (bonus.Value >= 0 ? $"+{bonus.Value}" : $"{bonus.Value}")
                : "...";
            badge.text = $"[color={color}][b]{text}[/b][/color]";

            badge.SetSize(CardW, 36f);
            badge.SetXY(0f, -36f);
            item.AddChild(badge);

            return item;
        }

        private static string BuildSignature(BattleRole role, int fontSize)
        {
            if (role == null) return string.Empty;
            var sb = new StringBuilder();
            sb.Append(role.PlayerId).Append(';').Append(fontSize).Append(';');
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
