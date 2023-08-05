using Eto.Drawing;
using Eto.Forms;

using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ZPLPrinter;
using ZPLPrinter.Config;
using P = Neodynamic.SDK.ZPLPrinter;

namespace ZPLPrinter
{
    public partial class MainForm : Form
    {
        int _currentIdx = 0;

        int _pageCount = 0;



        Label _statusPanel = new Label() { Width = 100 };

        ImageView _imageView;

        TextArea _textPanel;

        P.ZPLPrinter zplPrinter = new P.ZPLPrinter("", "");

        System.IO.Ports.SerialPort _serial;

        HSCommand _hsCommand = new HSCommand();

        CheckBox _paperEmptyCK;

        CheckBox _ribbonEmptyCK;

        CheckBox _stopedCK;

        CheckBox _printHeadOpenCK;

        ListBox _printElementList;

        List<Byte[]> _printResult = new List<byte[]>();

        Button _preBtn;
        Button _nextBtn;
        Label _pageInfo;



        public MainForm()
        {
            //初始化日志
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File(Path.Combine(@"logs", "log.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Setting.Current.Fonts.ForEach(x => zplPrinter.AddFont(x.Key, x.Value));
            Setting.Current.Graphics.ForEach(x => zplPrinter.AddGraphic(x.Key, x.Value));


            _serial = new System.IO.Ports.SerialPort();
            _serial.DataReceived += OnSerial_DataReceived;

            Title = "ZPL Printer";
            Size = new Size(1280, 800);
            MinimumSize = new Size(800, 600);

            _imageView = new ImageView() { BackgroundColor = Colors.Black };

            _textPanel = new TextArea() { Wrap = false };


            _paperEmptyCK = new CheckBox() { Text = "缺纸" };
            _paperEmptyCK.CheckedChanged += (s, e) => { _hsCommand.b = _paperEmptyCK.Checked.Value ? 1 : 0; };

            _ribbonEmptyCK = new CheckBox() { Text = "碳带用尽" };
            _ribbonEmptyCK.CheckedChanged += (s, e) => { _hsCommand.p = _ribbonEmptyCK.Checked.Value ? 1 : 0; };

            _stopedCK = new CheckBox() { Text = "暂停" };
            _stopedCK.CheckedChanged += (s, e) => { _hsCommand.c = _stopedCK.Checked.Value ? 1 : 0; };

            _printHeadOpenCK = new CheckBox() { Text = "打印头抬起" };
            _printHeadOpenCK.CheckedChanged += (s, e) => { _hsCommand.o = _printHeadOpenCK.Checked.Value ? 1 : 0; };


            _printElementList = new ListBox() { };

            var previewBtn = new Button() { Text = "预览" };

            previewBtn.Click += (s, e) =>
            {
                PreviewZpl(_textPanel.Text);
            };

            var settingBtn = new Button() { Text = "通信设置" };
            settingBtn.Click += (s, e) =>
            {
                SettingForm _settingDlg = new SettingForm(zplPrinter);
                _settingDlg.SettingChanged += (s, e) =>
                {
                    this.Open();
                };
                _settingDlg.ShowModal(this);
            };

            var storgaeBtn = new Button() { Text = "存储管理" };
            storgaeBtn.Click += (s, e) =>
            {
                StorageForm _settingDlg = new StorageForm(zplPrinter);

                _settingDlg.ShowModal();
            };

            var insertImage = new Button() { Text = "插入图片" };
            insertImage.Click += (s, e) =>
            {
                var sd = new OpenFileDialog();
                sd.Filters.Add(new FileFilter("PNG (*.PNG)|*.png"));
                sd.Filters.Add(new FileFilter("JPG (*.JPG)|*.jpg"));
                sd.Filters.Add(new FileFilter("BMP (*.BMP)|*.bmp"));

                if (sd.ShowDialog(this) == DialogResult.Ok)
                {
                    StringBuilder sb = new StringBuilder();
                    if (_textPanel.CaretIndex > 0)
                        sb.Append(_textPanel.Text.Substring(0, _textPanel.CaretIndex));


                    var rfgaCmd = ZplImageHelper.EncodeGFACommand(sd.FileName);
                    sb.Append(rfgaCmd);
                    sb.Append("\r\n");

                    if (_textPanel.CaretIndex < _textPanel.Text.Length)
                        sb.Append(_textPanel.Text.Substring(_textPanel.CaretIndex, _textPanel.Text.Length - _textPanel.CaretIndex));

                    _textPanel.Text = sb.ToString();
                }

            };


            var quitBtn = new Button() { Text = "退出" };
            quitBtn.Click += (s, e) =>
            {
                Application.Instance.Quit();
            };

            var clearResult = new Button() { Text = "清理结果" };
            clearResult.Click += (s, e) =>
            {
                _printResult.Clear();
                _imageView.Image = null;
                this._currentIdx = -1;
                this._pageCount = 0;
                SetPageStatus(-1);
            };

            var saveAs = new Button() { Text = "保存" };
            saveAs.Click += (s, e) =>
            {
                this.SaveAs();

            };

            _preBtn = new Button() { Text = "上一个" };
            _preBtn.Click += (s, e) => { DisplayRenderOutput(_currentIdx - 1); };

            _nextBtn = new Button() { Text = "下一个" };
            _nextBtn.Click += (s, e) => { DisplayRenderOutput(_currentIdx + 1); };

            _pageInfo = new Label() { Width = 100 };

            this.Content = new StackLayout()
            {
                Padding = new Padding(6),

                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                Items = {
                    new StackLayoutItem(){
                        Control = new StackLayout(){
                            Padding = new Padding(0,6),
                            Orientation = Orientation.Horizontal,
                            Items = {
                                previewBtn,
                                storgaeBtn,
                                settingBtn,
                                insertImage,
                                quitBtn,
                                new StackLayoutItem(){ Expand = true },
                                saveAs,
                                clearResult,
                                _preBtn,
                                _nextBtn,
                                _pageInfo,
                                _statusPanel
                            }
                        }
                    },
                    new StackLayoutItem(){
                        Expand = true,
                        Control = new Splitter(){
                            FixedPanel= SplitterFixedPanel.Panel1,
                            //Panel1MinimumSize = 500,
                            Position = 600,
                            Orientation = Orientation.Horizontal,
                            Panel1 =  new Splitter(){ 
                                //Width = 500,
                                Orientation= Orientation.Vertical,
                                //Position = 500,
                                RelativePosition = 1,
                                FixedPanel = SplitterFixedPanel.Panel2,
                                //Panel2MinimumSize = 100,
                                Panel1 = new StackLayout{
                                    Spacing = 10,
                                    HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                                    VerticalContentAlignment = Eto.Forms.VerticalAlignment.Stretch,
                                    Items={
                                        new GroupBox(){ Text = "打印机状态仿真" ,
                                            Content = new StackLayout{
                                                Padding = new Padding(10,10),
                                                Spacing = 20,
                                                Orientation = Orientation.Horizontal,
                                                Items={
                                                    _paperEmptyCK,
                                                    _ribbonEmptyCK,
                                                    _stopedCK,
                                                    _printHeadOpenCK
                                                }
                                            }
                                        },
                                        new StackLayoutItem{Expand = true, Control = _textPanel }

                                    }
                                },
                                Panel2 = _printElementList
                            }  ,
                            Panel2 = _imageView
                            //new Scrollable()
                            //{
                            //    Content = _imageView
                            //}
                        }

                    }
                }
            };

            this.Open();

        }

        private void OnSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //接收到数据事件；
            //Encoding.UTF8.GetString(s.)
            System.Threading.Thread.Sleep(50);
            int len = _serial.BytesToRead;
            if (len > 0)
            {
                byte[] buffer = new byte[len];
                _serial.Read(buffer, 0, len);
                string zplCommand = Encoding.UTF8.GetString(buffer);
                if (zplCommand == "~HS")
                {
                    _serial.Write(_hsCommand.Output, 0, _hsCommand.Output.Length);
                }
                else
                {
                    Application.Instance.Invoke(() =>
                    {
                        _textPanel.Text = zplCommand;
                        PreviewZpl(zplCommand);
                    });
                }
            }
        }

        public void Open()
        {
            if (_serial.IsOpen)
                _serial.Close();

            try
            {

                _serial.PortName = Setting.Current.PortName;
                _serial.BaudRate = Setting.Current.BaudRate;
                _serial.Parity = (Parity)Setting.Current.ParityBit;
                _serial.DataBits = Setting.Current.DataBit;
                _serial.StopBits = (StopBits)Setting.Current.StopBit;



                _serial.Open();

                SetStatus($"{Setting.Current.PortName}已监听。");
            }
            catch (Exception ex)
            {
                SetStatus($"{Setting.Current.PortName}监听失败。");
                MessageBox.Show(ex.Message);
            }


        }

        public void Close()
        {
            _serial.Close();
        }



        protected override void OnUnLoad(EventArgs e)
        {
            this.Close();
            base.OnUnLoad(e);
        }

        private void SetStatus(string text)
        {
            _statusPanel.Text = text;
            _statusPanel.Invalidate();
        }

        private void PreviewZpl(string zplCommand)
        {

            // _printResult.Clear();
            try
            {
                var result = zplPrinter.ProcessCommands(zplCommand, Encoding.UTF8, true);
                _printResult.AddRange(result);
                if (_printResult.Count > 20)
                {
                    _printResult.RemoveAt(0);
                }
                else
                {

                    _pageCount += result.Count;
                }
                _currentIdx = _pageCount - 1;


                DisplayRenderOutput(_currentIdx);
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行ZPL指令异常：" + ex.Message);
            }



            DisplayElementList();

        }



        private void DisplayElementList()
        {
            this._printElementList.Items.Clear();
            if (zplPrinter != null && zplPrinter.RenderedElements != null && zplPrinter.RenderedElements.Count > 0)
            {
                zplPrinter.RenderedElements.ForEach(p => p.ForEach(x =>
                {
                    this._printElementList.Items.Add(string.IsNullOrEmpty(x.Content) ? "^" + x.Name : string.Format("^{0}: `{1}`", x.Name, x.Content));
                }));

                //foreach (var zplElem in zplPrinter.RenderedElements[iCurrPage - 1])
                //{
                //    this.lstZPLElements.Items.Add(string.IsNullOrEmpty(zplElem.Content) ? "^" + zplElem.Name : string.Format("^{0}: `{1}`", zplElem.Name, zplElem.Content));
                //}
            }
        }

        private int SetPageStatus(int idx)
        {
            if (idx < -1)
            {
                idx = -1;
            }

            if (idx > _pageCount - 1)
            {
                idx = _pageCount - 1;
            }

            this._currentIdx = idx;

            _preBtn.Enabled = this._currentIdx > 0;
            _nextBtn.Enabled = this._currentIdx < this._pageCount - 1;
            _pageInfo.Text = $"{this._currentIdx + 1}/{this._pageCount}";

            return idx;
        }

        private void SaveAs()
        {
            if (_printResult.Count > 0)
            {
                Bitmap bitmap = new Bitmap(_printResult[_currentIdx]);

                var sd = new SaveFileDialog();
                sd.Filters.Add(new FileFilter("PNG (*.PNG)|*.png"));
                if (sd.ShowDialog(this) == DialogResult.Ok)
                    bitmap.Save(sd.FileName, ImageFormat.Png);
            }

        }

        private void DisplayRenderOutput(int idx = 0)
        {
            idx = SetPageStatus(idx);


            // the buffer param contains the binary output of the ZPL rendering result
            // The format of this buffer depends on the RenderOutputFormat property setting
            if (_printResult != null && _printResult.Count > 0)
            {
                if (zplPrinter.RenderOutputFormat == P.RenderOutputFormat.PNG ||
                    zplPrinter.RenderOutputFormat == P.RenderOutputFormat.JPG)
                {
                    Bitmap bitmap = new Bitmap(_printResult[idx]);
                    _imageView.Image = bitmap;

                    //temp folder for holding thermal label images
                    //this._imageView.Clear();
                    //string myDir = Directory.GetCurrentDirectory() + @"\temp\";
                    //if (Directory.Exists(myDir) == false) Directory.CreateDirectory(myDir);
                    //DirectoryInfo di = new DirectoryInfo(myDir);
                    //foreach (FileInfo file in di.GetFiles()) file.Delete();

                    //try
                    //{
                    //    int c = buffer.Count.ToString().Length;
                    //    //save images on disk 
                    //    for (int i = 0; i < buffer.Count; i++)
                    //    {
                    //        File.WriteAllBytes(myDir + "Image" + i.ToString().PadLeft(c, '0') + "." + zplPrinter.RenderOutputFormat.ToString(), buffer[i]);
                    //    }
                    //    //preview them
                    //    Bitmap bmp = new Bitmap()
                    //    this._imageView.Image .LoadImages(myDir, ref zplPrinter);

                    //}
                    //catch (Exception ex)
                    //{
                    //    MessageBox.Show(ex.Message);
                    //}



                }
                else if (zplPrinter.RenderOutputFormat == P.RenderOutputFormat.PDF)
                {
                    var sd = new SaveFileDialog();
                    sd.Filters.Add(new FileFilter("Portable Document Format (*.pdf)|*.pdf"));
                    if (sd.ShowDialog(this) == DialogResult.Ok)
                        System.IO.File.WriteAllBytes(sd.FileName, _printResult[idx]);
                }
                //else
                //{
                //    var sd = new SaveFileDialog();
                //    if (zplPrinter.RenderOutputFormat == RenderOutputFormat.PCX)
                //    {
                //        sd.Filter = "PiCture eXchange (*.pcx)|*.pcx";
                //        sd.DefaultExt = "pcx";
                //    }
                //    else if (zplPrinter.RenderOutputFormat == RenderOutputFormat.GRF)
                //    {
                //        sd.Filter = "Zebra GRF ASCII hexadecimal (*.grf)|*.grf";
                //        sd.DefaultExt = "grf";
                //    }
                //    else if (zplPrinter.RenderOutputFormat == RenderOutputFormat.EPL)
                //    {
                //        sd.Filter = "Zebra EPL Binary (*.epl)|*.epl";
                //        sd.DefaultExt = "epl";
                //    }
                //    else if (zplPrinter.RenderOutputFormat == RenderOutputFormat.FP)
                //    {
                //        sd.Filter = "Honeywell-Intermec Fingerprint Binary (*.fp)|*.fp";
                //        sd.DefaultExt = "fp";
                //    }
                //    else if (zplPrinter.RenderOutputFormat == RenderOutputFormat.NV)
                //    {
                //        sd.Filter = "EPSON ESC/POS NV Binary (*.nv)|*.nv";
                //        sd.DefaultExt = "nv";
                //    }

                //    sd.AddExtension = true;
                //    if (sd.ShowDialog() == DialogResult.OK)
                //        System.IO.File.WriteAllBytes(sd.FileName, buffer[0]);
                //}
            }
        }
    }
}