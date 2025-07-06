using Nova.TMP;
using UnityEngine;

namespace Game.UI
{
    public class UI_PlayerRoundLevel : MonoBehaviour
    {
        [SerializeField] private TextMeshProTextBlock _textLevel;

        private void Start()
        {
            ActionScheduler.RunNextFrame(() =>
            {
                if (ManagerXp.Instance != null)
                {
                    OnPlayerLevelChange(ManagerXp.Instance.GetCurrentLevel());
                    return;
                }
                //else Percent = 0;
                //UpdateProgressVisuals();
            });
        }

        private void OnEnable()
        {
            Events.OnPlayerLevelChange.AddListener(OnPlayerLevelChange);
            if (ManagerXp.Instance != null)
            {
                OnPlayerLevelChange(ManagerXp.Instance.GetCurrentLevel());
            }
        }

        private void OnDisable()
        {
            Events.OnPlayerLevelChange.RemoveListener(OnPlayerLevelChange);
        }

        private bool OnPlayerLevelChange(int level)
        {
            _textLevel.text = level.ToString();
            return true;
        }
    }
}