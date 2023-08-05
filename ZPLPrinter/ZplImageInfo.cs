using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPLPrinter
{
    public class ZplImageInfo
    {
        public ZplImageInfo() { }

        public ZplImageInfo(int width, int height)
        {
            Width = width;
            Height = height;
            Data = new byte[Width * RowRealBytesCount];
        }
        public int Width { get; set; }

        public int Height { get; set; }

        public int RowRealBytesCount { get { return (int)Math.Ceiling((double)Width / 8); } }

        public int RowSize { get { return (((Width) + 31) >> 5) << 2; } }

        public byte[] Data { get; set; }

        public void Inverse()
        {
            // 反色处理，黑白颜色对调
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] ^= 0xFF;
            }
        }

        public string ToHex()
        {

            return BitConverter.ToString(Data).Replace("-", string.Empty);

        }
    }
}
