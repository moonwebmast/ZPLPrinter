using System;
using System.Collections.Generic;
using System.Text;

namespace ZPLPrinter
{
    public class HSCommand
    {
        const byte STX = 2;
        const byte ETX = 3;
        const byte CR = 13;
        const byte LF = 10;

        /// <summary>
        /// 缺纸
        /// </summary>
        public int b { get; set; } = 0;

        /// <summary>
        /// 暂停
        /// </summary>
        public int c { get; set; } = 0;

        /// <summary>
        /// 打印头抬起
        /// </summary>
        public int o { get; set; } = 0;

        /// <summary>
        /// 碳带用尽
        /// </summary>
        public int p { get; set; } = 0;

        private string Str1
        {
            get
            {
                return "\x02" + $"\000,{b},{c},0000,000,0,0,0,000,0,0,0" + "\x03\x13\x10";
            }
        }

        private string Str2
        {
            get
            {
               
                return "\x02" + $"\000,0,{o},{p},0,0,0,0,00000000,0,000" + "\x03\x13\x10";
            }
        }

        public byte[] Output
        {
            get
            {
                string res = Str1 + Str2;
                return Encoding.UTF8.GetBytes(res);
            }
        }
    }
}
