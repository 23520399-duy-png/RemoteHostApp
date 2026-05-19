using System.Drawing;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace RemoteHostApp.Helpers;

/// <summary>
/// Helper xử lý ảnh: resize, nén JPEG, encode Base64
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Resize bitmap và nén thành JPEG Base64
    /// </summary>
    public static (string Base64, int Width, int Height)
    ResizeAndCompress(Bitmap source, int maxWidth = 1280, int quality = 75)
    {
        int srcW = source.Width;
        int srcH = source.Height;

        int dstW = srcW;
        int dstH = srcH;

        if (srcW > maxWidth)
        {
            dstW = maxWidth;
            dstH = (int)(srcH * ((double)maxWidth / srcW));
        }

        using var resized = new Bitmap(dstW, dstH, PixelFormat.Format24bppRgb);

        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

            g.DrawImage(source, 0, 0, dstW, dstH);
        }

        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] =
            new EncoderParameter(Encoder.Quality, (long)quality);

        var jpegCodec = GetJpegCodec();

        using var ms = new MemoryStream();

        resized.Save(ms, jpegCodec!, encoderParams);

        return (
            Convert.ToBase64String(ms.ToArray()),
            dstW,
            dstH
        );
    }

    private static ImageCodecInfo? GetJpegCodec()
    {
        return ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
    }
}