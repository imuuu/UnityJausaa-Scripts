using Sirenix.OdinInspector;
namespace Game.UI
{
    public class UI_XpBar : UI_ProgressBar
    {
        protected override void Start()
        {
        }

        private void OnEnable()
        {
            Events.OnPlayerXpChange.AddListener(OnXpChanged);
            ActionScheduler.RunAfterDelay(0.3f,() =>
            {
                if (ManagerXp.Instance != null)
                {
                    Percent = ManagerXp.Instance.GetXpPercent();
                }
                else Percent = 0;

                UpdateProgressVisuals();
            });
        }

        private void OnDisable()
        {
            Percent = 0;
            Events.OnPlayerXpChange.RemoveListener(OnXpChanged);
        }

        private bool OnXpChanged(float percent)
        {
            Percent = percent;
            UpdateProgressVisuals();
            return true;
        }

        [Button]
        private void AddXp()
        {
            Percent = 0;
        }

        [Button]
        private void UpdateXP()
        {
            ActionScheduler.RunNextFrame(() =>
            {
                if (ManagerXp.Instance != null)
                {
                    Percent = ManagerXp.Instance.GetXpPercent();
                }
                else Percent = 0;

                UpdateProgressVisuals();
            });
        }
    }
}
