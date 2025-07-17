using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[System.Serializable]
public class IndicatorConfig
{
    public INDICATOR_TYPE Type;
    [Tooltip("Prefab must have a UI_WorldIndicator component on its root.")]
    public GameObject Prefab;

    [Header("Appearance")]
    public bool EnableSprite = false;
    [ShowIf(nameof(EnableSprite))] public Sprite IconSprite;

    [HideIf(nameof(EnableTextColorGradient))]public bool EnableTextColor = false;
    [ShowIf("@EnableTextColor && !EnableTextColorGradient")] public Color TextColor = Color.white;

    public bool EnableTextColorGradient = false;
    [ShowIf(nameof(EnableTextColorGradient))]
    public VertexGradient TextColorGradient;
    public float StartScale = 1f;

    [Header("Layout")]
    public Vector3 WorldOffset = Vector3.up * 0.5f;
    [Tooltip("How long before it returns to pool")]
    public float Duration = 1f;

}
