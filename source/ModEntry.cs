using System;
using System.Text;
using CesiumLoader.SDK.Gameplay;
using CesiumLoader.SDK.Logging;
using CesiumLoader.SDK.Manifests;
using CesiumLoader.SDK.Mods;
using GameLogic;
using Tools;
using Core.Net;
using Cysharp.Threading.Tasks;
using party.model;

[assembly: ModManifest("战斗牌点数显示", "1.0.1", "LocalMod",
    "使用战斗牌后实时显示服务器已同步的攻击、防御和判定骰点数。",
    Permissions = ModPermission.ReadGameState, SdkVersion = "2.9.1")]

namespace BattleCardPointsMod
{
    public static class ModEntry
    {
        public static void Main() { ModBase.Run(new PointsMod()); }
    }

    public sealed class PointsMod : ModBase
    {
        private FightOverlayController _overlay;
        private Battle _battle;
        private bool _dirty;
        private float _nextRefresh;
        private string _lastError;
        private long _battleId;
        private int _attackerDice = -1;
        private int _defenderDice = -1;
        private string _lastCard;
        private bool _enabled = true;
        private bool _showForOthers = true;
        private bool _showCardDetails = true;
        private BattleChoiceS2CRPC _choiceRpc;
        private BattleChoiceS2CRPC.OnBattleChoiceS2CServerDelegate _choiceBefore;
        private BattleChoiceS2CRPC.OnBattleChoiceS2CServerDelegate _choiceWrapper;
        private float _nextHook;
        public override string Name { get { return "战斗牌点数显示"; } }

        public override void OnInitialize()
        {
            _overlay = new FightOverlayController(new RuntimeFightOverlayReflector());
            OnConfigChanged();
            GameEvents.BattleUpdate += OnBattle;
            GameEvents.BattlePhaseChanged += OnPhase;
            GameEvents.BattleDice += OnDice;
            GameEvents.CardUsed += OnCard;
            GameEvents.EffectCardUsed += OnCard;
            GameEvents.QuickCardUsed += OnCard;
            Context.RegisterCleanup(Unsubscribe);
            GameEvents.StartAutoHook();
            SdkLog.Info("BattleCardPoints", "已启用：战斗窗口右侧显示实时点数。");
        }

        public override void OnConfigChanged()
        {
            if (Config == null) return;
            _enabled = Config.GetBool("Enabled", true);
            _showForOthers = Config.GetBool("ShowForOthers", true);
            _showCardDetails = Config.GetBool("ShowCardDetails", true);
            RuntimeFightOverlayReflector.BaseFontSize = Math.Max(14, Math.Min(60, Config.GetInt("FontSize", 28)));
            RuntimeFightOverlayReflector.Anchor = Config.GetString("Anchor", "right-center");
            RuntimeFightOverlayReflector.OffsetX = (float)Config.GetDouble("OffsetX", 0);
            RuntimeFightOverlayReflector.OffsetY = (float)Config.GetDouble("OffsetY", 0);
            // Recreate the label so a changed font/anchor takes effect too.
            if (_overlay != null) _overlay.Dispose();
            _overlay = new FightOverlayController(new RuntimeFightOverlayReflector());
            _dirty = true;
        }

        private void OnBattle(Battle b)
        {
            if (b == null || b.Attacker == null || b.Defender == null) return;
            if (_battle == null || _battleId != b.BattleId ||
                _battle.Attacker.PlayerId != b.Attacker.PlayerId ||
                _battle.Defender.PlayerId != b.Defender.PlayerId)
            {
                _battleId = b.BattleId;
                _attackerDice = -1;
                _defenderDice = -1;
                _lastCard = null;
            }
            _battle = b;
            if (GameEvents.AttackerPoint >= 0) _attackerDice = GameEvents.AttackerPoint;
            if (b.Defender.Point > 0) _defenderDice = b.Defender.Point;
            // Render on the following Unity update: SDK events run before the game's
            // own RPC handler, so drawing here would read the previous UI state.
            _dirty = true;
        }

        private void OnPhase(Battle b, BattlePhase phase) { OnBattle(b); }

        private void OnDice(long playerId, int point)
        {
            if (_battle == null || point < 0) return;
            if (playerId == _battle.Attacker.PlayerId) _attackerDice = point;
            else if (playerId == _battle.Defender.PlayerId) _defenderDice = point;
            else return;
            _dirty = true;
        }

        private void OnCard(long playerId, int cardId, int unused)
        {
            if (_battle == null || _battle.IsEnd) return;
            if (playerId != _battle.Attacker.PlayerId && playerId != _battle.Defender.PlayerId) return;
            _lastCard = cardId > 0 ? CardName(cardId) : "战斗牌";
            _dirty = true;
        }

