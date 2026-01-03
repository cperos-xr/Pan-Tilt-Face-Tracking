using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

public static class OpenCVQRCodeUtil
{
    /// <summary>
    /// Generates a QR code Texture2D from the given string using OpenCVForUnity.
    /// </summary>
    /// <param name="text">The string to encode as a QR code.</param>
    /// <param name="size">The width/height of the output QR code image in pixels.</param>
    /// <returns>A Texture2D containing the QR code, or null if generation fails.</returns>
    public static Texture2D GenerateQRCode(string text, int size = 512)
    {
        using (var encoder = QRCodeEncoder.create())
        using (var gray = new Mat())
        {
            encoder.encode(text, gray);
            if (gray.empty())
                return null;

            using (var rgb = new Mat(gray.size(), CvType.CV_8UC3))
            using (var qrMat = new Mat(size, size, CvType.CV_8UC3))
            {
                Imgproc.cvtColor(gray, rgb, Imgproc.COLOR_GRAY2RGB);
                Imgproc.resize(rgb, qrMat, qrMat.size(), 0, 0, Imgproc.INTER_NEAREST);
                var tex = new Texture2D(qrMat.cols(), qrMat.rows(), TextureFormat.RGB24, false);
                Utils.matToTexture2D(qrMat, tex);
                return tex;
            }
        }
    }
}
