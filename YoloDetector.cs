using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace yoloinfredis_cs;

public class YoloDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly List<string> _classNames;

    public YoloDetector(string modelPath, string classNamesPath)
    {
        var sess_options = new SessionOptions();
        sess_options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC;
        sess_options.InterOpNumThreads = 1;
        sess_options.IntraOpNumThreads = 1;
        sess_options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
        _session = new InferenceSession(modelPath, sess_options);
        _classNames = File.ReadAllLines(classNamesPath).ToList();
    }

    private (List<float[]>, List<float>, List<int>) ParseDetections(Tensor<float> output, float confidence)
    {
        var boxes = new List<float[]>();
        var scores = new List<float>();
        var classIndices = new List<int>();
        for (int i = 0; i < output.Dimensions[1]; i++)
        {
            if (output[0, i, 4] < confidence) continue;
            scores.Add(output[0, i, 4]);
            classIndices.Add((int)output[0, i, 5]);
            var box = new float[4];
            for (int j = 0; j < 4; j++)
            {
                box[j] = output[0, i, j];
            }
            boxes.Add(box);
        }
        return (boxes, scores, classIndices);
    }

    private (List<float[]>, List<float>, List<int>) ParseDetectionsObb(Tensor<float> output, float confidence)
    {
        var boxes = new List<float[]>();
        var scores = new List<float>();
        var classIndices = new List<int>();
        int num = output.Dimensions[1];
        for (int i = 0; i < num; i++)
        {
            float score = output[0, i, 4];
            if (score < confidence) continue;
            // OBB: [x_center, y_center, w, h, score, class, rotation]
            float x = output[0, i, 0];
            float y = output[0, i, 1];
            float w = output[0, i, 2];
            float h = output[0, i, 3];
            float angleRad = output[0, i, 6];
            float angleDeg = angleRad * (180f / (float)Math.PI);
            var corners = GetRotatedBoxCorners(x, y, w, h, angleDeg);
            float minX = corners.Min(pt => pt[0]);
            float minY = corners.Min(pt => pt[1]);
            float maxX = corners.Max(pt => pt[0]);
            float maxY = corners.Max(pt => pt[1]);
            boxes.Add(new float[] { minX, minY, maxX, maxY });
            scores.Add(score);
            classIndices.Add((int)output[0, i, 5]);
        }
        return (boxes, scores, classIndices);
    }

    private static List<float[]> GetRotatedBoxCorners(float x, float y, float w, float h, float angleDeg)
    {
        // Center (x, y), width w, height h, angle in degrees
        double angle = angleDeg * Math.PI / 180.0;
        double cosA = Math.Cos(angle);
        double sinA = Math.Sin(angle);
        float halfW = w / 2f;
        float halfH = h / 2f;
        var corners = new List<float[]>();
        float[][] pts = new float[][] {
        new float[] { -halfW, -halfH },
        new float[] { halfW, -halfH },
        new float[] { halfW, halfH },
        new float[] { -halfW, halfH }
    };
        foreach (var pt in pts)
        {
            float xRot = (float)(pt[0] * cosA - pt[1] * sinA) + x;
            float yRot = (float)(pt[0] * sinA + pt[1] * cosA) + y;
            corners.Add(new float[] { xRot, yRot });
        }
        return corners;
    }

    private (List<float[]>, List<float>, List<int>) ParseDetectionsClassify(float tensorOutput, float confidence)
    {
        // boxes should have one value [0.05,0.05,0.95,0.95]
        var box = new float[] { 0.05f, 0.05f, 0.95f, 0.95f };
        var boxes = new List<float[]> { box };
        // scores should have one value [1.0]
        var score = 0.99f;
        var scores = new List<float> { score };

        var sigmoidOutput = 1 / (1 + MathF.Exp(-tensorOutput));
        var classindex = sigmoidOutput >= confidence ? 1 : 0; // Assuming binary classification
        var classIndices = new List<int> { classindex };
        return (boxes, scores, classIndices);
    }
    public (List<float[]>, List<float>, List<int>) RunInference(DenseTensor<float> tensor, float confidence = 0.25f)
    {
        var inputName = _session.InputMetadata.Keys.First();
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, tensor)
        };
        using var results = _session.Run(inputs);
        var output = results.First().AsTensor<float>();
        // Console.WriteLine($"Output shape: {string.Join(", ", output.Dimensions.ToArray())}");
        if (output.Dimensions.ToArray().Last() == 6)
        {
            // Console.WriteLine("Detected Axis aligned box output format");
            return ParseDetections(output, confidence);
        }
        else if (output.Dimensions.ToArray().Last() == 7)
        {
            // Console.WriteLine("Detected OBB output format");
            return ParseDetectionsObb(output, confidence);
        }
        else
        {
            throw new InvalidOperationException("Unsupported output shape: " + string.Join(", ", output.Dimensions.ToArray()));
        }
    }

    // public (List<float[]>, List<float>, List<int>) RunInferenceClassify(DenseTensor<float> tensor, float confidence = 0.25f)
    public (List<float[]>, List<float>, List<int>) RunInferenceClassify(DenseTensor<float> tensor, float confidence = 0.25f)
    {
        var inputName = _session.InputMetadata.Keys.First();
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, tensor)
        };
        using var results = _session.Run(inputs);
        var output = results.First().AsTensor<float>();
        float tensorOutput = output[0, 0];

        return ParseDetectionsClassify(tensorOutput, confidence);
    }

    public string GetClassName(int index) => _classNames[index];
    public List<string> GetClassNames() => _classNames;
    public void Dispose() => _session.Dispose();
}
