using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using ZPLPrinter.Conig;

namespace ZPLPrinter.Config
{
    /// <summary>
    /// 设置
    /// </summary>
    public class Setting
    {

        private readonly static string SETTING_FOLDER = "./Config/";

        private readonly static String SETTING_FILE_NANE = SETTING_FOLDER + "Setting.json";

        private static Setting _current;

        private readonly static string DICT_FILE_NAME = SETTING_FOLDER + "dict.xlsx";


        public int DPI { get; set; } = 300;

        public string OutPut { get; set; } = "JPG";

        public string PortName { get; set; } = "com11";

        public int BaudRate { get; set; } = 57600;

        public int StopBit { get; set; } = 1;

        // 校验位
        public int ParityBit { get; set; } = 0;

        public int DataBit { get; set; } = 8;

        public int defaultWidth = 1024;
        public int defaultHeight = 1748;

        public List<KeyValue> Fonts { get; set; } = new List<KeyValue>();

        public List<KeyValue> Graphics { get; set; } = new List<KeyValue>();

        public void AddFont(string key, string value)
        {
            var item = Fonts.FirstOrDefault(x => x.Key == key);
            if (item != null)
            {
                Fonts.Remove(item);
            }

            Fonts.Add(new KeyValue(key, value));
            Save();
        }

        public void UpdateFont(string key, string newKey)
        {
            var item = Fonts.FirstOrDefault(x => x.Key == key);
            if (item != null)
            {
                item.Key = newKey;
                Save();
            }
        }

        public void RemoveFont(string key)
        {
            var item = Fonts.FirstOrDefault(x => x.Key == key);
            if (item != null)
            {
                Fonts.Remove(item);
                Save();
            }
        }

        public void AddGraphic(string key, string value)
        {
            var item = Graphics.FirstOrDefault(x => x.Key == key);
            if (item != null)
            {
                Graphics.Remove(item);
            }

            Graphics.Add(new KeyValue(key, value));
            Save();
        }

        public void UpdateGraphic(string key, string newKey)
        {
            var item = Graphics.FirstOrDefault(x => x.Key == key);
            if (item != null)
            {
                item.Key = newKey;
                Save();
            }
        }

        public void RemoveGraphic(string key)
        {
            var item = Graphics.FirstOrDefault(x => x.Key == key);
            if (item != null)
            {
                Graphics.Remove(item);
                Save();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void Save()
        {
            if (!Directory.Exists(SETTING_FOLDER))
            {
                Directory.CreateDirectory(SETTING_FOLDER);
            }

            if (File.Exists(SETTING_FILE_NANE))
            {
                File.Delete(SETTING_FILE_NANE);
            }

            var jsonString = JsonConvert.SerializeObject(_current);
            File.WriteAllText(SETTING_FILE_NANE, jsonString);

        }

        /// <summary>
        /// 当前设置项
        /// </summary>
        public static Setting Current
        {
            get
            {
                if (_current == null)
                {
                    if (!Directory.Exists(SETTING_FOLDER))
                    {
                        Directory.CreateDirectory(SETTING_FOLDER);
                    }

                    if (!File.Exists(SETTING_FILE_NANE))
                    {
                        _current = new Setting();

                        var jsonString = JsonConvert.SerializeObject(_current);
                        File.WriteAllText(SETTING_FILE_NANE, jsonString);

                        return _current;
                    }

                    var settingString = File.ReadAllText(SETTING_FILE_NANE);
                    _current = JsonConvert.DeserializeObject<Setting>(settingString);

                }
                return _current;
            }


        }
    }
}
