using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        protected bool _showSoundDataInspector = false;
        [SerializeField, ShowIf("_showSoundDataInspector")] protected AudioSoundData _soundData;
        private AudioSource _audioSource;

        protected bool _showOverrideAudioSourceInspector = false;
        [Tooltip("Override the AudioSource values, the scriptable values will be ignored. Clip will be set and also if mixer group is null it will be set.")]
        [SerializeField, ShowIf("_showOverrideAudioSourceInspector")] private bool _overrideAudioSourceValues = false;
        //private Coroutine _playCoroutine;

        protected bool _enablePooling = true;
        private bool _hasReturnedToPool = false;

        protected virtual void Awake()
        {
            _audioSource = gameObject.GetOrAdd<AudioSource>();
        }

        public void Initialize(AudioSoundData soundData)
        {
            _soundData = soundData;

            if(_overrideAudioSourceValues)
            {
                _audioSource.clip = soundData.Clip;

                if(soundData.MixerGroup == null) _audioSource.outputAudioMixerGroup = soundData.MixerGroup;

                return;
            }
            _audioSource.clip = soundData.Clip;
            _audioSource.outputAudioMixerGroup = soundData.MixerGroup;
            _audioSource.loop = soundData.Loop;
            _audioSource.playOnAwake = soundData.PlayOnAwake;
            _audioSource.volume = soundData.Volume;
            _audioSource.pitch = soundData.Pitch;
            _audioSource.panStereo = soundData.StereoPan;
        }

        public void Play()
        {
            if(!_enablePooling)
            {
                _audioSource.Play();
                return;
            }

            _hasReturnedToPool = false;
            //if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            ActionScheduler.CancelActions(this.gameObject.GetInstanceID());

            _audioSource.Play();

            //_playCoroutine = StartCoroutine(WaitForSoundToEnd());

            ActionScheduler.RunWhenTrue(() => !_audioSource.isPlaying, () =>
            {
                if (ManagerSound.Instance != null && ManagerSound.Instance.IsDebug()) Debug.Log($"Sound {_soundData.Clip.name} finished playing.");

                Release();
            }, this.gameObject.GetInstanceID());
        }

        // private IEnumerator WaitForSoundToEnd()
        // {
        //     yield return new WaitWhile(() => _audioSource.isPlaying);

        //     if(ManagerSound.Instance != null && ManagerSound.Instance.IsDebug()) Debug.Log($"Sound {SoundData.Clip.name} finished playing.");

        //     Release();
        // }

        public void Stop(bool returnPool = true)
        {
            // if (_playCoroutine != null)
            // {
            //     StopCoroutine(_playCoroutine);
            //     _playCoroutine = null;
            // }

            if(!_enablePooling) 
            {
                _audioSource.Stop();
                return;
            }

            ActionScheduler.CancelActions(this.gameObject.GetInstanceID());
            _audioSource.Stop();

            if(returnPool)
                Release();
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            _audioSource.pitch += UnityEngine.Random.Range(min, max);
        }

        private void Release()
        {
            if (_hasReturnedToPool) return;
            
            _hasReturnedToPool = true;
            ManagerSound.Instance.ReturnToPool(this);
        }

        public AudioSoundData GetSoundData()
        {
            return _soundData;
        }


    }
}