
using UnityEngine;

namespace Game.AudioSystem
{
    public class SoundBuilder
    {
        private readonly ManagerSound _managerSound;
        private AudioSoundData _soundData;
        private Vector3 _position;
        private bool _randomPitch;

        public SoundBuilder(ManagerSound managerSound)
        {
            _managerSound = managerSound;
        }

        public SoundBuilder SetSoundData(AudioSoundData soundData)
        {
            _soundData = soundData;
            return this;
        }

        public SoundBuilder SetPosition(Vector3 position)
        {
            _position = position;
            return this;
        }

        public SoundBuilder SetRandomPitch(bool randomPitch)
        {
            _randomPitch = randomPitch;
            return this;
        }

        public void Play()
        {
            if(!_managerSound.CanPlaySound(_soundData)) return;

            SoundEmitter soundEmitter = _managerSound.GetSoundEmitter();
            
            soundEmitter.Initialize(_soundData);
            soundEmitter.transform.position = _position;

            soundEmitter.transform.SetParent(ManagerSound.Instance.transform);

            if(_randomPitch)
            {
                soundEmitter.WithRandomPitch();
            }

            if (_soundData.FrequentSound)
                _managerSound.RegisterFrequentEmitter(soundEmitter);

            soundEmitter.Play();

        }
    }
}