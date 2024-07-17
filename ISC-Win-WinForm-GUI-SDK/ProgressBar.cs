using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ISC_Win_WinForm_GUI
{
    public partial class ProgressBar : Form
    {
        private readonly int origFormWidth = 414;
        private readonly int origFormHeight = 175;

        private readonly int tbWidth = 343;
        private readonly int tbHeight = 39;
        private readonly int tbPosX = 27;
        private readonly int tbPosY = 12;

        private readonly int btnWidth = 75;
        private readonly int btnHeight = 33;
        private readonly int btnPosX = 156;
        private readonly int btnPosY = 93;

        private readonly int pgbWidth = 343;
        private readonly int pgbHeight = 23;
        private readonly int pgbPosX = 27;
        private readonly int pgbPosY = 59;

        System.Timers.Timer onTopTimer = new System.Timers.Timer();
        public static event Action UserCancelRequest = null;
        internal static bool SendUserCancelRequest { set { UserCancelRequest(); } }
        public ProgressBar(String Title, String Content, Boolean Cancellable, double? scaleX, double? scaleY)
        {
            InitializeComponent();
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = graphics.DpiX;
                float dpiY = graphics.DpiY;

                if (scaleX == null || scaleX == 0)
                    scaleX = 1.0;
                if (scaleY == null || scaleY == 0)
                    scaleY = 1.0;

                float factorX = (float)((double)(dpiX / 96) * scaleX);
                float factorY = (float)((double)(dpiY / 96) * scaleY);

                System.Drawing.Size newSize = new System.Drawing.Size((int)(origFormWidth * factorX), (int)(origFormHeight * factorY));
                System.Drawing.Point newPosition = new System.Drawing.Point(this.Bounds.Location.X, this.Bounds.Location.Y);

                this.Bounds = new System.Drawing.Rectangle(newPosition, newSize);
                this.Font = new System.Drawing.Font(this.Font.FontFamily, (float)((9F * scaleX) / 2) + (float)((9F * scaleY) / 2),
                    System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                newSize = new System.Drawing.Size((int)(tbWidth * factorX), (int)(tbHeight * factorY));
                newPosition = new System.Drawing.Point((int)(tbPosX * factorX), (int)(tbPosY * factorY));
                tb_content.Bounds = new System.Drawing.Rectangle(newPosition, newSize);
                tb_content.Font = new System.Drawing.Font(this.Font.FontFamily, (float)((9F * scaleX) / 2) + (float)((9F * scaleY) / 2),
                    System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                newSize = new System.Drawing.Size((int)(btnWidth * factorX), (int)(btnHeight * factorY));
                newPosition = new System.Drawing.Point((int)(btnPosX * factorX), (int)(btnPosY * factorY));
                button_cancel.Bounds = new System.Drawing.Rectangle(newPosition, newSize);

                newSize = new System.Drawing.Size((int)(pgbWidth * factorX), (int)(pgbHeight * factorY));
                newPosition = new System.Drawing.Point((int)(pgbPosX * factorX), (int)(pgbPosY * factorY));
                progressBar1.Bounds = new System.Drawing.Rectangle(newPosition, newSize);
            }
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MainWindow.RequestPBWClose += new Action(ProgressCompleted);
            MainWindow.RequestPBWContentChange += new Action<string>(UpdateContent);
            this.Text = Title;
            tb_content.AutoSize = true;
            Point content_start_pos = new Point();
            content_start_pos.Y = tb_content.Location.Y;
            content_start_pos.X = (int) (this.Location.X + this.Width * 0.025);
            tb_content.Location = content_start_pos;
            tb_content.Width = (int)(this.Width * 0.9);
            tb_content.Text = Content;
            if(!Cancellable)
            {
                button_cancel.Visible = false;
                this.Height = (int)(this.Height * 0.8);
            }
            onTopTimer.Interval = 250;
            onTopTimer.Enabled = true;
            onTopTimer.Elapsed += onTopTimer_Elapsed;
            onTopTimer.Start();
        }

        private void onTopTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.BringToFront();
        }

        private void ProgressCompleted()
        {
            this.Close();
        }

        private void UpdateContent(String newContent)
        {
            tb_content.Text = newContent;
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            button_cancel.Width = button_cancel.Width * 2;
            Point newLocation = new Point();
            newLocation.X = this.Width / 2 - button_cancel.Width / 2;
            newLocation.Y = button_cancel.Location.Y;
            button_cancel.Location = newLocation;
            button_cancel.Text = "Cancelling...";
            button_cancel.Enabled = false;

            SendUserCancelRequest = true;
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }
    }
}
