using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.AudioSystem
{
    public class SoundEmitterCustom : SoundEmitter
    {
        [SerializeField] private bool _playOnStart = false;
        
        private void Reset()
        {
            _showSoundDataInspector = true;
            _showOverrideAudioSourceInspector = true;
        }

        private void OnValidate()
        {
            _showSoundDataInspector = true;
            _showOverrideAudioSourceInspector = true;
        }

        protected override void Awake()
        {
            base.Awake();
            _enablePooling = false;

            Initialize(_soundData);
        }

        private void Start()
        {
            if (_playOnStart) Play();
        }

        [Button("Test Play")]
        [PropertySpace(20)]
        public void TestPlay()
        {
            Play();
        }

        [Button("Test Stop")]
        [PropertySpace(8)]
        public void TestStop()
        {
            Stop();
        }
        


    }
}