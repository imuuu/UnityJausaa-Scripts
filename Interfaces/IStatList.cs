using Game.StatSystem;

public interface IStatList
{
    /// <summary>
    /// Get the value of a specific stat.
    /// </summary>
    /// <param name="statType">The type of stat to get.</param>
    /// <returns>The value of the specified stat.</returns>
    public float GetStatValue(STAT_TYPE statType);

    /// <summary>
    /// Set the value of a specific stat.
    /// </summary>
    /// <param name="statType">The type of stat to set.</param>
    /// <param name="value">The value to set for the specified stat.</param>
    //void SetStatValue(STAT_TYPE statType, float value);
    
    public StatList GetStatList();
}