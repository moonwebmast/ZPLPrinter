using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using ZPLPrinter.Config;

namespace ZPLPrinter
{
    public enum PrinterResourceType
    {
        None,
        Font,
        Graphic

    }
    public class StorageForm : Dialog
    {
        private DropDown _selectList;

        private ListBox _listBox;

        private TextBox _name;

        private ImageView _preview;

        private Button _addBtn;

        private Button _updateBtn;

        private Button _deleteBtn;

       

        PrinterResourceType _resourceType = PrinterResourceType.None;


        public Neodynamic.SDK.ZPLPrinter.ZPLPrinter VirtualPrinter { get; set; }
        public event EventHandler SettingChanged;
        public StorageForm(Neodynamic.SDK.ZPLPrinter.ZPLPrinter zplPrinter)
        {
            VirtualPrinter = zplPrinter;
            InitControl();
            InitLayout();
        }

        private void InitControl()
        {
            _selectList = new DropDown() { Width = 300 };
           
            _selectList.Items.Add("字体", "Fonts");
            _selectList.Items.Add("图像", "Graphics");
            

            _selectList.SelectedIndexChanged += (s, e) =>
            {
                ListPrinterResource();
            };

            _listBox = new ListBox() { Width = 300, Height = 400 };
            _listBox.SelectedIndexChanged += (s, e) =>
            {
                _preview.Image = null;

                int index = _listBox.SelectedIndex;
                if (index != -1 && VirtualPrinter != null)
                {
                    _name.Text = _listBox.SelectedValue.ToString();

                    if (_resourceType == PrinterResourceType.Graphic)
                    {
                        DisplayGraphicPreview();
                    }

                }
            };

            _name = new TextBox() { Width = 300 };

            _addBtn = new Button() { Text = "添加" };
            _addBtn.Click += (s, e) =>
            {
                string selItem = "";
                if (_listBox.Items.Count > 0)
                {
                    selItem = _listBox.SelectedValue.ToString();
                }

                string name = _name.Text;
                if (!string.IsNullOrWhiteSpace(name) && VirtualPrinter != null && name != selItem)
                {
                    if (_resourceType == PrinterResourceType.Font)
                    {
                        var ofd = new OpenFileDialog();
                        ofd.Filters.Add(new FileFilter("TruType Font file (*.TTF)|*.TTF"));
                        if (ofd.ShowDialog(this) == DialogResult.Ok)
                        {
                            VirtualPrinter.AddFont(name, ofd.FileName);
                            Setting.Current.AddFont(name, ofd.FileName);
                        }
                    }
                    else if (_resourceType == PrinterResourceType.Graphic)
                    {
                        var ofd = new OpenFileDialog();
                        ofd.Filters.Add(new FileFilter ("Image Files(*.JPG;*.PNG)|*.JPG;*.PNG"));
                        if (ofd.ShowDialog(this) == DialogResult.Ok)
                        {
                            VirtualPrinter.AddGraphic(name, ofd.FileName);
                            Setting.Current.AddGraphic(name, ofd.FileName);
                           
                            DisplayGraphicPreview();
                        }
                    }

                   
                }
                else
                {
                    MessageBox.Show("请输入有效的名称。");
                }
                ListPrinterResource();
            };

            _updateBtn = new Button() { Text = "修改" };
            _updateBtn.Click += (s, e) =>
            {
                int index = _listBox.SelectedIndex;
                string newName = _name.Text;

                if (index != -1 && !string.IsNullOrWhiteSpace(newName) && VirtualPrinter != null)
                {
                    string curName = _listBox.SelectedValue.ToString();

                    if (_resourceType == PrinterResourceType.Font)
                    {
                        VirtualPrinter.RenameFont(curName, newName);
                        ListPrinterResource();
                    }
                    else if (_resourceType == PrinterResourceType.Graphic)
                    {
                        VirtualPrinter.RenameGraphic(curName, newName);
                        ListPrinterResource();
                        DisplayGraphicPreview();
                    }
                }
            };

            _deleteBtn = new Button() { Text = "删除" };
            _deleteBtn.Click += (s, e) =>
            {
                int index = _listBox.SelectedIndex;
                if (index != -1 && VirtualPrinter != null)
                {
                    clearTxt();

                    string curName = _listBox.SelectedValue.ToString();

                    if (_resourceType == PrinterResourceType.Font)
                    {
                        VirtualPrinter.RemoveFont(curName);
                        ListPrinterResource();
                    }
                    else if (_resourceType == PrinterResourceType.Graphic)
                    {
                        VirtualPrinter.RemoveGraphic(curName);
                        ListPrinterResource();
                        DisplayGraphicPreview();
                    }
                }
            };

            _preview = new ImageView() { Width = 300, Height = 300 ,BackgroundColor = Colors.White};

        }

        private void InitLayout()
        {
            StackLayout rootLayout = new StackLayout()
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Padding = new Eto.Drawing.Padding(10, 10),
                Spacing = 20,
                Items = {
                    new StackLayoutItem(){
                        Control = new StackLayout(){
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                             Padding = new Eto.Drawing.Padding(10,10),
                             Spacing = 5,
                            Items = {
                                new Label(){ Text = "资源类型"},
                                _selectList,
                                new StackLayoutItem(){
                                    Expand = true,
                                    Control = _listBox }
                            }
                        }
                    },
                    new StackLayoutItem(){
                        Control = new StackLayout(){
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                             Padding = new Eto.Drawing.Padding(10,10),
                              Spacing = 5,
                            Items = {
                                new Label(){ Text = "名称：([DRIVE:][NAME][.EXTENSION])"},
                                _name,
                                new StackLayout{
                                    Padding = new Eto.Drawing.Padding(0,20),
                                    Spacing = 20,
                                    Orientation = Orientation.Horizontal,
                                    Items = {
                                        _addBtn,
                                        _updateBtn,
                                        _deleteBtn
                                    }
                                },
                                new Label(){ Text = "预览："},
                                new StackLayoutItem(){
                                    Expand = true,
                                    Control = _preview }
                            }
                        }
                    }
                }


            };

            this.Content = rootLayout;
        }

        private void ListPrinterResource()
        {
            clearTxt();
            _listBox.DataStore = null;
            _resourceType = PrinterResourceType.None;
            _preview.Image = null;

            

            int index = _selectList.SelectedIndex;
            if (index != -1 && VirtualPrinter != null)
            {
                _listBox.DataStore = index == 0 ? VirtualPrinter.GetFonts() : VirtualPrinter.GetGraphics();
                _resourceType = index == 0 ? PrinterResourceType.Font : PrinterResourceType.Graphic;
                //lblPreview.Visible = pnlPreview.Visible = _resourceType == PrinterResourceType.Graphic;
            }
        }

        private void clearTxt()
        {
            _name.Text = "";
        }

        private void DisplayGraphicPreview()
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
                _preview.Image = null;
            else
                _preview.Image = new Bitmap(VirtualPrinter.GetGraphic(_name.Text));
        }
    }
}
