using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Windows.Data;
using System.Globalization;

public class formResize
{
    private List<Rectangle> arr_control_bounds = new List<Rectangle>();
    private List<String> arr_control_names = new List<String>();
    private bool showRowHeader = false;
    public formResize(Form form)
    {
        this.form = form;
        formSize = form.ClientSize;
        fontSize = form.Font.Size;
    }
    private SizeF formSize { get; set; }
    private float fontSize { get; set; }
    private Form form { get; set; }
    public double form_ratio_width { get; internal set; }
    public double form_ratio_height { get; internal set; }
    public void get_initial_size()
    {
        var controls = get_all_controls(form);
        foreach (Control control in controls)
        {
            arr_control_bounds.Add(control.Bounds);
            arr_control_names.Add(control.Name);
        }
    }
    public void resize()
    {
        form_ratio_width = (double)form.ClientSize.Width / (double)formSize.Width;
        form_ratio_height = (double)form.ClientSize.Height / (double)formSize.Height;
        var controls = get_all_controls(form);

        foreach (Control control in controls)
        {
            int index = arr_control_names.FindIndex(s => s.Equals(control.Name));

            System.Drawing.Size _controlSize = new System.Drawing.Size((int)(arr_control_bounds[index].Width * form_ratio_width),
                (int)(arr_control_bounds[index].Height * form_ratio_height));

            System.Drawing.Point _controlposition = new System.Drawing.Point((int)
            (arr_control_bounds[index].X * form_ratio_width), (int)(arr_control_bounds[index].Y * form_ratio_height));

            control.Bounds = new System.Drawing.Rectangle(_controlposition, _controlSize);

            if (control.GetType() == typeof(DataGridView))
                dgv_Column_Adjust(((DataGridView)control), showRowHeader);

            if (control.GetType() == typeof(LiveCharts.WinForms.CartesianChart))
            {
                var chart = (LiveCharts.WinForms.CartesianChart)control;
                if (chart.AxisX.Count > 0 && chart.AxisY.Count > 0)
                {
                    chart.AxisX[0].FontSize = (float)(((Convert.ToDouble(fontSize) * form_ratio_width) / 2) + ((Convert.ToDouble(fontSize) * form_ratio_height) / 2));
                    chart.AxisY[0].FontSize = (float)(((Convert.ToDouble(fontSize) * form_ratio_width) / 2) + ((Convert.ToDouble(fontSize) * form_ratio_height) / 2));
                }
            }
            else
                control.Font = new System.Drawing.Font(form.Font.FontFamily, (float)(((Convert.ToDouble(fontSize) * form_ratio_width) / 2) + ((Convert.ToDouble(fontSize) * form_ratio_height) / 2)));
        }
    }
    private void dgv_Column_Adjust(DataGridView dgv, bool showRowHeader)
    {
        int intRowHeader = 0;
        const int Hscrollbarwidth = 5;
        if (showRowHeader)
            intRowHeader = dgv.RowHeadersWidth;
        else
            dgv.RowHeadersVisible = false;

        for (int i = 0; i < dgv.ColumnCount; i++)
        {
            if (dgv.Dock == DockStyle.Fill)
                dgv.Columns[i].Width = ((dgv.Width - intRowHeader) / dgv.ColumnCount);
            else
                dgv.Columns[i].Width = ((dgv.Width - intRowHeader - Hscrollbarwidth) / dgv.ColumnCount);
        }
    }
    private static IEnumerable<Control> get_all_controls(Control c)
    {
        return c.Controls.Cast<Control>().SelectMany(item =>
            get_all_controls(item)).Concat(c.Controls.Cast<Control>()).Where(control =>
            control.Name != string.Empty);
    }
}

public static class Keyboard
{
    [DllImport("user32.dll")]
    static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    const byte VK_UP = 0x26; // Arrow Up key
    const byte VK_DOWN = 0x28; // Arrow Down key

    const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag, the key is going to be pressed
    const int KEYEVENTF_KEYUP = 0x0002; //Key up flag, the key is going to be released

    public static void KeyDown()
    {
        keybd_event(VK_DOWN, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUP, 0);
    }

    public static void KeyUp()
    {
        keybd_event(VK_UP, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_UP, 0, KEYEVENTF_KEYUP, 0);
    }
}

namespace ISC_Win_WinForm_GUI
{
    public partial class MainWindow : Form
    {
        bool IsMenuStripOpen = false;

        protected override void OnResizeBegin(EventArgs e)
        {
            SuspendLayout();
            base.OnResizeBegin(e);
        }
        protected override void OnResizeEnd(EventArgs e)
        {
            ResumeLayout();
            base.OnResizeEnd(e);
        }

        void rootItem_DropDownOpened(object sender, EventArgs e)
        {
            IsMenuStripOpen = true;
        }

        void rootItem_DropDownClosed(object sender, EventArgs e)
        {
            IsMenuStripOpen = false;
        }

