// See https://aka.ms/new-console-template for more information
using Amazon.S3.Model;
using SixLabors.ImageSharp;
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
        var rqData = yoloinfredis_cs.RedisHelper.BrPopAndParse("yolo_inference_requests", 2);
        if (rqData == null)
        {
            // Console.WriteLine("No request data found in the queue.");
            return;
        }
        var messageId = rqData.message_id;
        var modelId = rqData.data.model_name;
        var confidenceThreshold = rqData.data.conf;
        var inferaction = rqData.data.inferaction;
        var targetImageSize = rqData.data.imgsz;

        var imageBase64 = rqData.data.image_base64;
        using var originalImage = yoloinfredis_cs.ImageProcessor.LoadImageFromBase64(imageBase64);

        // CODE BLOCK FOR DETECTION THAT WORKS
        using var image = yoloinfredis_cs.ImageProcessor.ResizePad(originalImage, targetImageSize, targetImageSize, out int scaledWidth, out int scaledHeight);
        var tensor = yoloinfredis_cs.TensorHelper.ImageToTensor(image);
        var (detector, classNames) = modelManager.GetModel(modelId);
        var (boxes, scores, classIndices) = detector.RunInference(tensor, confidenceThreshold);
        var results = yoloinfredis_cs.DetectionSerializer.ToResultSummary(boxes, scores, classIndices, classNames, scaledWidth, scaledHeight);
        // END CODE BLOCK FOR DETECTION 

        if (inferaction.Contains("classify", StringComparison.CurrentCultureIgnoreCase))
        {
            // CODE BLOCK FOR CLASSIFICATION if "classify" in inferaction
            var targetClassifyImageSize = 160;
            var modelIdCls = modelId + "_CLS"; // Assuming classification model ID is suffixed with _CLS
            using var resizedImage = yoloinfredis_cs.ImageProcessor.SquareResize(originalImage, targetClassifyImageSize);
            var tensorCls = yoloinfredis_cs.TensorHelper.ImageToTensorClassification(resizedImage);
            var (detectorCls, classNamesCls) = modelManager.GetModel(modelIdCls);
            var (boxesCls, scoresCls, classIndicesCls) = detectorCls.RunInferenceClassify(tensorCls, confidenceThreshold);
            var resultsCls = yoloinfredis_cs.DetectionSerializer.ToResultSummaryClassify(boxesCls, scoresCls, classIndicesCls, classNamesCls, originalImage.Width, originalImage.Height);
            resizedImage.Dispose();
            // END CODE BLOCK FOR CLASSIFICATION
            results.detections.AddRange(resultsCls.detections);
        }
        var json = yoloinfredis_cs.DetectionSerializer.ToResultSummaryJson(results);
        // Console.WriteLine("Detection Results JSON:");
        // Console.WriteLine(json);
        yoloinfredis_cs.RedisHelper.SetResultSummary(messageId, json);
        Log.Information($"Results saved to Redis with message ID: {messageId}");
        Log.Information($"Detection Results JSON: {json}");
        originalImage.Dispose();
        image.Dispose();
    }

}