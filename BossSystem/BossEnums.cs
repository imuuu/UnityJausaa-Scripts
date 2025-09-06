public enum BOSS_ID : ushort { NONE = 0, WARDEN = 1, ORACLE = 2, HYDRA = 3 }
public enum ARENA_TYPE : byte { CIRCLE = 0, SQUARE = 1, POLYGON = 2 }
public enum PHASE_TRIGGER_TYPE : byte { OnHPPercentDown, AfterTime, OnEvent }
public enum OUT_OF_BOUNDS_RULE : byte { Pushback, DamageOverTime, TeleportBack }
public enum ABILITY_ID : ushort { NONE = 0, BeamSweep = 1, OrbVolley = 2, Slam = 3, Nova = 4 }