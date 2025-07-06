using UnityEngine;

public class PlayerUpgradeCard : MonoBehaviour
{
    private BlessingGroup _blessingGroup;
    private BlessingUpgradeVisual _visual;

    public void Bind(BlessingGroup blessingGroup, BlessingUpgradeVisual visual)
    {
        _blessingGroup = blessingGroup;
        _visual = visual;
    }

    public void Buy()
    {
        bool buyHappened = _blessingGroup.Buy();

        if (buyHappened)
        {
            _visual.Bind(_blessingGroup);
        }
    }

    public void Refund()
    {
        bool refundHappened = _blessingGroup.Refund();

        if (refundHappened)
        {
            _visual.Bind(_blessingGroup);
        }
    }
}