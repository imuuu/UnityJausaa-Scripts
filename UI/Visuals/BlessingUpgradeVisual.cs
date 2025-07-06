using System.Collections.Generic;
using Game.StatSystem;
using Nova;
using Nova.TMP;

public class BlessingUpgradeVisual : ItemVisuals
{
    public UIBlock2D Icon;
    public TextMeshProTextBlock TextBlessingName;
    public TextMeshProTextBlock TextBlessingDescription;
    public TextMeshProTextBlock TextUpgradeValue;
    public TextMeshProTextBlock TextTier;

    public UI_IconAndValueList IconAndValueList;

    public UI_Multibar RankIndicators;

    private BlessingGroup _blessingGroup;

    public void Bind(BlessingGroup blessGroup)
    {
        _blessingGroup = blessGroup;
        BlessingUpgrade blessingUpgrade = blessGroup.GetCurrentUpgrade();

        Icon.SetImage(blessGroup.GroupIcon);
        TextBlessingName.text = blessGroup.GroupName;
        TextBlessingDescription.text = blessGroup.GetCurrentDescription();

        Modifier blessingModifier = blessGroup.GetAllProgressedModifiers();
        float value = blessingModifier == null ? 0f : blessingModifier.GetValue();
        TextUpgradeValue.text = string.Format(blessGroup.GetCurrentValuePrefix(), value);

        int tier = blessGroup.CurrentTier;
        string tierText = string.Format(BlessingUpgradeGrouper.TierPrefix, tier);
        TextTier.text = tierText;
        int maxRank = blessingUpgrade.MaxRanks;
        int rank = blessingUpgrade.CurrentRank;

        RankIndicators.SetSlotAmount(maxRank, rank);
 
        ActionScheduler.RunAfterDelay(0.1f, () => RankIndicators.SetToggles(rank));

        UpdateCurrencies();
    }

    public void UpdateCurrencies()
    {
        BlessingUpgrade blessingUpgrade = _blessingGroup.GetCurrentUpgrade();

        var data = blessingUpgrade.GetCostForCurrentRank();

        List<CurrencyAmount> currencyAmounts = new();
        foreach (var currency in data)
        {
            if (currency.Amount > 0)
            {
                currencyAmounts.Add(currency);
            }
        }

        if (currencyAmounts.Count == 0)
        {
            return;
        }

        IconAndValueList.SetCurrencies(currencyAmounts);
    }
}