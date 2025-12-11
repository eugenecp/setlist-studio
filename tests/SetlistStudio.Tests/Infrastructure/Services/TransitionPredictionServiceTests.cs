using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using FluentAssertions;
using SetlistStudio.Core.Configuration;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Services;

public class TransitionPredictionServiceTests
{
    private readonly TransitionPredictionService _service;

    public TransitionPredictionServiceTests()
    {
        var options = Options.Create(new SetlistOptions
        {
            BaseTransitionSeconds = 15,
            BpmDifferencePenaltyMultiplier = 0.2,
            KeyMismatchPenaltySeconds = 10,
            MaxTransitionSeconds = 120
        });

        _service = new TransitionPredictionService(options, new NullLogger<TransitionPredictionService>());
    }

    [Fact]
    public void PredictTransition_BothNull_ReturnsBase()
    {
        var result = _service.PredictTransition(null, null);
        result.TotalSeconds.Should().Be(15);
    }

    [Fact]
    public void PredictTransition_SameBpmAndCompatibleKeys_ReturnsBase()
    {
        var a = new Song { Id = 1, Bpm = 120, MusicalKey = "C" };
        var b = new Song { Id = 2, Bpm = 120, MusicalKey = "Cm" };

        var result = _service.PredictTransition(a, b);
        result.TotalSeconds.Should().BeApproximately(15, 0.001);
    }

    [Fact]
    public void PredictTransition_LargeBpmDifference_AddsPenalty()
    {
        var a = new Song { Id = 1, Bpm = 60 };
        var b = new Song { Id = 2, Bpm = 140 };

        var result = _service.PredictTransition(a, b);
        // bpm diff = 80, multiplier 0.2 => bpm penalty 16 -> total 31
        result.TotalSeconds.Should().BeApproximately(31, 0.001);
    }

    [Fact]
    public void PredictTransition_KeyMismatch_AddsPenalty()
    {
        var a = new Song { Id = 1, Bpm = 100, MusicalKey = "C" };
        var b = new Song { Id = 2, Bpm = 100, MusicalKey = "F#" };

        var result = _service.PredictTransition(a, b);
        // base 15 + key penalty 10 = 25
        result.TotalSeconds.Should().BeApproximately(25, 0.001);
    }

    [Fact]
    public void PredictTransition_MissingBpmOrKey_HandlesGracefully()
    {
        var a = new Song { Id = 1 };
        var b = new Song { Id = 2 };

        var result = _service.PredictTransition(a, b);
        result.TotalSeconds.Should().Be(15);
    }
}
