namespace ISC_Win_WinForm_GUI
{
    partial class DeviceSelection
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBox_DevSel = new System.Windows.Forms.ListBox();
            this.button_Select = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBox_DevSel
            // 
            this.listBox_DevSel.FormattingEnabled = true;
            this.listBox_DevSel.Location = new System.Drawing.Point(12, 16);
            this.listBox_DevSel.Name = "listBox_DevSel";
            this.listBox_DevSel.Size = new System.Drawing.Size(218, 69);
            this.listBox_DevSel.TabIndex = 0;
            // 
            // button_Select
            // 
            this.button_Select.Location = new System.Drawing.Point(49, 98);
            this.button_Select.Name = "button_Select";
            this.button_Select.Size = new System.Drawing.Size(140, 27);
            this.button_Select.TabIndex = 1;
            this.button_Select.Text = "Connect";
            this.button_Select.UseVisualStyleBackColor = true;
            this.button_Select.Click += new System.EventHandler(this.button_Select_Click);
            // 
            // DeviceSelection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(243, 137);
            this.Controls.Add(this.button_Select);
            this.Controls.Add(this.listBox_DevSel);
            this.Name = "DeviceSelection";
            this.Text = "DeviceSelection";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_DevSel;
        private System.Windows.Forms.Button button_Select;
    }
}