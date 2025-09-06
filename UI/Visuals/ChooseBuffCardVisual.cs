
using System.Collections.Generic;
using Game.BuffSystem;
using Game.SkillSystem;
using Game.StatSystem;
using Game.UI;
using Nova;
using Nova.TMP;
using UnityEngine;

[System.Serializable]
public class ChooseBuffCardVisual : BuffCardVisual
{
    public UIBlock2D Icon;
    public UIBlock2D NewIcon;
    //public UIBlock2D _backGround;
    public TextMeshProTextBlock TextSkillName;
    public TextMeshProTextBlock TextSkillDescriptionSmall;
    public TextMeshProTextBlock TextSkillDescription;

    public TextMeshProTextBlock TextModifier1;
    public TextMeshProTextBlock TextModifier2;
    public TextMeshProTextBlock TextModifier3;

    public MODIFIER_RARITY _modifierRarity = MODIFIER_RARITY.MYTHIC;

    public Color _mainColor = Color.white;
    public Color _hoverColor = Color.white;
    public Color _mainGradientColor = Color.white;

    public override void Bind(int index, BuffCard buffCard)
    {
        base.Bind(index, buffCard);
        // DisableAll();
        // buffCard.ApplyBuffToVisual(index, this);

        RarityDefinition modRarity = buffCard.RarityDefinition;

        ChanceColor(modRarity);

        _mainColor = modRarity.MainColor;
        _hoverColor = modRarity.HoverColor;
        _mainGradientColor = modRarity.MainGradientColor;
    }

    // public void Bind(int index, BuffDefinition buff)
    // {
    //     if (buff == null)
    //     {
    //         return;
    //     }

    //     SkillDefinition skillDefinition = ManagerSkills.Instance.GetSkillDefinition(buff.TargetSkill);

    //     if (skillDefinition.Icon != null) Icon.SetImage(skillDefinition.Icon);

    //     TextSkillName.text = skillDefinition.Name;

    //     //_backGround.Color = buff.ModRarity.MainColor;

    //     RarityDefinition modRarity;


    //     if (buff.IsNewSkill)
    //     {
    //         NewIcon.gameObject.SetActive(true);

    //         TextSkillDescription.gameObject.SetActive(true);
    //         TextSkillDescription.text = skillDefinition.Description;

    //         TextSkillDescriptionSmall.gameObject.SetActive(false);

    //         TextModifier1.gameObject.SetActive(false);
    //         TextModifier2.gameObject.SetActive(false);
    //         TextModifier3.gameObject.SetActive(false);

    //         modRarity = ManagerBuffs.Instance.GetRarityListHolder().GetRarityByName(MODIFIER_RARITY.MYTHIC);
    //     }
    //     else
    //     {

    //         List<Modifier> modifiers = buff.GetRandomModifiers();
    //         float probability = buff.Probability;
    //         ManagerBuffs.Instance.SetRollModifierForBuff(index, modifiers);
    //         ManagerBuffs.Instance.SetRollProbability(index, probability);

    //         modRarity = ManagerBuffs.Instance.GetRarityListHolder().GetRarityByThreshold(probability);

    //         modifiers = Modifier.CombineModifiers(modifiers);

    //         TextSkillDescription.gameObject.SetActive(false);
    //         TextSkillDescriptionSmall.gameObject.SetActive(true);
    //         TextSkillDescriptionSmall.text = skillDefinition.DescriptionSmall;

    //         TextModifier1.text = buff.GetModifierString(modifiers[0]);
    //         TextModifier1.gameObject.SetActive(true);

    //         if (modifiers.Count > 1)
    //         {
    //             TextModifier2.text = buff.GetModifierString(modifiers[1]);
    //             TextModifier2.gameObject.SetActive(true);
    //         }
    //         else
    //             TextModifier2.gameObject.SetActive(false);

    //         if (modifiers.Count > 2)
    //         {
    //             TextModifier3.text = buff.GetModifierString(modifiers[2]);
    //             TextModifier3.gameObject.SetActive(true);
    //         }
    //         else
    //             TextModifier3.gameObject.SetActive(false);


    //         NewIcon.gameObject.SetActive(false);
    //     }

    //     _modifierRarity = modRarity.Rarity;

    //     ChanceColor(modRarity);

    //     _mainColor = modRarity.MainColor;
    //     _hoverColor = modRarity.HoverColor;
    //     _mainGradientColor = modRarity.MainGradientColor;

    // }

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
        NewIcon.gameObject.SetActive(false);
        TextSkillDescription.gameObject.SetActive(false);
        TextSkillDescriptionSmall.gameObject.SetActive(false);
        TextModifier1.gameObject.SetActive(false);
        TextModifier2.gameObject.SetActive(false);
        TextModifier3.gameObject.SetActive(false);
    }
}