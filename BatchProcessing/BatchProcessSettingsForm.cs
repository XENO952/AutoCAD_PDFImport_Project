using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoCAD_PDFImport_Project.BatchProcessing
{
    public partial class BatchProcessSettingsForm : Form
    {
        private TextBox txtCustomScale { get; set; } = new TextBox();
        private TextBox txtViewportCenterX { get; set; } = new TextBox();
        private TextBox txtViewportCenterY { get; set; } = new TextBox();
        private TextBox txtFileNameSuffix { get; set; } = new TextBox();
        private TextBox txtTotalPages { get; set; } = new TextBox();
        private TextBox txtDrawingTitle { get; set; } = new TextBox();
        private Button btnOK { get; set; } = new Button();
        private Button btnCancel { get; set; } = new Button();

        public double CustomScale { get; private set; } = 0.818355;
        public Point3d ViewportCenter { get; private set; } = new Point3d(300, 300, 0);
        public string FileNameSuffix { get; private set; } = "";
        public int TotalPages { get; set; }
        public string DrawingTitle { get; set; } = string.Empty;

        public BatchProcessSettingsForm()
        {
            this.Text = "Batch Process Settings";
            this.Width = 300;
            this.Height = 300;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Label lblCustomScale = new Label() { Text = "Custom Scale:", Left = 10, Top = 20, Width = 100 };
            txtCustomScale = new TextBox() { Left = 120, Top = 20, Width = 100, Text = "0.818355" };

            Label lblViewportCenterX = new Label() { Text = "Viewport Center X:", Left = 10, Top = 60, Width = 100 };
            txtViewportCenterX = new TextBox() { Left = 120, Top = 60, Width = 100, Text = "300" };

            Label lblViewportCenterY = new Label() { Text = "Viewport Center Y:", Left = 10, Top = 100, Width = 100 };
            txtViewportCenterY = new TextBox() { Left = 120, Top = 100, Width = 100, Text = "300" };

            Label lblFileNameSuffix = new Label() { Text = "File Name Suffix:", Left = 10, Top = 140, Width = 100 };
            txtFileNameSuffix = new TextBox() { Left = 120, Top = 140, Width = 100, Text = "" };

            Label lblTotalPages = new Label() { Text = "Total Pages:", Left = 10, Top = 180, Width = 100 };
            txtTotalPages = new TextBox() { Left = 120, Top = 180, Width = 100, Text = "0" };

            Label lblDrawingTitle = new Label() { Text = "Drawing Title:", Left = 10, Top = 220, Width = 100 };
            txtDrawingTitle = new TextBox() { Left = 120, Top = 220, Width = 100, Text = "" };

            btnOK = new Button() { Text = "OK", Left = 50, Top = 260, Width = 80 };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button() { Text = "Cancel", Left = 150, Top = 260, Width = 80 };
            btnCancel.Click += BtnCancel_Click;

            this.Controls.Add(lblCustomScale);
            this.Controls.Add(txtCustomScale);
            this.Controls.Add(lblViewportCenterX);
            this.Controls.Add(txtViewportCenterX);
            this.Controls.Add(lblViewportCenterY);
            this.Controls.Add(txtViewportCenterY);
            this.Controls.Add(lblFileNameSuffix);
            this.Controls.Add(txtFileNameSuffix);
            this.Controls.Add(lblTotalPages);
            this.Controls.Add(txtTotalPages);
            this.Controls.Add(lblDrawingTitle);
            this.Controls.Add(txtDrawingTitle);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (double.TryParse(txtCustomScale.Text, out double scale))
            {
                CustomScale = scale;
            }
            else
            {
                MessageBox.Show("Invalid custom scale value.");
                return;
            }

            if (double.TryParse(txtViewportCenterX.Text, out double centerX) &&
                double.TryParse(txtViewportCenterY.Text, out double centerY))
            {
                ViewportCenter = new Point3d(centerX, centerY, 0);
            }
            else
            {
                MessageBox.Show("Invalid viewport center values.");
                return;
            }

            FileNameSuffix = txtFileNameSuffix.Text;

            if (int.TryParse(txtTotalPages.Text, out int pages))
            {
                TotalPages = pages;
            }
            else
            {
                TotalPages = 0; // or handle invalid input
            }
            DrawingTitle = txtDrawingTitle.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
