namespace KrnlAI.Desktop.Core.Services;

public static class EmotionalNormalizer
{
    public static string NormalizeMood(double valence, double arousal)
    {
        if (valence > 0.3)
            return arousal < 0.4 ? "😌 Tranquilo" : "⚡ Animado";
        if (valence < -0.3)
            return arousal < 0.4 ? "😮‍💨 Cansado" : "😰 Tenso";
        return arousal >= 0.4 ? "🧐 Atento" : "😐 Neutro";
    }
}
