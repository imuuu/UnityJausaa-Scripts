
using Game.UI;
using Game.SkillSystem;
using Game.ChunkSystem;
using UnityEngine;
public static class Events
{
    //public static readonly EventPoint onGameStart = new EventPoint();
    //public static readonly EventPoint OnLevelLoad = new EventPoint();
    //public static readonly EventPoint OnAfterLevelLoad = new EventPoint();
    //public static readonly EventPoint OnBeforeLevelLoad = new EventPoint();

    public static readonly EventPoint<Player> OnPlayerSet = new();
    public static readonly EventPoint OnPlayerDeath = new();

    public static readonly EventPoint<IDamageDealer, IDamageReceiver> OnDamageDealt = new();
    public static readonly EventPoint<Transform, IHealth> OnDeath = new();

    /// <summary>
    /// returns slot index, not key code! 0 to CONSTANTS.MAX_PLAYER_SKILLS
    /// </summary>
    public static readonly EventPoint<int> OnSkillButtonDown = new();
    public static readonly EventPoint<int> OnSkillButtonUp = new();
    public static readonly EventPoint<int> OnSkillButtonHold = new();

    public static readonly EventPoint<PAGE_TYPE> OnUIPageButtonPress = new();

    public static readonly EventPoint<PAGE_TYPE> OnUIPageOpen = new();
    public static readonly EventPoint<PAGE_TYPE> OnUIPageClose = new();
    public static readonly EventPoint<int, SkillDefinition> OnAddPlayerSkill = new();
    public static readonly EventPoint<int, SkillDefinition> OnRemovePlayerSkill = new();

    public static readonly EventPoint<Chunk> OnChunkLoad = new();
    public static readonly EventPoint<Chunk> OnChunkUnload = new();

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Current Xp Between 0-1 </returns>
    public static readonly EventPoint<float> OnPlayerXpChange = new();
    public static readonly EventPoint<int> OnPlayerLevelChange = new();

    public static readonly EventPoint<PAUSE_REASON> OnGamePause = new();
    public static readonly EventPoint<PAUSE_REASON> OnGameUnPause = new();

    public static readonly EventPoint OnBuffCardsOpen = new();
    public static readonly EventPoint<int> OnBuffCardSelected = new();

    public static readonly EventPoint<float> OnRoundTimerUpdated = new();


    public static readonly EventPoint<SCENE_NAME> OnPlayableScenePreloadStart = new();
    public static readonly EventPoint<SCENE_NAME> OnPlayableScenePreloadReady = new();
    public static readonly EventPoint<SCENE_NAME, float> OnPlayableSceneStaticWaitBeforeLoad = new();
    public static readonly EventPoint<SCENE_NAME> OnActivatePreloadedScene = new();


    /// <summary>
    /// Called when the scene changes. Not every scene will be triggered. Only for example: Lobby and ToxicLevel.
    /// </summary>
    /// <returns></returns>
    public static readonly EventPoint<SCENE_NAME> OnPlayableSceneChangeEnter = new();

    public static readonly EventPoint<float> OnPlayerDamageTaken = new();

    //public static readonly EventPoint OnPlayerHit = new();

    /// <summary>
    /// Called when the player get currency. Returns the type of currency and the amount added.
    /// </summary> <summary>
    public static readonly EventPoint<CURRENCY, int> OnCurrencyAdded = new();

    /// <summary>
    /// Called when the player spends currency. Returns the type of currency and the current amount after change.
    /// </summary>
    public static readonly EventPoint<CURRENCY, int> OnCurrencyBalanceChange = new();


    /// <summary>
    /// Called when block happens
    /// </summary>
    public static EventPoint<IDamageDealer, IDamageReceiver> OnBlockHappened = new();

    /// <summary>
    /// Called when the player interacts with an object in 3D space normal using the "E" key.
    /// </summary>
    public static readonly EventPoint OnInteract = new();
    public static readonly EventPoint OnEscButtonPress = new();

    public static EventPoint OnPlayerStatsUpdated = new(); 
    
    

    //public static readonly EventPoint<GroupSpawner,GameObject> OnSpawnerObjectDetach = new EventPoint<GroupSpawner,GameObject>();

    // public static readonly EventPoint<ButtonPress> OnButtonPressBefore = new EventPoint<ButtonPress>();
    // public static readonly EventPoint<ButtonPress> OnButtonPress = new EventPoint<ButtonPress>();
    // public static readonly EventPoint<ButtonPress> OnButtonPressAfter = new EventPoint<ButtonPress>();

    // // Analytics
    // public static readonly EventPoint PressPlay = new EventPoint();
    // public static readonly EventPoint OpenMenu = new EventPoint();
    // public static readonly EventPoint<int, int> CarUnlocked = new EventPoint<int, int>();
    // public static readonly EventPoint<int, int> LevelUnlocked = new EventPoint<int, int>();
    // public static readonly EventPoint<bool> EndOfLevelAdViewed = new EventPoint<bool>();
    // public static readonly EventPoint<LevelCompletedAnalyticsData> LevelCompleted = new EventPoint<LevelCompletedAnalyticsData>();
    // public static readonly EventPoint PressContinue = new EventPoint();

    // public static readonly EventPoint<ExampleEventThing> CardPlayed = new EventPoint<ExampleEventThing>();
    // public static readonly EventPoint<ExampleEventThing> CardPlayedManaOrBattle = new EventPoint<ExampleEventThing>();
}