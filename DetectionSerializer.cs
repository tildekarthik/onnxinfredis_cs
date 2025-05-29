using System.Text.Json;

namespace yoloinfredis_cs;

public class Detection
{
    public string class_name { get; set; } = string.Empty;
    public float[] xyxyn { get; set; } = Array.Empty<float>();
    public float conf { get; set; }
}

public class ResultSummary
{
    public List<Detection> detections { get; set; } = new();
    public int[] image_shape { get; set; } = Array.Empty<int>();
}

public static class DetectionSerializer
{
    public static ResultSummary ToResultSummary(
        List<float[]> boxes,
        List<float> scores,
        List<int> classIndices,
        List<string> classNames,
        int imgWidth,
        int imgHeight)
    {
        var detections = new List<Detection>();
        for (int i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            var x1n = box[0] / imgWidth;
            var y1n = box[1] / imgHeight;
            var x2n = box[2] / imgWidth;
            var y2n = box[3] / imgHeight;
            detections.Add(new Detection
            {
                class_name = classNames[classIndices[i]],
                xyxyn = [x1n, y1n, x2n, y2n],
                conf = scores[i]
            });
        }
        return new ResultSummary
        {
            detections = detections,
            image_shape = [imgHeight, imgWidth]
        };
    }

    public static ResultSummary ToResultSummaryClassify(
        List<float[]> boxes,
        List<float> scores,
        List<int> classIndices,
        List<string> classNames,
        int imgWidth,
        int imgHeight)
    {
        var detections = new List<Detection>();
        for (int i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            var x1n = box[0];
            var y1n = box[1];
            var x2n = box[2];
            var y2n = box[3];
            detections.Add(new Detection
            {
                class_name = classNames[classIndices[i]],
                xyxyn = [x1n, y1n, x2n, y2n],
                conf = scores[i]
            });
        }
        return new ResultSummary
        {
            detections = detections,
            image_shape = [imgHeight, imgWidth]
        };
    }
    public static string ToResultSummaryJson(ResultSummary summary)
    {
        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = false });
    }
}
