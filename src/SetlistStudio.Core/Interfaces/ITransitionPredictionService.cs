using System;
using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

public interface ITransitionPredictionService
{
    TimeSpan PredictTransition(Song? a, Song? b);
}
