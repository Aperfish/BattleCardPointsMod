using System;
using BattleCardPointsMod;
using CesiumLoader.SDK.Configuration;
using Xunit;

namespace BattleCardPointsMod.Tests
{
    public class CardPointsTests
    {
        [Theory]
        [InlineData(10001, 1, 3, true, "[color=#FF5555][b]+1～3[/b][/color]")]
        [InlineData(10002, 1, 3, false, "[color=#4DA6FF][b]+1～3[/b][/color]")]
        [InlineData(10003, 1, 6, true, "[color=#FF5555][b]+1～6[/b][/color]")]
        [InlineData(10004, 1, 6, false, "[color=#4DA6FF][b]+1～6[/b][/color]")]
        [InlineData(10005, 1, 10, true, "[color=#FF5555][b]+1～10[/b][/color]")]
        [InlineData(10006, 1, 10, false, "[color=#4DA6FF][b]+1～10[/b][/color]")]
        [InlineData(10007, 1, 20, true, "[color=#FF5555][b]+1～20[/b][/color]")]
        public void TestBattleCardPoints(int cardId, int expectedMin, int expectedMax, bool expectedAtk, string expectedText)
        {
            var range = CardPointHelper.GetPointRange(cardId);
            Assert.True(range.IsValid, $"Card {cardId} should be valid");
            Assert.Equal(expectedMin, range.Min);
            Assert.Equal(expectedMax, range.Max);
            Assert.Equal(expectedAtk, range.IsAttack);
            Assert.Equal(expectedText, range.GetRangeText());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9999)]
        [InlineData(10008)]
        [InlineData(10014)]
        [InlineData(-1)]
        [InlineData(20001)]
        public void TestNonBattleCards(int cardId)
        {
            var range = CardPointHelper.GetPointRange(cardId);
            Assert.False(range.IsValid, $"Card {cardId} should not be valid");
            Assert.Null(range.GetRangeText());
        }

        [Fact]
        public void TestConfigDocsApply()
        {
            var config = ModConfig.ForModId("TestBattleCardPointsMod");
            bool changed = BattleCardPointsConfigDocs.Apply(config);
            Assert.True(changed);

            var comments = config.Get<System.Collections.Generic.Dictionary<string, object>>("_comments");
            Assert.NotNull(comments);
            Assert.True(comments.ContainsKey("CardPointsEnabled"));
            Assert.True(comments.ContainsKey("ShowHandPoints"));
            Assert.True(comments.ContainsKey("ShowPlayedPoints"));
        }

        [Fact]
        public void TestFormatHeaderText_AttackerBeforeAndAfterDice()
        {
            var role = new party.model.BattleRole
            {
                PlayerId = 1001,
                InitAtk = 3,
                Atk = 3
            };
            role.CardCombatBonus.Add(new party.model.cardCombat { CardId = 10001, CardBonus = 2 });

            // Before dice: shows base attack and post-card value in white/colored UBB (even if MinAtk/MaxAtk has a range)
            role.MinAtk = 4;
            role.MaxAtk = 6;
            role.UseCards.Add(4);
            string before = PlayedCardsOverlay.FormatHeaderText(role, isAtk: true, dice: -1);
            Assert.Contains("[color=#FFFFFF]攻击：[/color][color=#FFD56A][b]3[/b][/color]  [color=#FFFFFF]判定骰：未投[/color]", before);
            Assert.Contains("[color=#FFFFFF]牌后数值：[/color][color=#FFD56A][b]5[/b][/color]", before);
            Assert.DoesNotContain("牌后范围", before);

            // After dice (e.g. 4): final attack becomes 3 + 2 + 4 = 9
            role.Atk = 9;
            string after = PlayedCardsOverlay.FormatHeaderText(role, isAtk: true, dice: 4);
            Assert.Contains("[color=#FFFFFF]攻击：[/color][color=#FFD56A][b]9[/b][/color]  [color=#FFFFFF]判定骰：[/color][color=#FFD56A][b]4[/b][/color]", after);
        }

        [Fact]
        public void TestFormatHeaderText_DefenderDefenseAndDodge()
        {
            var defRole = new party.model.BattleRole
            {
                PlayerId = 1002,
                InitDef = 2,
                Def = 2
            };
            defRole.CardCombatBonus.Add(new party.model.cardCombat { CardId = 10002, CardBonus = 3 });

            defRole.UseCards.Add(9);
            defRole.MinDef = 3;
            defRole.MaxDef = 5;
            // Defending before dice: 2 + 3 = 5
            string defBefore = PlayedCardsOverlay.FormatHeaderText(defRole, isAtk: false, dice: -1);
            Assert.Contains("[color=#FFFFFF]防御：[/color][color=#5CD6FF][b]2[/b][/color]  [color=#FFFFFF]判定骰：未投[/color]", defBefore);
            Assert.Contains("[color=#FFFFFF]牌后数值：[/color][color=#5CD6FF][b]5[/b][/color]", defBefore);

            // Defending after dice 3: 2 + 3 + 3 = 8
            defRole.Def = 8;
            string defAfter = PlayedCardsOverlay.FormatHeaderText(defRole, isAtk: false, dice: 3);
            Assert.Contains("[color=#FFFFFF]防御：[/color][color=#5CD6FF][b]8[/b][/color]  [color=#FFFFFF]判定骰：[/color][color=#5CD6FF][b]3[/b][/color]", defAfter);

            // Dodge before dice
            var dodgeRole = new party.model.BattleRole
            {
                PlayerId = 1002,
                Dodge = true
            };
            string dodgeBefore = PlayedCardsOverlay.FormatHeaderText(dodgeRole, isAtk: false, dice: -1);
            Assert.Equal("[color=#FFFFFF]闪避  判定骰：未投[/color]", dodgeBefore);

            // Dodge after dice 6
            string dodgeAfter = PlayedCardsOverlay.FormatHeaderText(dodgeRole, isAtk: false, dice: 6);
            Assert.Equal("[color=#FFFFFF]闪避  判定骰：[/color][color=#5CD6FF][b]6[/b][/color]", dodgeAfter);
        }
    }
}