        public override void OnUpdate()
        {
            // Events invalidate immediately; a 100 ms refresh also catches changes
            // of Min/Max fields that the SDK BattleUpdate dedup key omits.
            float now = UnityEngine.Time.unscaledTime;
            if (now >= _nextHook)
            {
                _nextHook = now + 1f;
                try { EnsureChoiceHook(); } catch { }
            }
            if (!_dirty && now < _nextRefresh) return;
            _dirty = false;
            _nextRefresh = now + 0.1f;
            try
            {
                var b = _battle;
                // Read the same live snapshot as FightWindow. BattleUpdate omits
                // card bonuses and Min/Max values from its deduplication key.
                var live = SimpleSingletonProvider<GameLogicManager>.inst?.fight?.battleFightData;
                if (live != null && live.attackerInfo != null && live.defenderInfo != null)
                {
                    if (b == null || b.BattleId != live.battleId)
                    {
                        OnBattle(new Battle { BattleId = live.battleId,
                            Attacker = live.attackerInfo, Defender = live.defenderInfo, IsEnd = live.isEnd });
                        b = _battle;
                    }
                    else
                    {
                        // Local wrapper only: never change the game's Battle object.
                        b = new Battle { BattleId = live.battleId, Attacker = live.attackerInfo,
                            Defender = live.defenderInfo, IsEnd = live.isEnd };
                    }
                }
                if (!_enabled || b == null ||
                    (!_showForOthers && !Players.IsSelf(b.Attacker.PlayerId) && !Players.IsSelf(b.Defender.PlayerId)))
                {
                    _overlay.Hide();
                    return;
                }
                var sb = new StringBuilder(b.IsEnd ? "[b]战斗牌 · 结算点数[/b]\n" : "[b]战斗牌 · 实时点数[/b]\n");
                sb.Append("攻击方：").Append(Escape(Names.BattleRole(b.Attacker))).Append('\n');
                AppendRole(sb, "攻击", b.Attacker.Atk, b.Attacker.MinAtk, b.Attacker.MaxAtk, _attackerDice);
                if (_showCardDetails) AppendCards(sb, b.Attacker);
                sb.Append("\n防守方：").Append(Escape(Names.BattleRole(b.Defender))).Append('\n');
                AppendRole(sb, b.Defender.Dodge ? "闪避" : "防御", b.Defender.Def,
                    b.Defender.MinDef, b.Defender.MaxDef, _defenderDice);
                if (_showCardDetails) AppendCards(sb, b.Defender);
                if (_lastCard != null) sb.Append("\n最近使用：").Append(Escape(_lastCard));
                _overlay.Update(true, sb.ToString());
                _lastError = null;
            }
            catch (Exception e)
            {
                if (_overlay != null) _overlay.Hide();
                if (_lastError != e.Message)
                {
                    _lastError = e.Message;
                    SdkLog.Warn("BattleCardPoints", "点数显示暂不可用：" + e.Message);
                }
            }
        }

        private static void AppendRole(StringBuilder sb, string label, int value, int min, int max, int dice)
        {
            sb.Append(label).Append("：[color=#FFD56A][b]").Append(value).Append("[/b][/color]");
            if (dice >= 0) sb.Append("  判定骰：").Append(dice);
            else sb.Append("  判定骰：未投");
            if (dice < 0 && min != max)
                sb.Append("\n牌后范围：").Append(min).Append('～').Append(max).Append("（未含判定骰）");
            else if (dice < 0)
                sb.Append("\n牌后数值：").Append(min).Append("（未含判定骰）");
        }

        private static string Escape(string s)
        {
            // Names are player controlled; keep them from becoming UBB markup.
            return (s ?? "?").Replace("[", "［").Replace("]", "］").Replace('\n', ' ').Replace('\r', ' ');
        }

        private static void AppendCards(StringBuilder sb, BattleRole role)
        {
            if (role.CardCombatBonus.Count == 0)
            {
                if (role.UseCards.Count > 0) sb.Append("\n卡牌点数：等待服务器同步");
                return;
            }
            sb.Append("\n卡牌点数：");
            int count = role.CardCombatBonus.Count;
            // Show every entry, including repeated cards, zero and negative values.
            for (int i = 0; i < count; i++)
            {
                var c = role.CardCombatBonus[i];
                sb.Append('\n');
                sb.Append(Escape(CardName(c.CardId))).Append("：[color=#FFD56A][b]")
                    .Append(c.CardBonus).Append("[/b][/color]");
            }
        }

        private static string CardName(int cardId)
        {
            string name = Names.Card(cardId) ?? "战斗牌";
            string suffix = "(" + cardId + ")";
            return name.EndsWith(suffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - suffix.Length).TrimEnd()
                : name;
        }

        private void EnsureChoiceHook()
        {
            // Defender results use BattleChoiceS2C, not BattleThrowDiceS2C.
            // Preserve the game's callback and every existing mod's wrapper.
            var rpc = MonoSingletonProvider<NetManager>.inst?.RPC?.BattleChoiceS2C;
            if (rpc == null) return;
            var before = rpc.OnBattleChoiceS2CServerCallBackAsync;
            if (before == null || ReferenceEquals(before, _choiceWrapper)) return;
            BattleChoiceS2CRPC.OnBattleChoiceS2CServerDelegate wrapper = (model, error, dispatch) =>
            {
                try
                {
                    if (error == 0 && model != null && _battle != null &&
                        model.PlayerId == _battle.Defender.PlayerId)
                    {
                        _defenderDice = model.Val;
                        _dirty = true;
                    }
                }
                catch { }
                return before(model, error, dispatch);
            };
            _choiceRpc = rpc;
            _choiceBefore = before;
            _choiceWrapper = wrapper;
            rpc.OnBattleChoiceS2CServerCallBackAsync = wrapper;
        }

        private void Unsubscribe()
        {
            GameEvents.BattleUpdate -= OnBattle;
            GameEvents.BattlePhaseChanged -= OnPhase;
            GameEvents.BattleDice -= OnDice;
            GameEvents.CardUsed -= OnCard;
            GameEvents.EffectCardUsed -= OnCard;
            GameEvents.QuickCardUsed -= OnCard;
            if (_choiceRpc != null && ReferenceEquals(_choiceRpc.OnBattleChoiceS2CServerCallBackAsync, _choiceWrapper))
                _choiceRpc.OnBattleChoiceS2CServerCallBackAsync = _choiceBefore;
            _choiceRpc = null;
            _choiceWrapper = null;
            _choiceBefore = null;
            if (_overlay != null) _overlay.Dispose();
            _battle = null;
        }

        public override void OnUnload() { Unsubscribe(); }
    }
}
