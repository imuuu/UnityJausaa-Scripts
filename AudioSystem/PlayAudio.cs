using DarkTonic.MasterAudio;
using Sirenix.OdinInspector;

using UnityEngine;

public class PlayAudio : MonoBehaviour
{
    [Title("Master Audio")]
    [Tooltip("The On Enable Not First Is POOL friendly")]
    [BoxGroup("PlayMode Type", ShowLabel = false)]
    [PropertySpace(0, 8)]
    [SerializeField, EnumToggleButtons] private PLAY_MODE _playMode = PLAY_MODE.ON_ENABLED_NOT_FIRST;

    [BoxGroup("Sound Category")]
    [GUIColor(0.8f, 0.8f, 1f)]
    [PropertySpace(10, 10)]
    [EnumToggleButtons] public SOUND_CATEGORY _soundCategory;

    [BoxGroup("Sound Group")]
    [GUIColor(0.8f, 0.8f, 1f)]
    [PropertySpace(10, 10)]
    [ShowIf("@_soundCategory == SOUND_CATEGORY.SOUND_GROUP")]
    [SoundGroup] public string SoundName;

    [ShowIf("@_soundCategory == SOUND_CATEGORY.PLAYLIST")]
    [PropertySpace(10, 10)]
    [SerializeField] public string PlaylistName;

    private bool _initialized = false;

    private static float _playlistVolume = -1f;

    public enum PLAY_MODE
    {
        ON_ENABLED_NOT_FIRST,
        ON_ENABLED,
        MANUAL,
    }

    public enum SOUND_CATEGORY
    {
        SOUND_GROUP,
        PLAYLIST,
    }

    private void OnEnable()
    {
        if (_playMode == PLAY_MODE.ON_ENABLED)
        {
            Play();
        }

        if (_initialized && _playMode == PLAY_MODE.ON_ENABLED_NOT_FIRST)
        {
            Play();
        }

        _initialized = true;
    }

    public void Play()
    {
        if (_soundCategory == SOUND_CATEGORY.SOUND_GROUP)
        {
            MasterAudio.PlaySoundAndForget(SoundName);
            return;
        }

        if (_soundCategory == SOUND_CATEGORY.PLAYLIST)
        {
            var playlistControllers = PlaylistController.Instances;
            if(_playlistVolume < 0f)
            {
                _playlistVolume = playlistControllers[0].PlaylistVolume;
            }
            playlistControllers[0].FadeToVolume(0.0f,1);

            ActionScheduler.RunAfterDelay(1.3f, () =>
            {
                playlistControllers[0].StopPlaylist();
                playlistControllers[0].FadeToVolume(_playlistVolume, 0.4f);
                ActionScheduler.RunAfterDelay(0.5f, () =>
                {
                    MasterAudio.StartPlaylist(PlaylistName);
                });
            });
            return;
        }
    }
}
    
