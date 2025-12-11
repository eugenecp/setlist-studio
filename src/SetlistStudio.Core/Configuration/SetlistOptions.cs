namespace SetlistStudio.Core.Configuration;

public class SetlistOptions
{
    /// <summary>
    /// Base transition overhead in seconds between songs
    /// </summary>
    public int BaseTransitionSeconds { get; set; } = 15;

    /// <summary>
    /// Multiplier for BPM difference to compute transition penalty (seconds per BPM difference unit)
    /// </summary>
    public double BpmDifferencePenaltyMultiplier { get; set; } = 0.2;

    /// <summary>
    /// Penalty in seconds when keys are incompatible
    /// </summary>
    public int KeyMismatchPenaltySeconds { get; set; } = 10;

    /// <summary>
    /// Default fallback song duration in seconds when no metadata is available
    /// </summary>
    public int DefaultSongDurationSeconds { get; set; } = 180;

    /// <summary>
    /// Recommended performance slot durations in minutes
    /// </summary>
    public List<int> SlotDurationsMinutes { get; set; } = new List<int> { 45, 60, 90 };

    /// <summary>
    /// Maximum transition seconds allowed (safety cap)
    /// </summary>
    public int MaxTransitionSeconds { get; set; } = 120;

    /// <summary>
    /// BPM difference threshold considered 'large' (optional behaviour)
    /// </summary>
    public int LargeBpmThreshold { get; set; } = 20;
}
