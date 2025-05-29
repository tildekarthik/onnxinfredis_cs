using Amazon.S3;
using Amazon.S3.Model;
using dotenv.net;
namespace yoloinfredis_cs;

public static class S3Helper
{
    public static IAmazonS3 GetClient(string awsKey, string awsSecret)
    {
        return new AmazonS3Client(awsKey, awsSecret, Amazon.RegionEndpoint.USEast1); // Change region if needed
    }

    public static IAmazonS3 GetClientFromEnv()
    {
        DotEnv.Load();
        var awsKey = Environment.GetEnvironmentVariable("AWS_KEY");
        var awsSecret = Environment.GetEnvironmentVariable("AWS_SECRET");
        if (string.IsNullOrEmpty(awsKey) || string.IsNullOrEmpty(awsSecret))
            throw new InvalidOperationException("AWS_KEY or AWS_SECRET not found in environment variables or .env file");
        return new AmazonS3Client(awsKey, awsSecret, Amazon.RegionEndpoint.USEast1); // Change region if needed
    }

    public static void DownloadFile(IAmazonS3 client, string bucketName, string objectKey, string fileName)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };
        using var response = client.GetObjectAsync(request).GetAwaiter().GetResult();
        response.WriteResponseStreamToFileAsync(fileName, false, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static void DownloadOnnxModel(IAmazonS3 client, string modelId)
    {
        var modelFolder = Path.Combine("inference_models", modelId);
        Directory.CreateDirectory(modelFolder);
        Console.WriteLine($"Download key models/{modelId}/best.onnx");
        DownloadFile(client, "aicuemodels", $"models/{modelId}/best.onnx", Path.Combine(modelFolder, "best.onnx"));
        DownloadFile(client, "aicuemodels", $"models/{modelId}/class_names.txt", Path.Combine(modelFolder, "class_names.txt"));
    }
}
