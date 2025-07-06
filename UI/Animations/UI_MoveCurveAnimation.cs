using Nova;
using System;
using UnityEngine;

public enum DIRECTION
{
    HORIZONTAL,
    VERTICAL
}

[Serializable]
public struct UI_MoveCurveAnimation : IAnimation
{
    public UIBlock Target;
    public AnimationCurve Curve;
    public DIRECTION Direction;
    public float EndValue;
    public bool Yoyo;

    private Vector3 _startPosition;

    public void Update(float percentDone)
    {
        if (percentDone == 0f)
        {
            _startPosition = Target.Position.Value;
        }

        float remappedPercent = Yoyo
            ? (percentDone <= 0.5f ? percentDone * 2f : (1f - percentDone) * 2f)
            : percentDone;

        float curveValue = Curve.Evaluate(remappedPercent);

        Vector3 offset = Direction == DIRECTION.HORIZONTAL
            ? new Vector3(curveValue * EndValue, 0f, 0f)
            : new Vector3(0f, curveValue * EndValue, 0f);

        Target.Position.Value = _startPosition + offset;
    }
}
