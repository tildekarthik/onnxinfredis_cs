using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace yoloinfredis_cs;

public static class ImageProcessor
{
    public static Image<Rgb24> LoadImage(string imagePath)
    {
        return Image.Load<Rgb24>(imagePath);
    }


    public static Image<Rgb24> LoadImageFromBase64(string base64String)
    {
        byte[] imageBytes = Convert.FromBase64String(base64String);
        using var ms = new MemoryStream(imageBytes);
        return Image.Load<Rgb24>(ms);
    }

    public static Image<Rgb24> ResizePad(Image<Rgb24> image, int targetWidth, int targetHeight, out int scaledWidth, out int scaledHeight)
    {
        float scaleX = (float)targetWidth / image.Width;
        float scaleY = (float)targetHeight / image.Height;
        float scale = Math.Min(scaleX, scaleY);
        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);
        using var i = image.Clone();
        i.Mutate(x => x.Resize(newWidth, newHeight));
        var targetImage = new Image<Rgb24>(targetWidth, targetHeight, Color.Gray);
        targetImage.Mutate(x => x.DrawImage(image, new Point(0, 0), 1f));
        // image.Dispose();
        scaledWidth = newWidth;
        scaledHeight = newHeight;
        i.Dispose();
        return targetImage;
    }

    public static Image<Rgb24> SquareResize(Image<Rgb24> image, int targetSize)
    {
        var clone = image.Clone();
        clone.Mutate(x => x.Resize(targetSize, targetSize));
        return clone;
    }

}
