using System.Collections.Generic;
using System.Reflection;
using FairyGUI;
using UI;
using UnityEngine;

namespace BattleCardPointsMod
{
    public sealed class HandCardBadgeManager
    {
        private static readonly FieldInfo CardItemListField =
            typeof(FightWindow).GetField("cardItemList", BindingFlags.Instance | BindingFlags.NonPublic);

        public void UpdateBadges(FightWindow fightWin, bool enabled, int fontSize)
        {
            if (fightWin == null || !fightWin.isShowing || !enabled) return;
            if (CardItemListField == null) return;

            var list = CardItemListField.GetValue(fightWin) as List<UIHandCard_Button_Card>;
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item == null || item.isDisposed || !item.visible) continue;
                var cardObj = item.com_Card;
                if (cardObj == null || cardObj.isDisposed) continue;

                var badge = cardObj.GetChild("battle_point_badge") as GTextField;
                int cardId = item.CardData?.CardId ?? 0;
                var range = CardPointHelper.GetPointRange(cardId);

                if (!range.IsValid)
                {
                    if (badge != null) badge.visible = false;
                    continue;
                }

                if (badge == null)
                {
                    badge = new GTextField
                    {
                        name = "battle_point_badge",
                        touchable = false,
                        autoSize = AutoSizeType.None,
                        align = AlignType.Center,
                        verticalAlign = VertAlignType.Middle,
                        stroke = 0f,
                        UBBEnabled = true
                    };
                    badge.textFormat.color = Color.white;
                    badge.textFormat.bold = true;
                    badge.textFormat.size = fontSize;
                    float w = cardObj.width > 0 ? cardObj.width : 220f;
                    badge.SetSize(w, 36f);
                    badge.SetXY(0f, -36f);
                    cardObj.AddChild(badge);
                }
                else if (badge.textFormat.size != fontSize)
                {
                    badge.textFormat.size = fontSize;
                    badge.textFormat = badge.textFormat;
                }

                badge.text = range.GetRangeText();
                badge.visible = true;
            }
        }
    }
}
