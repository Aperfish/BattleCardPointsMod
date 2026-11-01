using System;
using CesiumLoader.SDK.Gameplay;
using CesiumLoader.SDK.Logging;
using CesiumLoader.SDK.Manifests;
using CesiumLoader.SDK.Mods;
using GameLogic;
using party.model;
using Tools;
using UI;
using UnityEngine;

[assembly: ModManifest("战斗牌点数显示", "1.0.3", "Aperfish",
    "在每张战斗牌上方直接显示点数（手牌上方显示预估点数范围，打出后显示实际点数）。",
    Permissions = ModPermission.ReadGameState, SdkVersion = "2.9.1")]

namespace BattleCardPointsMod
{
    public static class ModEntry
    {
        public static void Main() { ModBase.Run(new PointsMod()); }
    }

    public sealed class PointsMod : ModBase
    {
        private readonly HandCardBadgeManager _handBadges = new HandCardBadgeManager();
        private readonly PlayedCardsOverlay _playedOverlay = new PlayedCardsOverlay();

        private bool _enabled = true;
        private bool _showForOthers = true;
        private bool _showHandPoints = true;
        private bool _showPlayedPoints = true;
        private int _fontSize = 26;
        private float _offsetX = 0f;
        private float _offsetY = 0f;

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
            GameEvents.CardUsed += OnCard;
            GameEvents.EffectCardUsed += OnCard;
            GameEvents.QuickCardUsed += OnCard;

            Context.RegisterCleanup(Unsubscribe);
            GameEvents.StartAutoHook();
            SdkLog.Info("BattleCardPoints", "已启用：在每张战斗牌上方直接显示点数。");
        }

        public override void OnConfigChanged()
        {
            if (Config == null) return;
            _enabled = Config.GetBool("Enabled", true);
            _showForOthers = Config.GetBool("ShowForOthers", true);
            _showHandPoints = Config.GetBool("ShowHandPoints", true);
            _showPlayedPoints = Config.GetBool("ShowPlayedPoints", true);
            _fontSize = Math.Max(16, Math.Min(48, Config.GetInt("FontSize", 26)));
            _offsetX = (float)Config.GetDouble("OffsetX", 0);
            _offsetY = (float)Config.GetDouble("OffsetY", 0);
            _dirty = true;
        }

        private void OnBattle(Battle b)
        {
            _dirty = true;
        }

        private void OnPhase(Battle b, BattlePhase phase)
        {
            _dirty = true;
        }

        private void OnCard(long playerId, int cardId, int unused)
        {
            _dirty = true;
        }

        public override void OnUpdate()
        {
            if (!_enabled)
            {
                _playedOverlay.Hide();
                return;
            }

            float now = Time.unscaledTime;
            if (!_dirty && now < _nextRefresh) return;
            _dirty = false;
            _nextRefresh = now + 0.08f;

            try
            {
                FightWindow fightWin = null;
                try { fightWin = UIManager.inst?.Fight; } catch { }

                if (fightWin == null || !fightWin.isShowing)
                {
                    _playedOverlay.Hide();
                    return;
                }

                // 1. 手牌上方点数预估范围
                _handBadges.UpdateBadges(fightWin, _showHandPoints, _fontSize);

                // 2. 已打出卡牌上方实际点数
                var live = SimpleSingletonProvider<GameLogicManager>.inst?.fight?.battleFightData;
                if (live != null && !live.isEnd && live.attackerInfo != null && live.defenderInfo != null)
                {
                    if (!_showForOthers && !Players.IsSelf(live.attackerInfo.PlayerId) && !Players.IsSelf(live.defenderInfo.PlayerId))
                    {
                        _playedOverlay.Hide();
                    }
                    else
                    {
                        var pane = fightWin.contentPane as UIFightWindow;
                        _playedOverlay.Update(pane, live.attackerInfo, live.defenderInfo, _showPlayedPoints, _fontSize, _offsetX, _offsetY);
                    }
                }
                else
                {
                    _playedOverlay.Hide();
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
            GameEvents.CardUsed -= OnCard;
            GameEvents.EffectCardUsed -= OnCard;
            GameEvents.QuickCardUsed -= OnCard;

            _playedOverlay.Dispose();
        }

        public override void OnUnload()
        {
            Unsubscribe();
        }
    }
}
