using System;
using System.Collections.Generic;

namespace ZPLPrinter
{
    public class ZplHexCompression
    {
        public class KeyValue
        {

            public char Key { get; set; }

            public int Value { get; set; }
        }


        /// <summary>  
        /// ZPL压缩字典  
        /// </summary>  
        private static List<KeyValue> compressDictionary = new List<KeyValue>();

        static ZplHexCompression()
        {
            #region LZ77编码字典
            //G H I J K L M N O P Q R S T U V W X Y        对应1,2,3,4……18,19。  
            //g h i j k l m n o p q r s t u v w x y z      对应20,40,60,80……340,360,380,400。            

            for (int i = 0; i < 19; i++)
            {
                compressDictionary.Add(new KeyValue() { Key = Convert.ToChar(71 + i), Value = i + 1 });
            }
            for (int i = 0; i < 20; i++)
            {
                compressDictionary.Add(new KeyValue() { Key = Convert.ToChar(103 + i), Value = (i + 1) * 20 });
            }
            #endregion
        }

        /// <summary>
        /// LZ77 压缩
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string CompressLZ77(byte[] data)
        {
            // 二进制转成16进制
            string text = BitConverter.ToString(data).Replace("-", string.Empty);
            //将转成16进制的文本进行压缩  
            string result = string.Empty;
            char[] arrChar = text.ToCharArray();
            int count = 1;
            for (int i = 1; i < text.Length; i++)
            {
                if (arrChar[i - 1] == arrChar[i])
                {
                    count++;
                }
                else
                {
                    result += convertNumber(count) + arrChar[i - 1];
                    count = 1;
                }
                if (i == text.Length - 1)
                {
                    result += convertNumber(count) + arrChar[i];
                }
            }
            return result;
        }

        /// <summary>
        /// LZ77 解压
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string DecompressLZ77(string text)
        {
            string result = string.Empty;
            char[] arrChar = text.ToCharArray();
            int count = 0;
            for (int i = 0; i < arrChar.Length; i++)
            {
                if (isHexChar(arrChar[i]))
                {
                    //十六进制值
                    result += new string(arrChar[i], count == 0 ? 1 : count);
                    count = 0;
                }
                else
                {
                    //计算压缩码数值  
                    int value = GetCompressValue(arrChar[i]);
                    count += value;
                }
            }
            return result;
        }

        /// <summary>
        /// 压缩后的字符转数字
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int GetCompressValue(char c)
        {
            int result = 0;
            for (int i = 0; i < compressDictionary.Count; i++)
            {
                if (c == compressDictionary[i].Key)
                {
                    result = compressDictionary[i].Value;
                }
            }
            return result;
        }

        /// <summary>
        /// 数字转换成编码
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private static string convertNumber(int count)
        {
            //将连续的数字转换成LZ77压缩代码，如000可用I0表示。  
            string result = string.Empty;

            if (count > 1)
            {
                while (count > 0)
                {
                    for (int i = compressDictionary.Count - 1; i >= 0; i--)
                    {
                        if (count >= compressDictionary[i].Value)
                        {
                            result += compressDictionary[i].Key;
                            count -= compressDictionary[i].Value;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 是否16进制字符
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool isHexChar(char c)
        {
            return c > 47 && c < 58 || c > 64 && c < 71 || c > 96 && c < 103;
        }
    }
}
