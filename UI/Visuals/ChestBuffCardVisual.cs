
using Game.BuffSystem;
using Game.UI;
using Nova;
using Nova.TMP;
using UnityEngine;

[System.Serializable]
public class ChestBuffCardVisual : BuffCardVisual
{
    public UIBlock2D Icon;
    public TextMeshProTextBlock TextModifier;

    public MODIFIER_RARITY _modifierRarity = MODIFIER_RARITY.MYTHIC;

    public Color _mainColor = Color.white;
    public Color _hoverColor = Color.white;
    public Color _mainGradientColor = Color.white;

    public override void Bind(int index, BuffCard buffCard)
    {
        base.Bind(index, buffCard);

        RarityDefinition modRarity = buffCard.RarityDefinition;

        ChanceColor(modRarity);

        _mainColor = modRarity.MainColor;
        _hoverColor = modRarity.HoverColor;
        _mainGradientColor = modRarity.MainGradientColor;
    }

    public void ChanceColor(RarityDefinition rarity)
    {
        UI_ColorAnimationBase colorAnimation = this.View.GetComponent<UI_ColorAnimationBase>();
        if (colorAnimation != null)
        {
            //colorAnimation.ChangeBodyColor(uiBlock, color);
            colorAnimation.ChangeMainBodyColor(rarity.MainColor);
            colorAnimation.ChangeMainGradient(rarity.MainGradientColor);

            colorAnimation.ChangeHoverBodyColor(rarity.HoverColor);
            colorAnimation.ChangeHoverGradient(rarity.HoverGradientColor);
        }
        else
        {
            Debug.LogWarning("UI_ColorAnimationBase component not found on the View.");
        }

        //Debug.Log("ChanceColor: " + rarity.Rarity + " - " + rarity.MainColor + " - " + rarity.HoverColor);
    }

    protected override void DisableAll()
    {
        Icon.gameObject.SetActive(false);
        TextModifier.gameObject.SetActive(false);
    }

}