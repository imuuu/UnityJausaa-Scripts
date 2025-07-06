using Nova;
using UnityEngine;

public class UI_NovaMaterialProvider : MonoBehaviour
{
    [SerializeField]
    private UIBlock2D _uiBlock2D;

    [SerializeField]
    private Material _sheenMaterial;

    private void Start()
    {
        if (_uiBlock2D == null || _sheenMaterial == null)
        {
            Debug.LogError("UIBlock2D or SheenMaterial is not assigned.");
            return;
        }

        // Ensure the material has a main texture and cast it to Texture2D
        Texture mainTexture = _sheenMaterial.mainTexture;

        if (mainTexture is Texture2D texture2D)
        {
            _uiBlock2D.SetImage(texture2D);
        }
        else
        {
            Debug.LogError("Main texture is not a Texture2D.");
        }
    }
}
