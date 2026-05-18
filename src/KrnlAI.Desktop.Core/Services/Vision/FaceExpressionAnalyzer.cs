using KrnlAI.Desktop.Core.Models;
using OpenCvSharp;

namespace KrnlAI.Desktop.Core.Services.Vision;

public sealed record FaceAnalysisResult(
    UserEmotion Emotion,
    double Confidence,
    int FaceArea);

public sealed class FaceExpressionAnalyzer
{
    private readonly CascadeClassifier _faceCascade;
    private readonly CascadeClassifier _eyeCascade;
    private readonly CascadeClassifier _smileCascade;

    public FaceExpressionAnalyzer()
    {
        var haarPath = Environment.GetEnvironmentVariable("OPENCV_HAAR_PATH")
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascades");

        _faceCascade = LoadCascade(haarPath, "haarcascade_frontalface_default.xml");
        _eyeCascade = LoadCascade(haarPath, "haarcascade_eye.xml");
        _smileCascade = LoadCascade(haarPath, "haarcascade_smile.xml");
    }

    public FaceAnalysisResult? AnalyzeFrame(byte[] frameData, int width, int height)
    {
        try
        {
            using var mat = Mat.FromPixelData(height, width, MatType.CV_8UC3, frameData);
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gray, gray);

            var faces = _faceCascade.DetectMultiScale(gray, 1.1, 3);
            if (faces.Length == 0) return null;

            var face = faces[0];
            using var faceRoi = new Mat(gray, face);
            Cv2.Resize(faceRoi, faceRoi, new Size(100, 100));

            var hasEyes = _eyeCascade.DetectMultiScale(faceRoi, 1.1, 2).Length > 0;
            var hasSmile = _smileCascade.DetectMultiScale(faceRoi, 1.1, 3).Length > 0;

            var emotion = ClassifyExpression(hasEyes, hasSmile, faceRoi);
            var confidence = hasEyes ? 0.6 : 0.3;

            return new FaceAnalysisResult(emotion, confidence, face.Width * face.Height);
        }
        catch
        {
            return null;
        }
    }

    private static UserEmotion ClassifyExpression(bool hasEyes, bool hasSmile, Mat faceRoi)
    {
        if (!hasEyes) return UserEmotion.Neutral;
        if (hasSmile) return UserEmotion.Happy;

        var meanIntensity = Cv2.Mean(faceRoi).Val0;
        var std = ComputeStd(faceRoi, meanIntensity);

        if (std > 50) return UserEmotion.Surprised;
        if (meanIntensity < 80) return UserEmotion.Sad;
        if (std > 35) return UserEmotion.Angry;

        return UserEmotion.Neutral;
    }

    private static double ComputeStd(Mat grayMat, double mean)
    {
        Cv2.MeanStdDev(grayMat, out _, out var stdDev);
        return stdDev.Val0;
    }

    private static CascadeClassifier LoadCascade(string haarPath, string fileName)
    {
        var path = Path.Combine(haarPath, fileName);
        if (File.Exists(path))
            return new CascadeClassifier(path);

        try
        {
            return new CascadeClassifier(fileName);
        }
        catch
        {
            return new CascadeClassifier();
        }
    }
}