        void rootItem_MouseWheel(object sender, MouseEventArgs e)
        {
            if (IsMenuStripOpen)
            {
                if (e.Delta > 0)
                {
                    Keyboard.KeyUp();
                }
                else
                {
                    Keyboard.KeyDown();
                }
            }
        }

        protected virtual bool IsFileLocked(string file)
        {
            FileInfo f = new FileInfo(file);
            try
            {
                using (FileStream stream = f.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                /* The file is unavailable because it is:
                *  1. still being written to
                *  2. or being processed by another thread
                *  3. or does not exist (has already been processed)
                */
                return true;
            }
            // File is not locked
            return false;
        }

        public class SavedScanData
    	{
        	public bool Select { get; set; }
        	public string FileName { get; set; }
        	public DateTime TimeStamp { get; set; }
    	}

        private void dataGridViewSort(String column, SortOrder sortOrder)
        {
            switch (column)
            {
                case "FileName":
                    {
                        if (sortOrder == SortOrder.Ascending)
                            dataGridView_savescan.DataSource = SavedScanList.OrderBy(x => x.FileName).ToList();
                        else
                            dataGridView_savescan.DataSource = SavedScanList.OrderByDescending(x => x.FileName).ToList();
                        break;
                    }
                case "TimeStamp":
                    {
                        if (sortOrder == SortOrder.Ascending)
                            dataGridView_savescan.DataSource = SavedScanList.OrderBy(x => x.TimeStamp).ToList();
                        else
                            dataGridView_savescan.DataSource = SavedScanList.OrderByDescending(x => x.TimeStamp).ToList();
                        break;
                    }
            }

        }


        //[DllImport("user32.dll")]
        //static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        //[DllImport("user32.dll")]
        //static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //const int GWL_STYLE = -16;
        //const int ES_LEFT = 0x0000;
        //const int ES_CENTER = 0x0001;
        //const int ES_RIGHT = 0x0002;

        //[StructLayout(LayoutKind.Sequential)]
        //public struct RECT
        //{
        //    public int Left;
        //    public int Top;
        //    public int Right;
        //    public int Bottom;
        //    public int Width { get { return Right - Left; } }
        //    public int Height { get { return Bottom - Top; } }
        //}

        //private const int SWP_NOSIZE = 0x0001;
        //private const int SWP_NOZORDER = 0x0004;
        //private const int SWP_SHOWWINDOW = 0x0040;
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        //    int X, int Y, int cx, int cy, int uFlags);

        //[DllImport("user32.dll")]
        //public static extern bool GetComboBoxInfo(IntPtr hWnd, ref COMBOBOXINFO pcbi);

        //[StructLayout(LayoutKind.Sequential)]
        //public struct COMBOBOXINFO
        //{
        //    public int cbSize;
        //    public RECT rcItem;
        //    public RECT rcButton;
        //    public int stateButton;
        //    public IntPtr hwndCombo;
        //    public IntPtr hwndEdit;
        //    public IntPtr hwndList;
        //}
        //private int buttonWidth = SystemInformation.HorizontalScrollBarArrowWidth;

        //private void cbxDesign_DrawItem(object sender, DrawItemEventArgs e)
        //{
        //    // By using Sender, one method could handle multiple ComboBoxes
        //    ComboBox cbx = sender as ComboBox;
        //    if (cbx != null)
        //    {
        //        // Always draw the background
        //        e.DrawBackground();

        //        // Drawing one of the items?
        //        if (e.Index >= 0)
        //        {
        //            // Set the string alignment.  Choices are Center, Near and Far
        //            StringFormat sf = new StringFormat();
        //            sf.LineAlignment = StringAlignment.Center;
        //            sf.Alignment = StringAlignment.Center;

        //            // Set the Brush to ComboBox ForeColor to maintain any ComboBox color settings
        //            // Assumes Brush is solid
        //            Brush brush = new SolidBrush(cbx.ForeColor);

        //            // If drawing highlighted selection, change brush
        //            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        //                brush = SystemBrushes.HighlightText;

        //            // Draw the string
        //            e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, brush, e.Bounds, sf);
        //        }
        //    }
        //}

        //private void ComboBox_PGAGain_HandleCreated(Object sender, EventArgs e)
        //{
        //    SetupEdit();
        //}

        //private void SetupEdit()
        //{
        //    var info = new COMBOBOXINFO();
        //    info.cbSize = Marshal.SizeOf(info);
        //    GetComboBoxInfo(this.Handle, ref info);
        //    var style = GetWindowLong(info.hwndEdit, GWL_STYLE);
        //    style |= ES_CENTER;
        //    SetWindowLong(info.hwndEdit, GWL_STYLE, style);
        //}

        Func<double, string> chartLabelFormatFunc = (x) => string.Format("{0:N4}", x);
    }
}

public class MyComboBox : ComboBox
{
    public MyComboBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
    }

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    const int GWL_STYLE = -16;
    const int ES_LEFT = 0x0000;
    const int ES_CENTER = 0x0001;
    const int ES_RIGHT = 0x0002;
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }
    [DllImport("user32.dll")]
    public static extern bool GetComboBoxInfo(IntPtr hWnd, ref COMBOBOXINFO pcbi);

    [StructLayout(LayoutKind.Sequential)]
    public struct COMBOBOXINFO
    {
        public int cbSize;
        public RECT rcItem;
        public RECT rcButton;
        public int stateButton;
        public IntPtr hwndCombo;
        public IntPtr hwndEdit;
        public IntPtr hwndList;
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetupEdit();
    }
    private int buttonWidth = SystemInformation.HorizontalScrollBarArrowWidth;
    private void SetupEdit()
    {
        var info = new COMBOBOXINFO();
        info.cbSize = Marshal.SizeOf(info);
        GetComboBoxInfo(this.Handle, ref info);
        var style = GetWindowLong(info.hwndEdit, GWL_STYLE);
        style |= 1;
        SetWindowLong(info.hwndEdit, GWL_STYLE, style);
    }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        base.OnDrawItem(e);
        e.DrawBackground();
        var txt = "";
        if (e.Index >= 0)
            txt = GetItemText(Items[e.Index]);
        TextRenderer.DrawText(e.Graphics, txt, Font, e.Bounds,
            this.Enabled ? ForeColor : Color.Gray, TextFormatFlags.Left | TextFormatFlags.HorizontalCenter);
    }
}

