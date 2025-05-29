using System.Collections.Concurrent;
using System.Timers;
using Serilog;

namespace yoloinfredis_cs;

public class ModelManager : IDisposable
{
    private readonly ConcurrentDictionary<string, (YoloDetector detector, DateTime lastUsed)> _models = new();
    private readonly TimeSpan _maxIdleTime = TimeSpan.FromHours(1);
    private readonly System.Timers.Timer _cleanupTimer;

    public ModelManager()
    {
        _cleanupTimer = new System.Timers.Timer(10 * 60 * 1000); // every 10 minutes
        _cleanupTimer.Elapsed += (s, e) => CleanupIdleModels();
        _cleanupTimer.Start();
    }

    public (YoloDetector detector, List<string> classNames) GetModel(string modelId)
    {
        var now = DateTime.UtcNow;
        if (!_models.TryGetValue(modelId, out var entry))
        {
            var modelFilePath = Path.Combine("inference_models", modelId, "best.onnx");
            var classNameFilePath = Path.Combine("inference_models", modelId, "class_names.txt");
            if (!File.Exists(modelFilePath) || !File.Exists(classNameFilePath))
            {
                S3Helper.DownloadOnnxModel(S3Helper.GetClientFromEnv(), modelId);
            }
            var detector = new YoloDetector(modelFilePath, classNameFilePath);
            var classNames = detector.GetClassNames();
            _models[modelId] = (detector, now);
            return (detector, classNames);
        }
        else
        {
            _models[modelId] = (entry.detector, now); // update last used
            return (entry.detector, entry.detector.GetClassNames());
        }
    }

    public void CleanupIdleModels()
    {
        var now = DateTime.UtcNow;
        Log.Information("Cleaning up idle models...");
        foreach (var kvp in _models.ToArray())
        {
            if ((now - kvp.Value.lastUsed) > _maxIdleTime)
            {
                Log.Information($"Disposing idle model: {kvp.Key}");
                kvp.Value.detector.Dispose();
                _models.TryRemove(kvp.Key, out _);
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Stop();
        foreach (var kvp in _models)
        {
            kvp.Value.detector.Dispose();
        }
        _models.Clear();
    }
}
