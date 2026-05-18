using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Services.Voice;

public sealed record ProsodyFeatures(
    double Energy,
    double PitchMean,
    double PitchStd,
    double SpeakingRate,
    double VoiceQuality);

public sealed class ProsodyAnalyzer
{
    private const int SampleRate = 16000;
    private const double SilenceThreshold = 0.01;

    public ProsodyFeatures Analyze(byte[] pcmData)
    {
        var samples = ConvertToShorts(pcmData);
        if (samples.Length == 0)
            return new ProsodyFeatures(0, 0, 0, 0, 0);

        var energy = ComputeRmsEnergy(samples);
        var nonSilent = samples.Where(s => Math.Abs(s / 32768.0) > SilenceThreshold).ToList();
        var usable = nonSilent.Count > 10 ? nonSilent : samples.ToList();

        var (pitchMean, pitchStd) = ComputePitch(usable.ToArray());
        var speakingRate = EstimateSpeakingRate(samples);
        var quality = ClassifyVoiceQuality(energy, pitchStd);

        return new ProsodyFeatures(energy, pitchMean, pitchStd, speakingRate, quality);
    }

    public UserEmotion EstimateEmotion(ProsodyFeatures prosody)
    {
        if (prosody.PitchMean > 220 && prosody.Energy > 0.3 && prosody.PitchStd > 60)
            return UserEmotion.Angry;
        if (prosody.PitchMean > 200 && prosody.Energy < 0.15 && prosody.PitchStd > 50)
            return UserEmotion.Fearful;
        if (prosody.PitchMean < 140 && prosody.Energy < 0.1 && prosody.SpeakingRate < 2.0)
            return UserEmotion.Sad;
        if (prosody.PitchMean > 210 && prosody.Energy > 0.25 && prosody.SpeakingRate > 4.0)
            return UserEmotion.Happy;
        if (prosody.Energy > 0.3 && prosody.SpeakingRate > 4.5)
            return UserEmotion.Angry;
        if (prosody.Energy > 0.25 && prosody.PitchStd > 40)
            return UserEmotion.Surprised;
        if (prosody.SpeakingRate < 1.5)
            return UserEmotion.Sad;

        return UserEmotion.Neutral;
    }

    private static short[] ConvertToShorts(byte[] pcmData)
    {
        var count = pcmData.Length / 2;
        var result = new short[count];
        for (int i = 0; i < count; i++)
            result[i] = BitConverter.ToInt16(pcmData, i * 2);
        return result;
    }

    private static double ComputeRmsEnergy(short[] samples)
    {
        double sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            var normalized = samples[i] / 32768.0;
            sum += normalized * normalized;
        }
        return Math.Sqrt(sum / samples.Length);
    }

    private static (double mean, double stdDev) ComputePitch(short[] samples)
    {
        var frameSize = (int)(SampleRate * 0.03);
        var hopSize = frameSize / 2;
        if (frameSize < 2) return (0, 0);

        var pitches = new List<double>();
        for (int start = 0; start + frameSize < samples.Length; start += hopSize)
        {
            var pitch = EstimateFramePitch(samples, start, frameSize);
            if (pitch > 50 && pitch < 500)
                pitches.Add(pitch);
        }

        if (pitches.Count == 0) return (0, 0);

        var mean = pitches.Average();
        var variance = pitches.Average(p => (p - mean) * (p - mean));
        return (Math.Round(mean, 1), Math.Round(Math.Sqrt(variance), 1));
    }

    private static double EstimateFramePitch(short[] samples, int start, int frameSize)
    {
        var minLag = SampleRate / 500;
        var maxLag = SampleRate / 50;
        var bestLag = minLag;
        var bestCorr = double.MinValue;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            if (start + lag + frameSize > samples.Length) break;
            double corr = 0, sumSq = 0;
            for (int i = 0; i < frameSize; i++)
            {
                corr += samples[start + i] * samples[start + i + lag];
                sumSq += samples[start + i] * samples[start + i];
            }
            if (sumSq > 0)
            {
                corr /= Math.Sqrt(sumSq);
                if (corr > bestCorr)
                {
                    bestCorr = corr;
                    bestLag = lag;
                }
            }
        }

        return bestCorr > 0.3 ? (double)SampleRate / bestLag : 0;
    }

    private static double EstimateSpeakingRate(short[] samples)
    {
        var frameEnergy = new List<double>();
        var frameSize = (int)(SampleRate * 0.05);
        for (int i = 0; i < samples.Length; i += frameSize)
        {
            var end = Math.Min(i + frameSize, samples.Length);
            double energy = 0;
            for (int j = i; j < end; j++)
                energy += Math.Abs(samples[j] / 32768.0);
            frameEnergy.Add(energy / (end - i));
        }

        var threshold = frameEnergy.Average() * 0.5;
        var transitions = 0;
        bool wasAbove = frameEnergy.Count > 0 && frameEnergy[0] > threshold;
        for (int i = 1; i < frameEnergy.Count; i++)
        {
            bool isAbove = frameEnergy[i] > threshold;
            if (isAbove != wasAbove) transitions++;
            wasAbove = isAbove;
        }

        var durationSec = samples.Length / (double)SampleRate;
        return durationSec > 0 ? transitions / durationSec : 0;
    }

    private static double ClassifyVoiceQuality(double energy, double pitchStd)
    {
        if (pitchStd > 80 && energy > 0.2) return 1.0;
        if (pitchStd > 50 && energy > 0.15) return 0.7;
        if (pitchStd < 20 && energy < 0.1) return 0.2;
        return 0.5;
    }
}
