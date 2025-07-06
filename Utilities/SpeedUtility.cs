using UnityEngine;
using Game.SkillSystem;

// --- Universal Speed Utility ---

public static class SpeedUtility
{
    /// <summary>
    /// Calculates current speed based on type and normalized progress t (0-1).
    /// </summary>
    public static float GetSpeed(SPEED_TYPE type, float baseSpeed, float maxSpeed, float t)
    {
        switch (type)
        {
            case SPEED_TYPE.FIXED:
                return baseSpeed;
            case SPEED_TYPE.ACCELERATE:
                return Mathf.Lerp(baseSpeed, maxSpeed, t);
            case SPEED_TYPE.DECELERATE:
                return Mathf.Lerp(baseSpeed, 0f, t);
            case SPEED_TYPE.ACCELERATE_DECELERATE:
                if (t < 0.5f)
                    return Mathf.Lerp(baseSpeed, maxSpeed, t * 2f);
                else
                    return Mathf.Lerp(maxSpeed, 0f, (t - 0.5f) * 2f);
            case SPEED_TYPE.DECELERATE_ACCELERATE:
                if (t < 0.5f)
                    return Mathf.Lerp(baseSpeed, 0f, t * 2f);
                else
                    return Mathf.Lerp(0f, maxSpeed, (t - 0.5f) * 2f);
            case SPEED_TYPE.RANDOM:
                return Random.Range(baseSpeed, maxSpeed);
            default:
                return baseSpeed;
        }
    }
}
