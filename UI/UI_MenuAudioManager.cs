using UnityEngine;

public class UI_MenuAudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _audioSource;
    [SerializeField]
    private AudioClip _hoverSound;

    public void PlayHoverSound()
    {
        if (_audioSource && _hoverSound)
        {
            _audioSource.PlayOneShot(_hoverSound);
        }
    }
}
