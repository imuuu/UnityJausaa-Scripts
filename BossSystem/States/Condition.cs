using System;
using UnityEngine;

namespace Game.BossSystem
{
    public enum PercentReference : byte
    {
        FROM_FULL_HP,
        FROM_DAMAGE_TAKEN,
        HP_SCOPE,
    }

    public enum ConditionType : byte
    {
        None = 0,
        HpAtOrBelowPercent,
        HpAtOrAbovePercent,
        HpPercentInRange,             // uses value..valueB + percentRef
        PlayerWithinDistance,
        PlayerBeyondDistance,
        PhaseTimeAtLeast,
        RandomChance,

        // segment-size = 1 / childCount, window = ±segmentWindowHalfWidth around edges
        HpSegmentEdge_StaticChildren, // child count taken once from childCountRoot (cached)
        HpSegmentEdge_DynamicChildren // child count read each Evaluate from childCountRoot
    }

    [Serializable]
    public struct Condition
    {
        public ConditionType type;

        [Range(0f, 1f)] public float value;   // primary (hp%, chance, etc.)
        [Range(0f, 1f)] public float valueB;  // upper bound for HpPercentInRange
        public PercentReference percentRef;

        [Tooltip("Half-width around each segment edge (0..1). Example: 0.02 = ±2%.")]
        [Range(0f, 0.25f)] public float segmentWindowHalfWidth;

        [Tooltip("Where to count children from for the *Children conditions. If null, falls back to ctx.Boss.")]
        public Transform childCountRoot;

        public bool Evaluate(BossContext ctx)
        {
            switch (type)
            {
                case ConditionType.None:
                    return true;

                case ConditionType.HpAtOrBelowPercent:
                    return ctx != null && ctx.Hp01 <= value;

                case ConditionType.HpAtOrAbovePercent:
                    return ctx != null && ctx.Hp01 >= value;

                case ConditionType.HpPercentInRange:
                    {
                        if (ctx == null) return false;
                        float p = GetPercent(ctx, percentRef);
                        float a = Mathf.Min(value, valueB);
                        float b = Mathf.Max(value, valueB);
                        return p >= a && p <= b;
                    }

                case ConditionType.PlayerWithinDistance:
                    {
                        if (ctx == null || ctx.Player == null || ctx.Boss == null) return false;
                        Vector3 d = ctx.Player.position - ctx.Boss.position;
                        return d.sqrMagnitude <= value * value;
                    }

                case ConditionType.PlayerBeyondDistance:
                    {
                        if (ctx == null || ctx.Player == null || ctx.Boss == null) return false;
                        Vector3 d = ctx.Player.position - ctx.Boss.position;
                        return d.sqrMagnitude >= value * value;
                    }

                case ConditionType.PhaseTimeAtLeast:
                    return BossPhaseController.CurrentPhaseTime >= value;

                case ConditionType.RandomChance:
                    return UnityEngine.Random.value <= Mathf.Clamp01(value);

                case ConditionType.HpSegmentEdge_StaticChildren:
                    return EvaluateSegmentEdge(ctx, dynamicChildren: false);

                case ConditionType.HpSegmentEdge_DynamicChildren:
                    return EvaluateSegmentEdge(ctx, dynamicChildren: true);

                default:
                    return false;
            }
        }

        private static float GetPercent(BossContext ctx, PercentReference mode)
        {
            float hp01 = Mathf.Clamp01(ctx.Hp01);
            return mode == PercentReference.FROM_FULL_HP ? hp01 : (1f - hp01);
        }

        private bool EvaluateSegmentEdge(BossContext ctx, bool dynamicChildren)
        {
            if (ctx == null) return false;

            Transform root = childCountRoot != null ? childCountRoot : ctx.Boss;
            if (root == null) return false;

            int segments = dynamicChildren
                ? root.childCount
                : ChildCountCache.GetOrCaptureInitialChildCount(root);

            if (segments <= 0) return false;

            float p = GetPercent(ctx, percentRef);   // 0..1 (from full or from zero)
            float segSize = 1f / segments;

            // nearest edge at k*segSize
            float nearestEdge = Mathf.Round(p / segSize) * segSize;
            float delta = Mathf.Abs(p - nearestEdge);

            float w = Mathf.Min(Mathf.Abs(segmentWindowHalfWidth), segSize * 0.5f);
            return delta <= w;
        }
    }
}
