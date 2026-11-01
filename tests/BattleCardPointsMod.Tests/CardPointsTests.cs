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
        [InlineData(10008, 3, 3, true, "[color=#FF5555][b]+3[/b][/color]")]
        [InlineData(10009, 5, 5, true, "[color=#FF5555][b]+5[/b][/color]")]
        [InlineData(10010, 6, 6, true, "[color=#FF5555][b]+6[/b][/color]")]
        [InlineData(10011, 0, 0, true, "[color=#FF5555][b]+0[/b][/color]")]
        [InlineData(10012, 0, 4, true, "[color=#FF5555][b]+0～4[/b][/color]")]
        [InlineData(10013, 3, 3, true, "[color=#FF5555][b]+3[/b][/color]")]
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
            Assert.True(comments.ContainsKey("Enabled"));
            Assert.True(comments.ContainsKey("ShowHandPoints"));
            Assert.True(comments.ContainsKey("ShowPlayedPoints"));
        }
    }
}
