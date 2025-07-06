public static class CONSTANTS
{
    public const string ROOT_SCENE_PATH = "Assets/Game/Scenes/RootScene.unity";
    public const string ROOT_SCENE_NAME = "RootScene";

    public const int MAX_PLAYER_SKILLS = 30;

    public const float MOB_STAND_TILT_THRESHOLD = 15f; // determines when a mob is considered standing, below this value
    public const float MOB_MOVE_THRESHOLD = 0.1f; // determines when a mob is considered moving, above this value
    public const float MOB_UPSIDE_DOWN_THRESHOLD = 1f; // determinates when a mob is considered upside down, in range from (180 -+value) degree where
    public const float MOB_UPSIDE_DOWN_VALUE = 180;

    public const int PLAYABLE_SCENE_INDEX_THRESHOLD = 100; // determines when a scene is considered playable, above this value

    public const float SCENE_LOAD_LOBBY_WAIT_TIME = 2.5f; // seconds to wait before loading a scene
    public const float SCENE_LOAD_LOBBY_WAIT_TIME_AFTER_PRELOAD = 1.5f; // seconds to wait after preloading a scene before loading it

    public const float SKILL_LOWEST_COOLDOWN = 0.1f; // minimum cooldown for skills, in seconds

    public const float DEFAULT_CRIT_MULTIPLIER = 200f; // default critical hit multiplier, in percent
}