public enum DAMAGE_SOURCE
{
    PLAYER,
    ENEMY,
    ENVIRONMENT,
    GET_OWNER,
    FALL,
    PHYSIC_HAND,


}

public static class DamageSourceHelper
{
    public static DAMAGE_SOURCE GetSourceFromOwner(OWNER_TYPE ownerType)
    {
        switch (ownerType)
        {
            case OWNER_TYPE.PLAYER:
                return DAMAGE_SOURCE.PLAYER;
            case OWNER_TYPE.ENEMY:
                return DAMAGE_SOURCE.ENEMY;
            case OWNER_TYPE.PHYSIC_HAND:
                return DAMAGE_SOURCE.PHYSIC_HAND;
            default:
                return DAMAGE_SOURCE.ENVIRONMENT;
        }
    }
}