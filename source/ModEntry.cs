using System;
using System.Reflection;
using CesiumLoader.SDK.Gameplay;
using CesiumLoader.SDK.Logging;
using CesiumLoader.SDK.Manifests;
using CesiumLoader.SDK.Mods;
using Core.Net;
using Cysharp.Threading.Tasks;
using GameLogic;
using party.model;
using Tools;
using UI;
using UnityEngine;

[assembly: ModManifest("战斗牌点数", "1.2.2", "Aperfish",
    "显示手牌点数范围与已出牌的实际点数。",
    Permissions = ModPermission.ReadGameState, SdkVersion = "2.9.1")]

namespace BattleCardPointsMod
{
    public static class ModEntry
    {
        public static void Main()
        {
            ModBase.Run(new PointsMod());

        }
    }

    public sealed class PointsMod : ModBase
    {
        private static readonly FieldInfo FightWindowField =
            typeof(UIManager).GetField("_cachedFightWindow", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly HandCardBadgeManager _handBadges = new HandCardBadgeManager();
        private readonly PlayedCardsOverlay _playedOverlay = new PlayedCardsOverlay();

        private bool _enabled = true;
        private bool _showForOthers = true;
        private bool _showHandPoints = true;
        private bool _showPlayedPoints = true;
        private int _fontSize = 26;
        private float _offsetX = 0f;
        private float _offsetY = 0f;

        private long _lastBattleId;
        private int _attackerDice = -1;
        private int _defenderDice = -1;

        private BattleChoiceS2CRPC _choiceRpc;
        private BattleChoiceS2CRPC.OnBattleChoiceS2CServerDelegate _choiceBefore;
        private BattleChoiceS2CRPC.OnBattleChoiceS2CServerDelegate _choiceWrapper;
        private float _nextHook;

        private bool _dirty;
        private float _nextRefresh;
        private string _lastError;

        public override string Name { get { return "战斗牌点数显示"; } }

        public override void OnInitialize()
        {
            BattleCardPointsConfigDocs.Apply(Config);
            OnConfigChanged();

            GameEvents.BattleUpdate += OnBattle;
            GameEvents.BattlePhaseChanged += OnPhase;
            GameEvents.BattleDice += OnDice;
            GameEvents.CardUsed += OnCard;
            GameEvents.EffectCardUsed += OnCard;
            GameEvents.QuickCardUsed += OnCard;

            Context.RegisterCleanup(Unsubscribe);
            GameEvents.StartAutoHook();
            SdkLog.Info("BattleCardPoints", "已启用：在每张战斗牌上方显示点数并展示最终攻击与防御。");
        }

        public override void OnConfigChanged()
        {
            if (Config == null) return;
            _enabled = Config.GetBool("CardPointsEnabled", Config.GetBool("Enabled", true));
            _showForOthers = Config.GetBool("CardPointsShowForOthers", Config.GetBool("ShowForOthers", true));
            _showHandPoints = Config.GetBool("ShowHandPoints", true);
            _showPlayedPoints = Config.GetBool("ShowPlayedPoints", true);
            _fontSize = Math.Max(16, Math.Min(48, Config.GetInt("FontSize", 26)));
            _offsetX = (float)Config.GetDouble("OffsetX", 0);
            _offsetY = (float)Config.GetDouble("OffsetY", 0);
            _dirty = true;
        }

        private void OnBattle(Battle b)
        {
            if (b != null && b.BattleId != _lastBattleId)
            {
                _lastBattleId = b.BattleId;
                _attackerDice = -1;
                _defenderDice = -1;
            }
            if (GameEvents.AttackerPoint >= 0)
                _attackerDice = GameEvents.AttackerPoint;
            if (b != null && b.Defender != null && b.Defender.Point > 0)
                _defenderDice = b.Defender.Point;
            _dirty = true;
        }

        private void OnPhase(Battle b, BattlePhase phase)
        {
            _dirty = true;
        }

        private void OnDice(long playerId, int point)
        {
            if (point < 0) return;
            var live = SimpleSingletonProvider<GameLogicManager>.inst?.fight?.battleFightData;
            if (live != null)
            {
                if (live.attackerInfo != null && playerId == live.attackerInfo.PlayerId)
                    _attackerDice = point;
                else if (live.defenderInfo != null && playerId == live.defenderInfo.PlayerId)
                    _defenderDice = point;
            }
            _dirty = true;
        }

        private void OnCard(long playerId, int cardId, int unused)
        {
            _dirty = true;
        }

        private void EnsureChoiceHook()
        {
            var rpc = MonoSingletonProvider<NetManager>.inst?.RPC?.BattleChoiceS2C;
            if (rpc == null) return;
            var before = rpc.OnBattleChoiceS2CServerCallBackAsync;
            if (before == null || ReferenceEquals(before, _choiceWrapper)) return;
            BattleChoiceS2CRPC.OnBattleChoiceS2CServerDelegate wrapper = (model, error, dispatch) =>
            {
                try
                {
                    var live = SimpleSingletonProvider<GameLogicManager>.inst?.fight?.battleFightData;
                    if (error == 0 && model != null && live != null && live.defenderInfo != null &&
                        model.PlayerId == live.defenderInfo.PlayerId)
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

        public override void OnUpdate()
        {
            float now = Time.unscaledTime;
            if (now >= _nextHook)
            {
                _nextHook = now + 1f;
                try { EnsureChoiceHook(); } catch { }
            }

            if (!_enabled)
            {
                _playedOverlay.Hide();
                return;
            }

            if (!_dirty && now < _nextRefresh) return;
            _dirty = false;
            _nextRefresh = now + 0.08f;

            try
            {
                // 1. 严格守卫：只有实际处于战斗状态中才检查 FightWindow，绝不在切地图或大厅中触发实例化导致黑屏
                var fight = SimpleSingletonProvider<GameLogicManager>.inst?.fight;
                if (fight == null || !fight.fightStatus)
                {
                    _playedOverlay.Hide();
                    return;
                }

                FightWindow fightWin = null;
                try
                {
                    var uiMgr = SimpleSingletonProvider<UIManager>.inst;
                    if (uiMgr != null)
                    {
                        fightWin = FightWindowField != null
                            ? FightWindowField.GetValue(uiMgr) as FightWindow
                            : null;
                    }
                }
                catch { }

                if (fightWin == null || !fightWin.isShowing)
                {
                    _playedOverlay.Hide();
                    return;
                }

                var live = fight.battleFightData;
                if (live == null || live.attackerInfo == null || live.defenderInfo == null)
                {
                    _playedOverlay.Hide();
                    return;
                }

                if (live.battleId != _lastBattleId)
                {
                    _lastBattleId = live.battleId;
                    _attackerDice = -1;
                    _defenderDice = -1;
                }

                if (GameEvents.AttackerPoint >= 0)
                    _attackerDice = GameEvents.AttackerPoint;
                if (live.defenderInfo.Point > 0)
                    _defenderDice = live.defenderInfo.Point;

                // 2. 手牌上方点数预估范围
                _handBadges.UpdateBadges(fightWin, _showHandPoints && !live.isEnd, _fontSize);

                // 3. 双方最终数值标头与已打出卡牌
                if (!_showForOthers && !Players.IsSelf(live.attackerInfo.PlayerId) && !Players.IsSelf(live.defenderInfo.PlayerId))
                {
                    _playedOverlay.Hide();
                }
                else
                {
                    var pane = fightWin.contentPane as UIFightWindow;
                    _playedOverlay.Update(pane, live.attackerInfo, live.defenderInfo,
                        live.isEnd ? live.attackerInfo.Point : _attackerDice,
                        live.isEnd ? live.defenderInfo.Point : _defenderDice,
                        _showPlayedPoints, _fontSize, _offsetX, _offsetY);
                }

                _lastError = null;
            }
            catch (Exception e)
            {
                _playedOverlay.Hide();
                if (_lastError != e.Message)
                {
                    _lastError = e.Message;
                    SdkLog.Warn("BattleCardPoints", "点数显示暂不可用：" + e.Message);
                }
            }
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

            _playedOverlay.Dispose();
        }

        public override void OnUnload()
        {
            Unsubscribe();
        }
    }
}
