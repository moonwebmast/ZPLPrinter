using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZPLPrinter
{
    public class ZplZ64Compression
    {
        //^GFA,04608,04608,00024,
        //:Z64:eJztlsFqFEEQhnuzswxkwTmJF6F9g1yDkcwcBK85GHKSfRFh5g18hbxBrgEPmYMPkItHyYA3TyussOxstu3q7uqq6mkE8WhKMPj7pfavqt7uUuop/jGK4m/o2YeHb18z+tGdMbtuiq+MjV9THGRzmPyCw41Zp/pnM8KfrVSL0oR4K//jBPVBumlR32TcQOyEHtMb02TTG3PN9ZZ0UQHJhlewYPqe6XOmH5iumW6YXnO9y9oxps/a4R2aCZ0K4Hb4CErBUwGV0KkAtP/g/n5M7R8+JXywv3/nfzaJ/aGSPNpvtGyE18cd+kK9RN9aNqjEvpxI/oX/533Kx7S15APWXLSSr4PtV7XU22DnItHPnP2t6rw+Br3w/HrRJ/mD/aPA44BDe/pZnu/myPeCb8pG8vNg/5mqM7rtZtIfr2/VsapH3h/f5s2E9/panSfzQv0l8qL9gzpV8jzokPQjDlTwnXoTK0l5rMTHMvCntlMjO/+ebyy/CJWw8dqk29DZQfDK8n7S9wl/G6zh8WmJ1/6TJvnnzL7n946fMfue3zoejtKQ8IXl1ZUxX3K8uvmhMvmVev5epfytkiF4Fsif53irt4kOxx+a/jrHb6Y85q+7DG8vNd3n+SrHb/7AD/n8JVycxZSfQ0OPp/lLmFSZ4WGyZSf5LQwX9H7CK+CZKey/avtgKuGhQeV2yuv7YCrhlzZ3tZvwhYYijOSdd2gSPUd4/l1uTc9Rhfx8l+ehYE3PUeSh4Ire08jDidB0/vH77gpeEu9vhR4u0h5+WeqD50/S5wJsaMdjAfPIV+4JkPzG8y29p3gfumHVpPv7dh/5WIDToe9VrwwtEGF5aIBfYOmRBxvVwC+IsPv0MOAFtgSixgI8n1xA8A1Yl1g68e4bUGHpNIBH4DWWSAOAYQ011z1vDel1PHvUOJu2Ggzn6b3+GUdBDbUdqr7HUVBDbV/CB8UTFPSuMoLHfaOPxkKccX6kExr4IfAo47uV7ifRhza83FhA4Cf71Xol7ccXvpb2ccK+m3zf8wWsHT92pPvEm1baj09qYl8ufHxf5QvlwGS+UPacr0nvuK5J57Jajq7zRq7PfIGW+zwZupb6CvVG6ssgy/WfPiBJr9JdL4ZfEh9TWak7qOB6ql9l3EAc3ZhDBrdP7+VlTn6Kp/gP4zdEPduJ:C0BE

        public static string EncodeZ64Command(byte[] data)
        {
            var z64Str = CompressZ64Data(data);
            var crcStr = getCRCHexString(Encoding.ASCII.GetBytes(z64Str));
            return string.Format(":Z64:{0}:{1}", z64Str, crcStr);
        }

        ////:Z64:{Data}:{CRC}
        public static byte[] DecodeZ64Command(string commandStr)
        {
            var cmds = commandStr.Split(":", StringSplitOptions.RemoveEmptyEntries);
            if (cmds.Length != 3)
            {
                throw new ArgumentException("无效参数");
            }
            var encodeType = cmds[0];
            var encodeData = cmds[1];
            var crcData = cmds[2];

            var crcStr = getCRCHexString(Encoding.ASCII.GetBytes(encodeData));
            if (crcData.ToLower() != crcStr.ToLower())
            {
                throw new ArgumentException("无效参数,校验失败。");
            }

            return DecompressZ64Data(encodeData);
        }

        /// <summary>
        /// Z64数据编码
        /// LZ77 + Base64
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CompressZ64Data(byte[] data)
        {
            //压缩后补充前2个字节，编码
            var compressData = Compress(data);
            var fData = new byte[compressData.Length + 2];
            compressData.CopyTo(fData, 2);
            return Convert.ToBase64String(fData);
        }

        /// <summary>
        /// Z64字符解码
        /// </summary>
        /// <param name="compressedString"></param>
        /// <returns></returns>
        public static byte[] DecompressZ64Data(string compressedString)
        {
            //截掉前2个字节，进行解压
            var b64 = Convert.FromBase64String(compressedString.Split(':')[0]).Skip(2).ToArray();
            return Decompress(b64);
        }

        /// <summary>
        /// LZ77 压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data)
        {
            byte[] compressedArray = null;
            try
            {
                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
                    {
                        deflateStream.Write(data, 0, data.Length);
                    }
                    compressedArray = compressedStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("无效参数,压缩异常。");
            }

            return compressedArray;
        }

        /// <summary>
        /// LZ77解压
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] data)
        {
            byte[] decompressedArray = null;
            try
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (MemoryStream compressStream = new MemoryStream(data))
                    {
                        using (DeflateStream deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressedStream);
                        }
                    }
                    decompressedArray = decompressedStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("无效参数,解压失败。");
            }

            return decompressedArray;
        }


        private static String getCRCHexString(byte[] bytes)
        {
            int crc = 0x0000;           // initial value
            int polynomial = 0x1021;    // 0001 0000 0010 0001  (0, 5, 12)
            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    Boolean bit = ((b >> (7 - i) & 1) == 1);
                    Boolean c15 = ((crc >> 15 & 1) == 1);
                    crc <<= 1;

                    if (c15 ^ bit)
                    {
                        crc ^= polynomial;
                    }
                }
            }

            crc &= 0xffff;
            return crc.ToString("X");
        }
    }
}