public class ColourGenerator
{

    private int index = 0;
    private IntensityGenerator intensityGenerator = new IntensityGenerator();

    public string NextColour()
    {
        const double lumenLimit = 159.0;
        double lumen = 0;
        string colour = "";

        colour = string.Format(PatternGenerator.NextPattern(index), intensityGenerator.NextIntensity(index++));

        // Brightness = 0.299 * R + 0.587 * G + 0.114 * B
        double r = int.Parse(colour.Substring(0, 2), NumberStyles.HexNumber);
        double g = int.Parse(colour.Substring(2, 2), NumberStyles.HexNumber);
        double b = int.Parse(colour.Substring(4, 2), NumberStyles.HexNumber);
        lumen = 0.299 * r + 0.587 * g + 0.114 * b;

        if (lumen > lumenLimit)
        {
            r *= Math.Abs(2 * lumenLimit - lumen) / lumen;
            g *= Math.Abs(2 * lumenLimit - lumen) / lumen;
            b *= Math.Abs(2 * lumenLimit - lumen) / lumen;
        }

        colour = string.Format("{0:X2}{1:X2}{2:X2}", (int)r, (int)g, (int)b);
        return colour;
    }
}

public class PatternGenerator
{
    public static string NextPattern(int index)
    {
        switch (index % 7)
        {
            case 0: return "00{0}{0}";
            case 1: return "{0}{0}00";
            case 2: return "0000{0}";
            case 3: return "{0}00{0}";
            case 4: return "00{0}00";
            case 5: return "{0}{0}{0}";
            case 6: return "{0}0000";
            default: throw new Exception("Math error");
        }
    }
}

public class IntensityGenerator
{
    private IntensityValueWalker walker;
    private int current;

    public string NextIntensity(int index)
    {
        if (index == 0)
        {
            current = 255;
        }
        else if (index % 7 == 0)
        {
            if (walker == null)
            {
                walker = new IntensityValueWalker();
            }
            else
            {
                walker.MoveNext();
            }
            current = walker.Current.Value;
        }
        string currentText = current.ToString("X");
        if (currentText.Length == 1) currentText = "0" + currentText;
        return currentText;
    }
}

public class IntensityValue
{

    private IntensityValue mChildA;
    private IntensityValue mChildB;

    public IntensityValue(IntensityValue parent, int value, int level)
    {
        if (level > 7) throw new Exception("There are no more colours left");
        Value = value;
        Parent = parent;
        Level = level;
    }

    public int Level { get; set; }
    public int Value { get; set; }
    public IntensityValue Parent { get; set; }

    public IntensityValue ChildA
    {
        get
        {
            return mChildA ?? (mChildA = new IntensityValue(this, this.Value - (1 << (7 - Level)), Level + 1));
        }
    }

    public IntensityValue ChildB
    {
        get
        {
            return mChildB ?? (mChildB = new IntensityValue(this, Value + (1 << (7 - Level)), Level + 1));
        }
    }
}

public class IntensityValueWalker
{

    public IntensityValueWalker()
    {
        Current = new IntensityValue(null, 1 << 7, 1);
    }

    public IntensityValue Current { get; set; }

    public void MoveNext()
    {
        if (Current.Parent == null)
        {
            Current = Current.ChildA;
        }
        else if (Current.Parent.ChildA == Current)
        {
            Current = Current.Parent.ChildB;
        }
        else
        {
            int levelsUp = 1;
            Current = Current.Parent;
            while (Current.Parent != null && Current == Current.Parent.ChildB)
            {
                Current = Current.Parent;
                levelsUp++;
            }
            if (Current.Parent != null)
            {
                Current = Current.Parent.ChildB;
            }
            else
            {
                levelsUp++;
            }
            for (int i = 0; i < levelsUp; i++)
            {
                Current = Current.ChildA;
            }

        }
    }
}