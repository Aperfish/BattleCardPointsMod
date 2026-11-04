using BattleCardPointsMod;
using party.model;
using Xunit;

public class PlayedCardGuidTests
{
    [Fact]
    public void ActualBonusUsesCardTypeEvenWhenUseCardsContainsInstanceId()
    {
        var role = new BattleRole { MinAtk = 6, MaxAtk = 8 };
        role.UseCards.Add(4);
        role.CardCombatBonus.Add(new cardCombat { CardId = 10001, CardBonus = 2 });

        Assert.True(CardPointHelper.TryNarrowPlayedRange(role, true, out int min, out int max));
        Assert.Equal(7, min);
        Assert.Equal(7, max);
    }

    [Fact]
    public void RepeatedCardTypesKeepBothIndependentBonuses()
    {
        var role = new BattleRole { MinAtk = 7, MaxAtk = 11 };
        role.UseCards.Add(4);
        role.UseCards.Add(9);
        role.CardCombatBonus.Add(new cardCombat { CardId = 10001, CardBonus = 1 });
        role.CardCombatBonus.Add(new cardCombat { CardId = 10001, CardBonus = 3 });

        Assert.True(CardPointHelper.TryNarrowPlayedRange(role, true, out int min, out int max));
        Assert.Equal(9, min);
        Assert.Equal(9, max);
    }
}
