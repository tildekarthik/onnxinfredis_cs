using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using dotenv.net;
using Serilog;
namespace yoloinfredis_cs;

public static class RedisHelper
{
    private static ConnectionMultiplexer? _redis;
    private static IDatabase? _db;
    private static string? _host;
    private static string? _password;
    private static bool _envLoaded = false;

    private static void LoadEnv()
    {
        if (_envLoaded) return;
        DotEnv.Load();
        _host = Environment.GetEnvironmentVariable("REDIS_SERVER");
        _password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
        _envLoaded = true;
    }

    public static IDatabase GetDatabase()
    {
        LoadEnv();
        if (string.IsNullOrEmpty(_host))
            throw new InvalidOperationException("REDIS_SERVER not found in .env");
        if (_redis == null)
        {
            // Console.WriteLine($"Connecting to Redis at {_host}");
            // Console.WriteLine($"Using password: {_password}");
            var options = ConfigurationOptions.Parse(_host);
            options.Password = _password;
            options.User = "default";
            options.DefaultDatabase = 0;
            _redis = ConnectionMultiplexer.Connect(options);
        }
        _db = _redis.GetDatabase();
        return _db;
    }

    public class InferenceInputModel
    {
        public string image_base64 { get; set; } = string.Empty;
        public string model_name { get; set; } = string.Empty;
        public int imgsz { get; set; }
        public float conf { get; set; }
        public float iou { get; set; }
        public string inferaction { get; set; } = string.Empty;
    }

    public class RedisQueueModel
    {
        public string message_id { get; set; } = string.Empty;
        public InferenceInputModel data { get; set; } = new InferenceInputModel();
    }

    public static RedisQueueModel? BrPopAndParse(string queueName, int timeoutSeconds = 0)
    {
        var db = GetDatabase();
        try
        {
            var result = db.Execute("BRPOP", queueName, timeoutSeconds);
            if (result.IsNull)
            {
                // Console.WriteLine("BRPOP returned null, no data in queue.");
                // Log.Information("BRPOP returned null, no data in queue for {QueueName}", queueName);
                return null;
            }
            var json = ((RedisResult)result)[1].ToString();
            // Console.WriteLine($"BRPOP result: {json}");
            return System.Text.Json.JsonSerializer.Deserialize<RedisQueueModel>(json);
        }
        catch (RedisTimeoutException)
        {
            // Console.WriteLine("Timeout occurred while waiting for BRPOP");
            // Log.Error("Timeout occurred while waiting for BRPOP on queue {QueueName}", queueName);
            return null;
        }
    }

    public static void SetResultSummary(string messageId, string json)
    {
        var db = GetDatabase();
        db.StringSet(messageId, json);
    }

}
