

using Game.StatSystem;
using UnityEngine;

public static class DamageCalculator
{
    /// <summary>
    /// Attempts to calculate and apply damage from the dealer to the receiver.
    /// Returns true if damage was successfully applied, false otherwise.
    /// </summary>
    public static bool CalculateDamage(IDamageDealer dealer, IDamageReceiver receiver, StatList dealerStats = null, StatList receiverStats = null)
    {
        if (dealer == null || receiver == null)
        {
            return false;
        }

        if (ManagerPause.IsPaused())
        {
            return false;
        }

        DAMAGE_SOURCE dealerSource = dealer.GetDamageSource();

        DAMAGE_SOURCE[] acceptedSources = receiver.GetAcceptedDamageSource();
        if (acceptedSources == null || acceptedSources.Length == 0)
        {
            return false;
        }

        IOwner receiverOwner = receiver.GetTransform().GetComponent<IOwner>().GetRootOwner();

        Player player = Player.Instance;

        float critChance = 0f;
        if (player != null &&
            player.GetStatList().TryGetStat(STAT_TYPE.CRIT_CHANCE, out Stat playerCritStat))
        {
            critChance += playerCritStat.GetValue();
        }

        if (dealerStats != null &&
            dealerStats.TryGetStat(STAT_TYPE.CRIT_CHANCE, out Stat dealerCritStat))
        {
            critChance += dealerCritStat.GetValue();
        }

        float critMultiplier = 0f;

        if (player != null &&
            player.GetStatList().TryGetStat(STAT_TYPE.CRIT_MULTIPLIER, out Stat playerCritMultStat))
        {
            critMultiplier += playerCritMultStat.GetValue();
        }

        if (dealerStats != null)
        {
            critMultiplier += dealerStats.GetValueOfStat(STAT_TYPE.CRIT_MULTIPLIER, 0f);
        }

        if (critMultiplier < 0f)
            critMultiplier = CONSTANTS.DEFAULT_CRIT_MULTIPLIER;

        bool isCritical = Random.Range(0f, 100f) <= critChance;
        if (isCritical)
        {
            dealer.SetCriticalMultiplier(critMultiplier);
        }

        foreach (DAMAGE_SOURCE source in acceptedSources)
        {
            if (source == dealerSource)
            {
                if (!Events.OnDamageDealt.Invoke(dealer, receiver))
                {
                    Debug.Log("Damage was canceled by a listener");
                    continue;
                }

                receiver.GetHealth().TakeDamage(dealer);

                if (ManagerFloatingDamages.Instance != null
                && receiverOwner.GetOwnerType() == OWNER_TYPE.PLAYER
                || receiverOwner.GetOwnerType() == OWNER_TYPE.ENEMY)
                {
                    //these could be moved inside IDamageReceiver, maybe in future
                    if(receiverOwner.GetOwnerType() == OWNER_TYPE.PLAYER)
                    {
                        UI_ManagerWorldIndicators.Instance.CreateFloatingDamage(
                            receiver.GetTransform(),
                            INDICATOR_TYPE.PLAYER_GOT_HIT,
                            dealer.GetDamage() * -1f); // NEGATIVE, just for SHOW
                    }
                    else if (!isCritical)
                    {
                        UI_ManagerWorldIndicators.Instance.CreateFloatingDamage(
                            receiver.GetTransform(),
                             INDICATOR_TYPE.DAMAGE,
                              dealer.GetDamage());
                    }
                    else
                    {
                        UI_ManagerWorldIndicators.Instance.CreateFloatingDamage(
                            receiver.GetTransform(),
                            INDICATOR_TYPE.DAMAGE_CRITICAL,
                             dealer.GetDamage());
                    }
                }

                if (isCritical)
                    dealer.SetCriticalMultiplier(-1f); // Reset critical multiplier after applying damage
                return true;
            }
        }

        if (isCritical)
            dealer.SetCriticalMultiplier(-1f); // Reset critical multiplier after applying damage

        return false;
    }


}
