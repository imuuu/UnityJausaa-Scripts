using System;
using Nova;
using UnityEngine;

public class IconAndValueVisual : ItemVisuals
{
    public UIBlock2D Icon;
    public TextBlock ValueText;

    public void Bind(CurrencyDefinition currencyDefinition)
    {
        Bind(currencyDefinition.Icon, ManagerCurrency.Instance.GetBalance(currencyDefinition.CurrencyType));
    }

    public void Bind(CurrencyAmount currencyAmount)
    {
        Bind(ManagerCurrency.Instance.GetIcon(currencyAmount.CurrencyType), currencyAmount.Amount);
    }

    public void Bind(Sprite icon, int value)
    {
        if (icon == null)
        {
            Unbind();
            return;
        }
        SetActive(true);

        Icon.SetImage(icon);
        ValueText.Text = value.ToString();
    }

    public void Unbind()
    {
        SetActive(false);
    }

   

    private void SetActive(bool active)
    {
        Icon.gameObject.SetActive(active);
        ValueText.gameObject.SetActive(active);
    }
}