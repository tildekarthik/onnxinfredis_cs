// See https://aka.ms/new-console-template for more information
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using yoloinfredis_cs;
using Serilog;


public partial class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: "logs/app.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 2 // <- keep only the latest 2 log files
            )
            .CreateLogger();
        Log.Information("Starting Inference Service...");
        Console.WriteLine("Starting Inference Service...");
        var modelManager = new yoloinfredis_cs.ModelManager();
        while (true)
        {
            try
            {
                ProcessRequest(modelManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
                Log.Error(ex, "Error processing request");
            }
        }

    }

    public static void ProcessRequest(ModelManager modelManager)
    {
        var (messageId, originalImage, modelId, confidenceThreshold, inferaction, targetImageSize) = ParseRequest();
        if (originalImage == null || messageId == null || modelId == null || inferaction == null) return;

        using var image = yoloinfredis_cs.ImageProcessor.ResizePad(originalImage, targetImageSize, targetImageSize, out int scaledWidth, out int scaledHeight);
        var detectionResults = RunDetection(modelManager, modelId, image, confidenceThreshold, scaledWidth, scaledHeight);

        if (inferaction.Contains("classify", StringComparison.CurrentCultureIgnoreCase))
        {
            var classificationResults = RunClassification(modelManager, modelId, originalImage, confidenceThreshold);
            detectionResults.detections.AddRange(classificationResults.detections);
        }

        SaveResults(messageId, detectionResults);
        originalImage.Dispose();
        image.Dispose();
    }

    private static (string? messageId, Image<SixLabors.ImageSharp.PixelFormats.Rgb24>? originalImage, string? modelId, float confidenceThreshold, string? inferaction, int targetImageSize) ParseRequest()
    {
        var rqData = yoloinfredis_cs.RedisHelper.BrPopAndParse("yolo_inference_requests", 2);
        if (rqData == null)
        {
            return (null, null, null, 0, null, 0);
        }
        var messageId = rqData.message_id;
        var modelId = rqData.data.model_name;
        var confidenceThreshold = rqData.data.conf;
        var inferaction = rqData.data.inferaction;
        var targetImageSize = rqData.data.imgsz;
        var imageBase64 = rqData.data.image_base64;
        var originalImage = yoloinfredis_cs.ImageProcessor.LoadImageFromBase64(imageBase64);
        return (messageId, originalImage, modelId, confidenceThreshold, inferaction, targetImageSize);
    }

    private static yoloinfredis_cs.ResultSummary RunDetection(ModelManager modelManager, string modelId, Image<SixLabors.ImageSharp.PixelFormats.Rgb24> image, float confidenceThreshold, int scaledWidth, int scaledHeight)
    {
        var tensor = yoloinfredis_cs.TensorHelper.ImageToTensor(image);
        var (detector, classNames) = modelManager.GetModel(modelId);
        var (boxes, scores, classIndices) = detector.RunInference(tensor, confidenceThreshold);
        return yoloinfredis_cs.DetectionSerializer.ToResultSummary(boxes, scores, classIndices, classNames, scaledWidth, scaledHeight);
    }

    private static yoloinfredis_cs.ResultSummary RunClassification(ModelManager modelManager, string modelId, Image<SixLabors.ImageSharp.PixelFormats.Rgb24> originalImage, float confidenceThreshold)
    {
        var targetClassifyImageSize = 160;
        var modelIdCls = modelId + "_CLS";
        using var resizedImage = yoloinfredis_cs.ImageProcessor.SquareResize(originalImage, targetClassifyImageSize);
        var tensorCls = yoloinfredis_cs.TensorHelper.ImageToTensorClassification(resizedImage);
        var (detectorCls, classNamesCls) = modelManager.GetModel(modelIdCls);
        var (boxesCls, scoresCls, classIndicesCls) = detectorCls.RunInferenceClassify(tensorCls, confidenceThreshold);
        return yoloinfredis_cs.DetectionSerializer.ToResultSummaryClassify(boxesCls, scoresCls, classIndicesCls, classNamesCls, originalImage.Width, originalImage.Height);
    }

    private static void SaveResults(string messageId, yoloinfredis_cs.ResultSummary results)
    {
        var json = yoloinfredis_cs.DetectionSerializer.ToResultSummaryJson(results);
        yoloinfredis_cs.RedisHelper.SetResultSummary(messageId, json);
        Log.Information($"Results saved to Redis with message ID: {messageId}");
        Log.Information($"Detection Results JSON: {json}");
    }

}