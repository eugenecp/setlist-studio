using System.Collections.Generic;

namespace SetlistStudio.Core.Models;

public class SetlistItemDurationDto
{
    public int SetlistSongId { get; set; }
    public int SongId { get; set; }
    public string SongTitle { get; set; } = string.Empty;
    public double ResolvedDurationSeconds { get; set; }
    public double? EstimatedDurationSeconds { get; set; }
    public double? CustomDurationOverrideSeconds { get; set; }
    public double PredictedTransitionSecondsToNext { get; set; }
    public int Position { get; set; }
}

public class SetlistDurationResult
{
    public double TotalSongSeconds { get; set; }
    public double TotalTransitionSeconds { get; set; }
    public double CombinedTotalSeconds { get; set; }
    public IEnumerable<SetlistItemDurationDto> Items { get; set; } = new List<SetlistItemDurationDto>();
}
