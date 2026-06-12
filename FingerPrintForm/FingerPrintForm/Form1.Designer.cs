using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace FingerPrintForm
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            Timer1 = new System.Windows.Forms.Timer(components);
            OracleCommandBuilder1 = new Oracle.ManagedDataAccess.Client.OracleCommandBuilder();
            mainLayout = new TableLayoutPanel();
            panelHeader = new Panel();
            lblRecordCount = new Label();
            lblStatus = new Label();
            lblSubtitle = new Label();
            lblTitle = new Label();
            panelFilters = new TableLayoutPanel();
            grpConnection = new GroupBox();
            lblPort = new Label();
            lblIp = new Label();
            btnDisconnect = new Button();
            btnConnect = new Button();
            txtPort = new TextBox();
            txtIP = new TextBox();
            grpDateRange = new GroupBox();
            lblToDate = new Label();
            lblFromDate = new Label();
            dtpToDate = new DateTimePicker();
            dtpFromDate = new DateTimePicker();
            grpActions = new GroupBox();
            Button1 = new Button();
            btnGetAttendance = new Button();
            btnGetUsers = new Button();
            btnExport = new Button();
            grpAttendance = new GroupBox();
            dgvAttendance = new DataGridView();
            grpLog = new GroupBox();
            txtLog = new RichTextBox();
            lblFooter = new Label();
            mainLayout.SuspendLayout();
            panelHeader.SuspendLayout();
            panelFilters.SuspendLayout();
            grpConnection.SuspendLayout();
            grpDateRange.SuspendLayout();
            grpActions.SuspendLayout();
            grpAttendance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAttendance).BeginInit();
            grpLog.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.BackColor = Color.FromArgb(244, 247, 251);
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(panelHeader, 0, 0);
            mainLayout.Controls.Add(panelFilters, 0, 1);
            mainLayout.Controls.Add(grpAttendance, 0, 2);
            mainLayout.Controls.Add(grpLog, 0, 3);
            mainLayout.Controls.Add(lblFooter, 0, 4);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(18);
            mainLayout.RowCount = 5;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 101F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 191F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 184F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            mainLayout.Size = new Size(1219, 779);
            mainLayout.TabIndex = 0;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.BorderStyle = BorderStyle.FixedSingle;
            panelHeader.Controls.Add(lblRecordCount);
            panelHeader.Controls.Add(lblStatus);
            panelHeader.Controls.Add(lblSubtitle);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Dock = DockStyle.Fill;
            panelHeader.Location = new Point(18, 18);
            panelHeader.Margin = new Padding(0, 0, 0, 12);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(20, 12, 20, 12);
            panelHeader.Size = new Size(1183, 89);
            panelHeader.TabIndex = 0;
            // 
            // lblRecordCount
            // 
            lblRecordCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblRecordCount.BackColor = Color.FromArgb(239, 246, 255);
            lblRecordCount.BorderStyle = BorderStyle.Fixed3D;
            lblRecordCount.Font = new System.Drawing.Font("Segoe UI", 11F);
            lblRecordCount.ForeColor = Color.FromArgb(29, 78, 216);
            lblRecordCount.Location = new Point(423, 28);
            lblRecordCount.Name = "lblRecordCount";
            lblRecordCount.Size = new Size(214, 37);
            lblRecordCount.TabIndex = 3;
            lblRecordCount.Text = "Records: 0";
            lblRecordCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatus.BackColor = Color.FromArgb(239, 246, 255);
            lblStatus.BorderStyle = BorderStyle.Fixed3D;
            lblStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(29, 78, 216);
            lblStatus.Location = new Point(666, 28);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(487, 37);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "Status: Ready";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 11F);
            lblSubtitle.ForeColor = Color.FromArgb(71, 85, 105);
            lblSubtitle.Location = new Point(17, 50);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(366, 25);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Device attendance reader and Oracle sync";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblTitle.Location = new Point(11, 4);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(406, 41);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Fingerprint Attendance Sync";
            // 
            // panelFilters
            // 
            panelFilters.ColumnCount = 3;
            panelFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            panelFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            panelFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            panelFilters.Controls.Add(grpConnection, 0, 0);
            panelFilters.Controls.Add(grpDateRange, 1, 0);
            panelFilters.Controls.Add(grpActions, 2, 0);
            panelFilters.Dock = DockStyle.Fill;
            panelFilters.Location = new Point(18, 119);
            panelFilters.Margin = new Padding(0, 0, 0, 12);
            panelFilters.Name = "panelFilters";
            panelFilters.RowCount = 1;
            panelFilters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panelFilters.Size = new Size(1183, 179);
            panelFilters.TabIndex = 1;
            // 
            // grpConnection
            // 
            grpConnection.BackColor = Color.Transparent;
            grpConnection.Controls.Add(lblPort);
            grpConnection.Controls.Add(lblIp);
            grpConnection.Controls.Add(btnDisconnect);
            grpConnection.Controls.Add(btnConnect);
            grpConnection.Controls.Add(txtPort);
            grpConnection.Controls.Add(txtIP);
            grpConnection.Dock = DockStyle.Fill;
            grpConnection.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            grpConnection.ForeColor = Color.FromArgb(30, 41, 59);
            grpConnection.Location = new Point(0, 0);
            grpConnection.Margin = new Padding(0, 0, 12, 0);
            grpConnection.Name = "grpConnection";
            grpConnection.Padding = new Padding(14, 12, 14, 14);
            grpConnection.Size = new Size(390, 179);
            grpConnection.TabIndex = 0;
            grpConnection.TabStop = false;
            grpConnection.Text = "Device Connection";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Font = new System.Drawing.Font("Segoe UI", 11F);
            lblPort.ForeColor = Color.Black;
            lblPort.Location = new Point(188, 30);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(46, 25);
            lblPort.TabIndex = 5;
            lblPort.Text = "Port";
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Font = new System.Drawing.Font("Segoe UI", 11F);
            lblIp.ForeColor = Color.Black;
            lblIp.Location = new Point(17, 30);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(89, 25);
            lblIp.TabIndex = 4;
            lblIp.Text = "Device IP";
            // 
            // btnDisconnect
            // 
            btnDisconnect.BackColor = Color.Gray;
            btnDisconnect.Enabled = false;
            btnDisconnect.FlatAppearance.BorderSize = 0;
            btnDisconnect.FlatStyle = FlatStyle.Flat;
            btnDisconnect.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnDisconnect.ForeColor = Color.FromArgb(30, 41, 59);
            btnDisconnect.Image = Properties.Resources.disconnect__1__2;
            btnDisconnect.ImageAlign = ContentAlignment.MiddleLeft;
            btnDisconnect.Location = new Point(188, 111);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Padding = new Padding(8, 0, 8, 0);
            btnDisconnect.Size = new Size(167, 61);
            btnDisconnect.TabIndex = 3;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnDisconnect.UseVisualStyleBackColor = false;
            // 
            // btnConnect
            // 
            btnConnect.BackColor = Color.RoyalBlue;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnConnect.ForeColor = Color.Black;
            btnConnect.Image = Properties.Resources.disconnect_1__21;
            btnConnect.ImageAlign = ContentAlignment.MiddleLeft;
            btnConnect.Location = new Point(18, 111);
            btnConnect.Name = "btnConnect";
            btnConnect.Padding = new Padding(8, 0, 8, 0);
            btnConnect.Size = new Size(153, 61);
            btnConnect.TabIndex = 2;
            btnConnect.Text = "Connect";
            btnConnect.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnConnect.UseVisualStyleBackColor = false;
            // 
            // txtPort
            // 
            txtPort.BackColor = Color.White;
            txtPort.BorderStyle = BorderStyle.FixedSingle;
            txtPort.Font = new System.Drawing.Font("Segoe UI", 12F);
            txtPort.ForeColor = Color.Black;
            txtPort.Location = new Point(188, 59);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(167, 34);
            txtPort.TabIndex = 1;
            txtPort.Text = "4370";
            txtPort.TextAlign = HorizontalAlignment.Center;
            // 
            // txtIP
            // 
            txtIP.BackColor = Color.White;
            txtIP.BorderStyle = BorderStyle.FixedSingle;
            txtIP.Font = new System.Drawing.Font("Segoe UI", 12F);
            txtIP.ForeColor = Color.Black;
            txtIP.Location = new Point(18, 59);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(153, 34);
            txtIP.TabIndex = 0;
            txtIP.Text = "192.168.88.101";
            txtIP.TextAlign = HorizontalAlignment.Center;
            // 
            // grpDateRange
            // 
            grpDateRange.BackColor = Color.Transparent;
            grpDateRange.Controls.Add(lblToDate);
            grpDateRange.Controls.Add(lblFromDate);
            grpDateRange.Controls.Add(dtpToDate);
            grpDateRange.Controls.Add(dtpFromDate);
            grpDateRange.Dock = DockStyle.Fill;
            grpDateRange.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            grpDateRange.ForeColor = Color.FromArgb(30, 41, 59);
            grpDateRange.Location = new Point(402, 0);
            grpDateRange.Margin = new Padding(0, 0, 12, 0);
            grpDateRange.Name = "grpDateRange";
            grpDateRange.Padding = new Padding(14, 12, 14, 14);
            grpDateRange.Size = new Size(390, 179);
            grpDateRange.TabIndex = 1;
            grpDateRange.TabStop = false;
            grpDateRange.Text = "Date Range";
            // 
            // lblToDate
            // 
            lblToDate.AutoSize = true;
            lblToDate.Font = new System.Drawing.Font("Segoe UI", 13F);
            lblToDate.ForeColor = Color.Black;
            lblToDate.Location = new Point(192, 46);
            lblToDate.Name = "lblToDate";
            lblToDate.Size = new Size(87, 30);
            lblToDate.TabIndex = 3;
            lblToDate.Text = "To Date";
            // 
            // lblFromDate
            // 
            lblFromDate.AutoSize = true;
            lblFromDate.Font = new System.Drawing.Font("Segoe UI", 13F);
            lblFromDate.ForeColor = Color.Black;
            lblFromDate.Location = new Point(18, 46);
            lblFromDate.Name = "lblFromDate";
            lblFromDate.Size = new Size(115, 30);
            lblFromDate.TabIndex = 2;
            lblFromDate.Text = "From Date";
            // 
            // dtpToDate
            // 
            dtpToDate.Font = new System.Drawing.Font("Segoe UI", 13F);
            dtpToDate.Format = DateTimePickerFormat.Short;
            dtpToDate.Location = new Point(192, 88);
            dtpToDate.Name = "dtpToDate";
            dtpToDate.Size = new Size(156, 36);
            dtpToDate.TabIndex = 1;
            // 
            // dtpFromDate
            // 
            dtpFromDate.Font = new System.Drawing.Font("Segoe UI", 13F);
            dtpFromDate.Format = DateTimePickerFormat.Short;
            dtpFromDate.Location = new Point(18, 88);
            dtpFromDate.Name = "dtpFromDate";
            dtpFromDate.Size = new Size(156, 36);
            dtpFromDate.TabIndex = 0;
            // 
            // grpActions
            // 
            grpActions.BackColor = Color.Transparent;
            grpActions.Controls.Add(Button1);
            grpActions.Controls.Add(btnGetAttendance);
            grpActions.Controls.Add(btnGetUsers);
            grpActions.Controls.Add(btnExport);
            grpActions.Dock = DockStyle.Fill;
            grpActions.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            grpActions.ForeColor = Color.FromArgb(30, 41, 59);
            grpActions.Location = new Point(804, 0);
            grpActions.Margin = new Padding(0);
            grpActions.Name = "grpActions";
            grpActions.Padding = new Padding(14, 12, 14, 14);
            grpActions.Size = new Size(379, 179);
            grpActions.TabIndex = 2;
            grpActions.TabStop = false;
            grpActions.Text = "Actions";
            // 
            // Button1
            // 
            Button1.BackColor = Color.Lime;
            Button1.Enabled = false;
            Button1.FlatAppearance.BorderSize = 0;
            Button1.FlatStyle = FlatStyle.Flat;
            Button1.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            Button1.ForeColor = Color.Black;
            Button1.Image = Properties.Resources.insert_db6;
            Button1.ImageAlign = ContentAlignment.MiddleLeft;
            Button1.Location = new Point(18, 30);
            Button1.Name = "Button1";
            Button1.Padding = new Padding(8, 0, 8, 0);
            Button1.Size = new Size(150, 63);
            Button1.TabIndex = 0;
            Button1.Text = "Insert to DB";
            Button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            Button1.UseVisualStyleBackColor = false;
            // 
            // btnGetAttendance
            // 
            btnGetAttendance.BackColor = Color.Cyan;
            btnGetAttendance.Enabled = false;
            btnGetAttendance.FlatAppearance.BorderSize = 0;
            btnGetAttendance.FlatStyle = FlatStyle.Flat;
            btnGetAttendance.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnGetAttendance.ForeColor = Color.Black;
            btnGetAttendance.Image = Properties.Resources.load_attendance6;
            btnGetAttendance.ImageAlign = ContentAlignment.MiddleLeft;
            btnGetAttendance.Location = new Point(180, 30);
            btnGetAttendance.Name = "btnGetAttendance";
            btnGetAttendance.Padding = new Padding(8, 0, 8, 0);
            btnGetAttendance.Size = new Size(170, 63);
            btnGetAttendance.TabIndex = 1;
            btnGetAttendance.Text = "Load Attendance";
            btnGetAttendance.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnGetAttendance.UseVisualStyleBackColor = false;
            // 
            // btnGetUsers
            // 
            btnGetUsers.BackColor = Color.LightSkyBlue;
            btnGetUsers.Enabled = false;
            btnGetUsers.FlatAppearance.BorderSize = 0;
            btnGetUsers.FlatStyle = FlatStyle.Flat;
            btnGetUsers.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnGetUsers.ForeColor = Color.Black;
            btnGetUsers.Image = Properties.Resources.load_users6;
            btnGetUsers.ImageAlign = ContentAlignment.MiddleLeft;
            btnGetUsers.Location = new Point(18, 99);
            btnGetUsers.Name = "btnGetUsers";
            btnGetUsers.Padding = new Padding(8, 0, 8, 0);
            btnGetUsers.Size = new Size(150, 61);
            btnGetUsers.TabIndex = 2;
            btnGetUsers.Text = "Load Users";
            btnGetUsers.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnGetUsers.UseVisualStyleBackColor = false;
            // 
            // btnExport
            // 
            btnExport.BackColor = Color.DarkOrange;
            btnExport.Enabled = false;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnExport.ForeColor = Color.Black;
            btnExport.Image = Properties.Resources.export_csv6;
            btnExport.ImageAlign = ContentAlignment.MiddleLeft;
            btnExport.Location = new Point(180, 99);
            btnExport.Name = "btnExport";
            btnExport.Padding = new Padding(8, 0, 8, 0);
            btnExport.Size = new Size(170, 61);
            btnExport.TabIndex = 3;
            btnExport.Text = "Export CSV";
            btnExport.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnExport.UseVisualStyleBackColor = false;
            // 
            // grpAttendance
            // 
            grpAttendance.BackColor = Color.White;
            grpAttendance.Controls.Add(dgvAttendance);
            grpAttendance.Dock = DockStyle.Fill;
            grpAttendance.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            grpAttendance.ForeColor = Color.FromArgb(30, 41, 59);
            grpAttendance.Location = new Point(18, 310);
            grpAttendance.Margin = new Padding(0, 0, 0, 12);
            grpAttendance.Name = "grpAttendance";
            grpAttendance.Padding = new Padding(12, 28, 12, 12);
            grpAttendance.Size = new Size(1183, 225);
            grpAttendance.TabIndex = 2;
            grpAttendance.TabStop = false;
            grpAttendance.Text = "Attendance Records";
            // 
            // dgvAttendance
            // 
            dgvAttendance.AllowUserToAddRows = false;
            dgvAttendance.AllowUserToDeleteRows = false;
            dgvAttendance.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 250, 252);
            dgvAttendance.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dgvAttendance.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAttendance.BackgroundColor = Color.White;
            dgvAttendance.BorderStyle = BorderStyle.None;
            dgvAttendance.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvAttendance.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(15, 23, 42);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = Color.White;
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dataGridViewCellStyle2.SelectionForeColor = Color.White;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dgvAttendance.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dgvAttendance.ColumnHeadersHeight = 38;
            dgvAttendance.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.White;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = Color.FromArgb(30, 41, 59);
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dgvAttendance.DefaultCellStyle = dataGridViewCellStyle3;
            dgvAttendance.Dock = DockStyle.Fill;
            dgvAttendance.EnableHeadersVisualStyles = false;
            dgvAttendance.GridColor = Color.FromArgb(226, 232, 240);
            dgvAttendance.Location = new Point(12, 51);
            dgvAttendance.MultiSelect = false;
            dgvAttendance.Name = "dgvAttendance";
            dgvAttendance.ReadOnly = true;
            dgvAttendance.RowHeadersVisible = false;
            dgvAttendance.RowHeadersWidth = 51;
            dgvAttendance.RowTemplate.Height = 30;
            dgvAttendance.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAttendance.Size = new Size(1159, 162);
            dgvAttendance.TabIndex = 0;
            // 
            // grpLog
            // 
            grpLog.BackColor = Color.White;
            grpLog.Controls.Add(txtLog);
            grpLog.Dock = DockStyle.Fill;
            grpLog.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            grpLog.ForeColor = Color.FromArgb(30, 41, 59);
            grpLog.Location = new Point(18, 547);
            grpLog.Margin = new Padding(0);
            grpLog.Name = "grpLog";
            grpLog.Padding = new Padding(12, 28, 12, 12);
            grpLog.Size = new Size(1183, 184);
            grpLog.TabIndex = 3;
            grpLog.TabStop = false;
            grpLog.Text = "Activity Log";
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.FromArgb(15, 23, 42);
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new System.Drawing.Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtLog.ForeColor = Color.White;
            txtLog.Location = new Point(12, 51);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
            txtLog.Size = new Size(1159, 121);
            txtLog.TabIndex = 0;
            txtLog.Text = "";
            txtLog.WordWrap = false;
            // 
            // lblFooter
            // 
            lblFooter.Dock = DockStyle.Fill;
            lblFooter.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblFooter.ForeColor = Color.Black;
            lblFooter.Location = new Point(18, 731);
            lblFooter.Margin = new Padding(0);
            lblFooter.Name = "lblFooter";
            lblFooter.Size = new Size(1183, 30);
            lblFooter.TabIndex = 4;
            lblFooter.Text = "Copyright © 2012 Beta Solution, Bangladesh. All rights reserved.";
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 247, 251);
            ClientSize = new Size(1219, 779);
            Controls.Add(mainLayout);
            Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1060, 680);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Fingerprint Attendance Sync              ";
            mainLayout.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelFilters.ResumeLayout(false);
            grpConnection.ResumeLayout(false);
            grpConnection.PerformLayout();
            grpDateRange.ResumeLayout(false);
            grpDateRange.PerformLayout();
            grpActions.ResumeLayout(false);
            grpAttendance.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvAttendance).EndInit();
            grpLog.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer Timer1;
        private Oracle.ManagedDataAccess.Client.OracleCommandBuilder OracleCommandBuilder1;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.TableLayoutPanel panelFilters;
        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Button Button1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.DateTimePicker dtpFromDate;
        private System.Windows.Forms.DateTimePicker dtpToDate;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label lblRecordCount;
        private System.Windows.Forms.Button btnGetUsers;
        private System.Windows.Forms.Button btnGetAttendance;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.DataGridView dgvAttendance;
        private System.Windows.Forms.GroupBox grpDateRange;
        private System.Windows.Forms.Label lblFromDate;
        private System.Windows.Forms.Label lblToDate;
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.GroupBox grpAttendance;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.Label lblFooter;
    }
}
