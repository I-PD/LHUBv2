using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ISC_Win_WinForm_GUI
{
    public partial class DeviceSelection : Form
    {
        public string SelectedDeviceSerialNumber { get; set; }
        private string[,] DeviceList { get; set; }
        public DeviceSelection(string[,] DevList)
        {
            InitializeComponent();
            this.ControlBox = false;
            for (int i = 0; i < DevList.GetLength(0); i++) 
            {
                listBox_DevSel.Items.Add(String.Format($"{DevList[i,0]} : {DevList[i, 1]}"));
            }
            this.DeviceList = DevList;
        }

        private void button_Select_Click(object sender, EventArgs e)
        {
            if (listBox_DevSel.SelectedIndex == -1)
                MessageBox.Show("Please select a device to connect!");
            else
            {
                SelectedDeviceSerialNumber = DeviceList[listBox_DevSel.SelectedIndex, 1];
                this.Close();
            }
        }
    }
}
