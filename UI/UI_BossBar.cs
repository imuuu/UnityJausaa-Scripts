using Nova;
using UnityEngine;
namespace Game.UI
{
    public class UI_BossBar : UI_ProgressBar
    {
        [SerializeField] private TextBlock _bossName;
        protected override void Start()
        {
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            Percent = 0;
        }

        public void SetBossName(string name)
        {
            _bossName.Text = name;
        }

    }
}
