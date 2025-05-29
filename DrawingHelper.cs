using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;

namespace yoloinfredis_cs;

public static class DrawingHelper
{
    public static void DrawResults(Image<Rgb24> image, List<float[]> boxes, List<float> scores, List<int> classIndices, List<string> classNames)
    {
        var font = SystemFonts.CreateFont("Arial", 24);
        image.Mutate(x =>
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i];
                var className = classNames[classIndices[i]];
                var score = scores[i];
                var x1 = (int)box[0];
                var y1 = (int)box[1];
                var x2 = (int)box[2];
                var y2 = (int)box[3];
                var w = (int)(x2 - x1);
                var h = (int)(y2 - y1);
                var rect = new Rectangle(x1, y1, w, h);
                x.Draw(Color.Green, 2f, rect);
            }
            for (int i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i];
                var x1 = (int)box[0];
                var y1 = (int)box[1];
                var className = classNames[classIndices[i]];
                var score = scores[i];
                var text = $"{className}: {score:F2}";
                var fontOptions = new RichTextOptions(font)
                {
                    Origin = new PointF(x1, y1 - 20),
                };
                x.DrawText(fontOptions, text, Color.White);
            }
        });
    }

    public static void DrawResults(Image<Rgb24> image, yoloinfredis_cs.ResultSummary summary)
    {
        var font = SystemFonts.CreateFont("Arial", 24);
        image.Mutate(x =>
        {
            for (int i = 0; i < summary.detections.Count; i++)
            {
                var det = summary.detections[i];
                var box = det.xyxyn;
                // Convert normalized coordinates to pixel coordinates
                int x1 = (int)(box[0] * image.Width);
                int y1 = (int)(box[1] * image.Height);
                int x2 = (int)(box[2] * image.Width);
                int y2 = (int)(box[3] * image.Height);
                int w = x2 - x1;
                int h = y2 - y1;
                var rect = new Rectangle(x1, y1, w, h);
                x.Draw(Color.Green, 2f, rect);
            }
            for (int i = 0; i < summary.detections.Count; i++)
            {
                var det = summary.detections[i];
                var box = det.xyxyn;
                int x1 = (int)(box[0] * image.Width);
                int y1 = (int)(box[1] * image.Height);
                var text = $"{det.class_name}: {det.conf:F2}";
                var fontOptions = new RichTextOptions(font)
                {
                    Origin = new PointF(x1, y1 - 20),
                };
                x.DrawText(fontOptions, text, Color.White);
            }
        });
    }
}
