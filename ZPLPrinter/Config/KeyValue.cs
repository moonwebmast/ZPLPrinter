using System;
using System.Collections.Generic;
using System.Text;

namespace ZPLPrinter.Conig
{
    public class KeyValue
    {
        public KeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
