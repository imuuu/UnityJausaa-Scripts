using Nova;
using Sirenix.OdinInspector;
using UnityEngine;

public class ManagerMouseInput : MonoBehaviour
{
    public static ManagerMouseInput Instance { get; private set; }
    public const uint MousePointerControlID = 1;
    public const uint ScrollWheelControlID = 2;

    /// <summary>Invert scrolling direction.</summary>
    public bool InvertScrolling = true;

    [Title("Debug to see the camera")]
    [ReadOnly]
    [SerializeField] private Camera _mainCamera;

    // Reunahavainnointiin RT:lle (virtuaali Mouse0)
    private bool _lastGamepadMouse0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void Update()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // 1) Määritä ruutukoordinaatti yhdestä lähteestä:
        //    - jos ohjain ohjaa kursoria → käytä virtuaalikurssoria
        //    - muuten käytä oikeaa hiirtä
        Vector2 screenPos = GamepadCursor.ControllingWithGamepad
            ? GamepadCursor.CurrentScreenPosition
            : (Vector2)Input.mousePosition;

        // 2) Muodosta ray
        Ray cursorRay = _mainCamera.ScreenPointToRay(screenPos);

        // 3) Scroll (vain jos oikea hiiri on läsnä ja scrollaa)
        Vector2 mouseScrollDelta = Vector2.zero;
        if (Input.mousePresent)
        {
            mouseScrollDelta = Input.mouseScrollDelta;
            if (mouseScrollDelta != Vector2.zero)
            {
                if (InvertScrolling) mouseScrollDelta.y *= -1f;
                var scrollInteraction = new Interaction.Update(cursorRay, ScrollWheelControlID);
                Interaction.Scroll(scrollInteraction, mouseScrollDelta);
            }
        }

        // 4) Klikki: hiiri tai RT (virtuaali Mouse0)
        bool mouse0Held = Input.GetMouseButton(0);
        bool gamepad0Held = GamepadCursor.GamepadMouse0;
        bool leftMouseButtonDown = mouse0Held || gamepad0Held;

        // (valinnainen) reunahavainnointi, jos tarvitset down/up -tietoa:
        bool gamepad0Down = !_lastGamepadMouse0 && gamepad0Held;
        bool gamepad0Up   =  _lastGamepadMouse0 && !gamepad0Held;
        _lastGamepadMouse0 = gamepad0Held;

        // 5) Syötä Nova:lle point-tilapäivitys
        var pointInteraction = new Interaction.Update(cursorRay, MousePointerControlID);
        Interaction.Point(pointInteraction, leftMouseButtonDown);

        // Jos joskus tarvitset erikseen painallus/release -signaalit,
        // voit kutsua lisämetodeja tähän perustuen (esim. oma logiikka).
        // Nova:n perus Point(..., pressed) riittää useimpiin UI- ja 3D-hit caseihin.
    }

    public bool TryGetCurrentRay(out Ray ray)
    {
        _mainCamera = Camera.main;
        if (_mainCamera != null)
        {
            Vector2 screenPos = GamepadCursor.ControllingWithGamepad
                ? GamepadCursor.CurrentScreenPosition
                : (Vector2)Input.mousePosition;

            ray = _mainCamera.ScreenPointToRay(screenPos);
            return true;
        }

        ray = default;
        return false;
    }
}
