using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace yoloinfredis_cs;

public static class TensorHelper
{
    public static DenseTensor<float> ImageToTensor(Image<Rgb24> image)
    {
        int width = image.Width;
        int height = image.Height;
        var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = pixelRow[x];
                    tensor[0, 0, y, x] = pixel.R / 255f;
                    tensor[0, 1, y, x] = pixel.G / 255f;
                    tensor[0, 2, y, x] = pixel.B / 255f;
                }
            }
        });
        return tensor;
    }

    public static DenseTensor<float> ImageToTensorClassification(Image<Rgb24> resizedImage)
    {
        // Only do a np expand_dims equivalent for mobilenet classification as the 
        // normalization is done in the model
        int width = resizedImage.Width;
        int height = resizedImage.Height;
        var tensor = new DenseTensor<float>(new[] { 1, height, width, 3 });
        resizedImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = pixelRow[x];
                    tensor[0, y, x, 0] = (float)pixel.R;
                    tensor[0, y, x, 1] = (float)pixel.G;
                    tensor[0, y, x, 2] = (float)pixel.B;
                }
            }
        });
        return tensor;
    }

}
