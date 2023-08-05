using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZPLPrinter
{
    public enum CompressionType
    {
        LZ77,
        LZ64
    }

    public class ZplImageHelper
    {

        public static string EncodeGFACommand(string filePath, CompressionType compressionType = CompressionType.LZ64)
        {
            var zplImageInfo = LoadBitmapData(filePath);
            zplImageInfo.Inverse();
            string textBitmap = string.Empty;
                       
            if (compressionType == CompressionType.LZ77)
            {
                textBitmap = ZplHexCompression.CompressLZ77(zplImageInfo.Data);
            }
            else if (compressionType == CompressionType.LZ64)
            {
                textBitmap = ZplZ64Compression.EncodeZ64Command(zplImageInfo.Data);
            }

            return string.Format("^GFA,{0},{0},{1},{2}",
                zplImageInfo.Height * zplImageInfo.RowRealBytesCount,
                zplImageInfo.RowRealBytesCount,
                textBitmap);
        }


        public static ZplImageInfo LoadBitmapData(string filePath)
        {
            ZplImageInfo zplImageInfo = new ZplImageInfo();

            MemoryStream srcStream = new MemoryStream();
            MemoryStream dstStream = new MemoryStream();
            Bitmap srcBmp = null;
            Bitmap dstBmp = null;
            byte[] srcBuffer = null;
            byte[] dstBuffer = null;

            try
            {
                var imageBuffer = File.ReadAllBytes(filePath);

                srcStream = new MemoryStream(imageBuffer);
                srcBmp = Bitmap.FromStream(srcStream) as Bitmap;
                srcBuffer = srcStream.ToArray();

                zplImageInfo = new ZplImageInfo(srcBmp.Width, srcBmp.Height);

                // 转成灰度图像
                dstBmp = ConvertToGrayscale(srcBmp);
                dstBmp.Save(dstStream, ImageFormat.Bmp);
                dstBuffer = dstStream.ToArray();
                int bfOffBits = BitConverter.ToInt32(dstBuffer, 10);

                //读取时需要反向读取每行字节实现上下翻转的效果，打印机打印顺序需要这样读取。  
                for (int i = 0; i < zplImageInfo.Height; i++)
                {
                    Array.Copy(dstBuffer, bfOffBits + (zplImageInfo.Height - 1 - i) * zplImageInfo.RowSize, zplImageInfo.Data, i * zplImageInfo.RowRealBytesCount, zplImageInfo.RowRealBytesCount);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                if (srcStream != null)
                {
                    srcStream.Dispose();
                    srcStream = null;
                }
                if (dstStream != null)
                {
                    dstStream.Dispose();
                    dstStream = null;
                }
                if (srcBmp != null)
                {
                    srcBmp.Dispose();
                    srcBmp = null;
                }
                if (dstBmp != null)
                {
                    dstBmp.Dispose();
                    dstBmp = null;
                }
            }
            return zplImageInfo;
        }

        /// <summary>  
        ///   
        /// </summary>  
        /// <param name="pimage"></param>  
        /// <returns></returns>  
        public static Bitmap ConvertToGrayscale(Bitmap pimage)
        {
            Bitmap source = null;

            // If original bitmap is not already in 32 BPP, ARGB format, then convert  
            if (pimage.PixelFormat != PixelFormat.Format32bppArgb)
            {
                source = new Bitmap(pimage.Width, pimage.Height, PixelFormat.Format32bppArgb);
                source.SetResolution(pimage.HorizontalResolution, pimage.VerticalResolution);
                using (Graphics g = Graphics.FromImage(source))
                {
                    g.DrawImageUnscaled(pimage, 0, 0);
                }
            }
            else
            {
                source = pimage;
            }

            // Lock source bitmap in memory  
            BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            // Copy image data to binary array  
            int imageSize = sourceData.Stride * sourceData.Height;
            byte[] sourceBuffer = new byte[imageSize];
            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, imageSize);

            // Unlock source bitmap  
            source.UnlockBits(sourceData);

            // Create destination bitmap  
            Bitmap destination = new Bitmap(source.Width, source.Height, PixelFormat.Format1bppIndexed);

            // Lock destination bitmap in memory  
            BitmapData destinationData = destination.LockBits(new Rectangle(0, 0, destination.Width, destination.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

            // Create destination buffer  
            imageSize = destinationData.Stride * destinationData.Height;
            byte[] destinationBuffer = new byte[imageSize];

            int sourceIndex = 0;
            int destinationIndex = 0;
            int pixelTotal = 0;
            byte destinationValue = 0;
            int pixelValue = 128;
            int height = source.Height;
            int width = source.Width;
            int threshold = 500;

            // Iterate lines  
            for (int y = 0; y < height; y++)
            {
                sourceIndex = y * sourceData.Stride;
                destinationIndex = y * destinationData.Stride;
                destinationValue = 0;
                pixelValue = 128;

                // Iterate pixels  
                for (int x = 0; x < width; x++)
                {
                    // Compute pixel brightness (i.e. total of Red, Green, and Blue values)  
                    pixelTotal = sourceBuffer[sourceIndex + 1] + sourceBuffer[sourceIndex + 2] + sourceBuffer[sourceIndex + 3];
                    if (pixelTotal > threshold)
                    {
                        destinationValue += (byte)pixelValue;
                    }
                    if (pixelValue == 1)
                    {
                        destinationBuffer[destinationIndex] = destinationValue;
                        destinationIndex++;
                        destinationValue = 0;
                        pixelValue = 128;
                    }
                    else
                    {
                        pixelValue >>= 1;
                    }
                    sourceIndex += 4;
                }
                if (pixelValue != 128)
                {
                    destinationBuffer[destinationIndex] = destinationValue;
                }
            }

            // Copy binary image data to destination bitmap  
            Marshal.Copy(destinationBuffer, 0, destinationData.Scan0, imageSize);

            // Unlock destination bitmap  
            destination.UnlockBits(destinationData);

            // Dispose of source if not originally supplied bitmap  
            if (source != pimage)
            {
                source.Dispose();
            }

            // Return  
            return destination;
        }

        public static Bitmap ArrayToBitmap(byte[] bytes, int width, int height, PixelFormat pixelFormat)
        {
            var image = new Bitmap(width, height, pixelFormat);
            var imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                              ImageLockMode.ReadWrite, pixelFormat);
            try
            {
                Marshal.Copy(bytes, 0, imageData.Scan0, bytes.Length);
            }
            finally
            {
                image.UnlockBits(imageData);
            }
            return image;
        }

    }
}
