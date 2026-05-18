using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services.Voice;

namespace KrnlAI.Desktop.Tests.Services.Voice;

public sealed class ProsodyAnalyzerTests
{
    [Fact]
    public void Analyze_SilentPcm_ShouldReturnZeroEnergy()
    {
        var pcm = new byte[16000];
        var analyzer = new ProsodyAnalyzer();
        var result = analyzer.Analyze(pcm);
        Assert.Equal(0, result.Energy);
    }

    [Fact]
    public void Analyze_HighAmplitudeSine_ShouldDetectEnergy()
    {
        var pcm = GenerateSineWave(220, 0.5, 1.0);
        var analyzer = new ProsodyAnalyzer();
        var result = analyzer.Analyze(pcm);
        Assert.True(result.Energy > 0.1);
    }

    [Fact]
    public void Analyze_220HzTone_ShouldEstimatePitch()
    {
        var pcm = GenerateSineWave(220, 0.5, 2.0);
        var analyzer = new ProsodyAnalyzer();
        var result = analyzer.Analyze(pcm);
        Assert.InRange(result.PitchMean, 100, 300);
    }

    [Fact]
    public void EstimateEmotion_HighPitchHighEnergy_ShouldReturnAngry()
    {
        var result = new ProsodyAnalyzer().EstimateEmotion(new ProsodyFeatures(
            Energy: 0.35, PitchMean: 250, PitchStd: 70, SpeakingRate: 4.0, VoiceQuality: 1.0));
        Assert.Equal(UserEmotion.Angry, result);
    }

    [Fact]
    public void EstimateEmotion_LowPitchLowEnergy_ShouldReturnSad()
    {
        var result = new ProsodyAnalyzer().EstimateEmotion(new ProsodyFeatures(
            Energy: 0.05, PitchMean: 120, PitchStd: 10, SpeakingRate: 1.5, VoiceQuality: 0.2));
        Assert.Equal(UserEmotion.Sad, result);
    }

    [Fact]
    public void EstimateEmotion_HighPitchHighRate_ShouldReturnHappy()
    {
        var result = new ProsodyAnalyzer().EstimateEmotion(new ProsodyFeatures(
            Energy: 0.3, PitchMean: 220, PitchStd: 30, SpeakingRate: 4.5, VoiceQuality: 0.7));
        Assert.Equal(UserEmotion.Happy, result);
    }

    private static byte[] GenerateSineWave(double freqHz, double amplitude, double durationSec)
    {
        var sampleRate = 16000;
        var samples = (int)(sampleRate * durationSec);
        var pcm = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            var value = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * freqHz * i / sampleRate));
            pcm[i * 2] = (byte)(value & 0xFF);
            pcm[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }
        return pcm;
    }
}
