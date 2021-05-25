namespace Image_Combinator
{
    partial class Form2
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
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.comboBoxShop = new System.Windows.Forms.ComboBox();
            this.label30 = new System.Windows.Forms.Label();
            this.checkedListBoxBrands = new System.Windows.Forms.CheckedListBox();
            this.checkBoxAllBrands = new System.Windows.Forms.CheckBox();
            this.button3 = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.button_UM = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(632, 596);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(88, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Analyze DB";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(198, 578);
            this.textBox1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(538, 596);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(88, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Create by SKU";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // comboBoxShop
            // 
            this.comboBoxShop.FormattingEnabled = true;
            this.comboBoxShop.Location = new System.Drawing.Point(222, 598);
            this.comboBoxShop.Name = "comboBoxShop";
            this.comboBoxShop.Size = new System.Drawing.Size(88, 21);
            this.comboBoxShop.TabIndex = 68;
            // 
            // label30
            // 
            this.label30.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label30.Location = new System.Drawing.Point(120, 599);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(96, 24);
            this.label30.TabIndex = 67;
            this.label30.Text = "Current Shop:";
            // 
            // checkedListBoxBrands
            // 
            this.checkedListBoxBrands.CheckOnClick = true;
            this.checkedListBoxBrands.ColumnWidth = 70;
            this.checkedListBoxBrands.FormattingEnabled = true;
            this.checkedListBoxBrands.Location = new System.Drawing.Point(234, 12);
            this.checkedListBoxBrands.MultiColumn = true;
            this.checkedListBoxBrands.Name = "checkedListBoxBrands";
            this.checkedListBoxBrands.Size = new System.Drawing.Size(486, 574);
            this.checkedListBoxBrands.TabIndex = 69;
            // 
            // checkBoxAllBrands
            // 
            this.checkBoxAllBrands.AutoSize = true;
            this.checkBoxAllBrands.Location = new System.Drawing.Point(12, 600);
            this.checkBoxAllBrands.Name = "checkBoxAllBrands";
            this.checkBoxAllBrands.Size = new System.Drawing.Size(102, 17);
            this.checkBoxAllBrands.TabIndex = 70;
            this.checkBoxAllBrands.Text = "select all brands";
            this.checkBoxAllBrands.UseVisualStyleBackColor = true;
            this.checkBoxAllBrands.CheckedChanged += new System.EventHandler(this.checkBoxAllBrands_CheckedChanged);
            // 
            // button3
            // 
            this.button3.Enabled = false;
            this.button3.Location = new System.Drawing.Point(417, 596);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(115, 23);
            this.button3.TabIndex = 71;
            this.button3.Text = "Get picURLs by SKU";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 623);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(708, 20);
            this.progressBar1.Step = 1;
            this.progressBar1.TabIndex = 72;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // backgroundWorker2
            // 
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker2_DoWork);
            this.backgroundWorker2.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker2.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // button_UM
            // 
            this.button_UM.Location = new System.Drawing.Point(316, 596);
            this.button_UM.Name = "button_UM";
            this.button_UM.Size = new System.Drawing.Size(95, 23);
            this.button_UM.TabIndex = 73;
            this.button_UM.Text = "get UM pictures";
            this.button_UM.UseVisualStyleBackColor = true;
            this.button_UM.Click += new System.EventHandler(this.button_UM_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(732, 655);
            this.Controls.Add(this.button_UM);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.checkBoxAllBrands);
            this.Controls.Add(this.checkedListBoxBrands);
            this.Controls.Add(this.comboBoxShop);
            this.Controls.Add(this.label30);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Name = "Form2";
            this.Text = "Pictures Creator";
            this.Shown += new System.EventHandler(this.Form2_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ComboBox comboBoxShop;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.CheckedListBox checkedListBoxBrands;
        private System.Windows.Forms.CheckBox checkBoxAllBrands;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private System.Windows.Forms.Button button_UM;
    }
}