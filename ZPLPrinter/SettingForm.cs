using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using Neodynamic.SDK.ZPLPrinter;
using System.IO.Ports;
using ZPLPrinter.Config;

namespace ZPLPrinter
{
    public class SettingForm : Dialog
    {
        public event EventHandler SettingChanged;
        DropDown _dpiList;
        DropDown _outputList;
        NumericUpDown _defaultWidth;
        NumericUpDown _defaultHeight;
        private TextBox _portName;
        private NumericUpDown _baudRate;
        private NumericUpDown _dataBit;
        private DropDown _parityBit;
        private DropDown _stopBits;

        private Button _save;

        private Button _cancel;

        public Neodynamic.SDK.ZPLPrinter.ZPLPrinter VisualPrinter { get; set; }

        public SettingForm(Neodynamic.SDK.ZPLPrinter.ZPLPrinter zplPrinter)
        {
            VisualPrinter = zplPrinter;
            InitControl();
            InitLayout();

        }

        private void InitControl()
        {
            Title = "设置";


            // 分辨率
            _dpiList = new DropDown() { Width = 150 };
            _dpiList.Items.Add("152 dpi(6 dpmm)", "152");
            _dpiList.Items.Add("203 dpi(8 dpmm)", "203");
            _dpiList.Items.Add("300 dpi(12 dpmm)", "300");
            _dpiList.Items.Add("600 dpi(24 dpmm)", "600");

            _dpiList.SelectedKey = Setting.Current.DPI.ToString();

            // 输出格式
            _outputList = new DropDown() { Width = 150 };

            _outputList.Items.Add("PNG");
            _outputList.Items.Add("JPG");
            _outputList.Items.Add("PDF");

            _outputList.SelectedKey = Setting.Current.OutPut;



            _defaultWidth = new NumericUpDown() { Value = 105, Width = 200 };
            _defaultHeight = new NumericUpDown() { Value = 148, Width = 200 };

            _portName = new TextBox() { Text = Setting.Current.PortName, Width = 200 };

            _baudRate = new NumericUpDown() { Value = Setting.Current.BaudRate, Width = 200 };

            _dataBit = new NumericUpDown() { Value = Setting.Current.DataBit, Width = 200 };

            _parityBit = new DropDown() { };
            _parityBit.DataStore = Enum.GetNames(typeof(Parity));
            _parityBit.SelectedKey = ((Parity)Setting.Current.ParityBit).ToString();


            _stopBits = new DropDown() { };
            _stopBits.DataStore = Enum.GetNames(typeof(StopBits));
            _stopBits.SelectedKey = ((StopBits)Setting.Current.StopBit).ToString();

            _save = new Button() { Text = "保存", Width = 100 };
            _save.Click += (s, e) =>
            {
                Setting.Current.DPI = int.Parse(_dpiList.SelectedKey);
                Setting.Current.OutPut = _outputList.SelectedKey;
                Setting.Current.defaultWidth = (int)_defaultWidth.Value;
                Setting.Current.defaultHeight = (int)_defaultHeight.Value;
                Setting.Current.PortName = _portName.Text;
                Setting.Current.BaudRate = (int)_baudRate.Value;
                Setting.Current.DataBit = (int)_dataBit.Value;
                Setting.Current.ParityBit = (int)Enum.Parse(typeof(Parity), _parityBit.SelectedKey);
                Setting.Current.StopBit = (int)Enum.Parse(typeof(StopBits), _stopBits.SelectedKey);
                Setting.Current.Save();
                
                this.SettingChanged?.Invoke(this, null);
                this.Close();
            };

            _cancel = new Button() { Text = "取消", Width = 100 };
            _cancel.Click += (s, e) =>
            {
                this.Close();
            };
        }

        private void InitLayout()
        {
            var rootLayout = new DynamicLayout() { };

            rootLayout.BeginVertical(new Eto.Drawing.Padding(10, 10), new Eto.Drawing.Size(10, 10));

            rootLayout.BeginHorizontal();
            rootLayout.AddColumn(NewLabel("打印精度"));
            rootLayout.AddColumn(_dpiList);

            rootLayout.AddColumn(NewLabel("输出格式"));
            rootLayout.AddColumn(_outputList);

            rootLayout.BeginHorizontal();
            rootLayout.AddColumn(NewLabel("默认宽度"));
            rootLayout.AddColumn(_defaultWidth);

            rootLayout.AddColumn(NewLabel("默认高度"));
            rootLayout.AddColumn(_defaultHeight);
            rootLayout.EndHorizontal();

            rootLayout.BeginHorizontal();
            rootLayout.AddColumn(NewLabel("串口"));
            rootLayout.AddColumn(_portName);
            rootLayout.EndHorizontal();

            rootLayout.BeginHorizontal();
            rootLayout.AddColumn(NewLabel("波特率"));
            rootLayout.AddColumn(_baudRate);

            rootLayout.AddColumn(NewLabel("数据位"));
            rootLayout.AddColumn(_dataBit);
            rootLayout.EndHorizontal();

            rootLayout.BeginHorizontal();
            rootLayout.AddColumn(NewLabel("校验位"));
            rootLayout.AddColumn(_parityBit);

            rootLayout.AddColumn(NewLabel("停止位"));
            rootLayout.AddColumn(_stopBits);
            rootLayout.EndHorizontal();

            rootLayout.BeginHorizontal();


            rootLayout.EndHorizontal();
            rootLayout.EndVertical();



            this.Content = new StackLayout()
            {
                Padding = new Eto.Drawing.Padding(30, 30),
                Spacing = 20,
                Orientation = Orientation.Vertical,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items = {
                    rootLayout,
                    new StackLayout(){
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Spacing = 20,
                        Items = {
                            new StackLayoutItem{ Expand = true, Control = new Panel() },
                            _save,
                            _cancel,
                            new StackLayoutItem{ Expand = true, Control = new Panel() }
                        }
                    }
                }
            };

        }

        private Label NewLabel(string text, int width = 100)
        {
            return new Label() { Text = text, Width = width, TextAlignment = TextAlignment.Right };
        }
    }
}
