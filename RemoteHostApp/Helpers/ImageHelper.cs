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
    public static string ResizeAndCompress(Bitmap source, int maxWidth = 1280, int quality = 75)
    {
        // Tính kích thước đích giữ tỉ lệ
        int srcW = source.Width, srcH = source.Height;
        int dstW = srcW, dstH = srcH;

        if (srcW > maxWidth)
        {
            dstW = maxWidth;
            dstH = (int)(srcH * ((double)maxWidth / srcW));
        }

        using var resized = new Bitmap(dstW, dstH, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            g.DrawImage(source, 0, 0, dstW, dstH);
        }

        // Nén JPEG với quality tuỳ chỉnh
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

        var jpegCodec = GetJpegCodec();
        if (jpegCodec == null)
        {
            throw new Exception("JPEG codec not found.");
        }

        using var ms = new MemoryStream();
        resized.Save(ms, jpegCodec, encoderParams);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static ImageCodecInfo? GetJpegCodec()
    {
        return ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
    }
}