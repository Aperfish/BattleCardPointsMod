using System;
using System.Runtime.CompilerServices;
using CesiumLoader.SDK.Gameplay;
using Tools;

namespace BattleCardPointsMod
{
    public static class CardPointHelper
    {
        public struct PointRange
        {
            public int Min;
            public int Max;
            public bool IsAttack;
            public bool IsValid;

            public string GetRangeText()
            {
                if (!IsValid) return null;
                string color = IsAttack ? "#FF5555" : "#4DA6FF";
                if (Min == Max)
                {
                    string val = Min >= 0 ? $"+{Min}" : $"{Min}";
                    return $"[color={color}][b]{val}[/b][/color]";
                }
                return $"[color={color}][b]+{Min}～{Max}[/b][/color]";
            }
        }

        public static PointRange GetPointRange(int cardId)
        {
            if (cardId < 10001) return default;

            // 既有 13 张战斗牌快速确定（避免离线单测或启动早期触发 StaticConfigure 触发 Unity.ResourceManager 加载）
            switch (cardId)
            {
                case 10001: return new PointRange { Min = 1, Max = 3, IsAttack = true, IsValid = true };
                case 10002: return new PointRange { Min = 1, Max = 3, IsAttack = false, IsValid = true };
                case 10003: return new PointRange { Min = 1, Max = 6, IsAttack = true, IsValid = true };
                case 10004: return new PointRange { Min = 1, Max = 6, IsAttack = false, IsValid = true };
                case 10005: return new PointRange { Min = 1, Max = 10, IsAttack = true, IsValid = true };
                case 10006: return new PointRange { Min = 1, Max = 10, IsAttack = false, IsValid = true };
                case 10007: return new PointRange { Min = 1, Max = 20, IsAttack = true, IsValid = true };
                case 10008: return new PointRange { Min = 3, Max = 3, IsAttack = true, IsValid = true };
                case 10009: return new PointRange { Min = 5, Max = 5, IsAttack = true, IsValid = true };
                case 10010: return new PointRange { Min = 6, Max = 6, IsAttack = true, IsValid = true };
                case 10011: return new PointRange { Min = 0, Max = 0, IsAttack = true, IsValid = true };
                case 10012: return new PointRange { Min = 0, Max = 4, IsAttack = true, IsValid = true };
                case 10013: return new PointRange { Min = 3, Max = 3, IsAttack = true, IsValid = true };
            }

            // 未知战斗牌（大于 10013）尝试从运行时配表中动态获取
            try
            {
                return TryGetDynamicPointRange(cardId);
            }
            catch
            {
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static PointRange TryGetDynamicPointRange(int cardId)
        {
            try
            {
                if (StaticConfigure.Card.InfoDict != null && StaticConfigure.Card.InfoDict.TryGetValue(cardId, out var cfg))
                {
                    if (cfg != null && cfg.Params != null && cfg.Params.Count >= 2)
                    {
                        bool isAtk = cfg.EffectType == EffectType.Attack;
                        return new PointRange { Min = cfg.Params[0], Max = cfg.Params[1], IsAttack = isAtk, IsValid = true };
                    }
                }
            }
            catch { }

            return default;
        }

        public static string CardName(int cardId)
        {
            string name = Names.Card(cardId) ?? "战斗牌";
            string suffix = "(" + cardId + ")";
            return name.EndsWith(suffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - suffix.Length).TrimEnd()
                : name;
        }
    }
}
