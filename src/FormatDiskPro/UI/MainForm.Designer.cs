namespace FormatDiskPro
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip = new MenuStrip();
            mnuTools = new ToolStripMenuItem();
            mnuVerify = new ToolStripMenuItem();
            mnuEject = new ToolStripMenuItem();
            mnuHistory = new ToolStripMenuItem();
            mnuConfig = new ToolStripMenuItem();
            mnuLang = new ToolStripMenuItem();
            mnuLangEs = new ToolStripMenuItem();
            mnuLangEn = new ToolStripMenuItem();
            mnuTheme = new ToolStripMenuItem();
            mnuThemeLight = new ToolStripMenuItem();
            mnuThemeDark = new ToolStripMenuItem();
            mnuPresets = new ToolStripMenuItem();
            mnuHelp = new ToolStripMenuItem();
            mnuAbout = new ToolStripMenuItem();
            pnlHeader = new Panel();
            lblHeaderTitle = new Label();
            lblHeaderSub = new Label();
            lblDrive = new Label();
            cboDrive = new ComboBox();
            btnRefresh = new Button();
            pnlInfo = new Panel();
            lblInfoTotal = new Label();
            lblInfoFree = new Label();
            lblInfoFs = new Label();
            lblInfoType = new Label();
            lblInfoHealth = new Label();
            lblInfoBus = new Label();
            lblFileSystem = new Label();
            cboFileSystem = new ComboBox();
            lblFsDesc = new Label();
            lblAllocUnit = new Label();
            cboAllocUnit = new ComboBox();
            btnRestore = new Button();
            lblVolumeLabel = new Label();
            txtLabel = new TextBox();
            grpOptions = new GroupBox();
            chkQuickFormat = new CheckBox();
            chkCompress = new CheckBox();
            chkSecureWipe = new CheckBox();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            lblElapsed = new Label();
            btnStart = new Button();
            btnClose = new Button();
            timerElapsed = new System.Windows.Forms.Timer(components);
            toolTip = new ToolTip(components);
            menuStrip.SuspendLayout();
            pnlHeader.SuspendLayout();
            pnlInfo.SuspendLayout();
            grpOptions.SuspendLayout();
            SuspendLayout();
            //
            // menuStrip
            //
            menuStrip.Items.AddRange(new ToolStripItem[] { mnuTools, mnuConfig, mnuHelp });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(457, 24);
            menuStrip.TabIndex = 0;
            //
            // mnuTools
            //
            mnuTools.DropDownItems.AddRange(new ToolStripItem[] { mnuVerify, mnuEject, new ToolStripSeparator(), mnuHistory });
            mnuTools.Name = "mnuTools";
            mnuTools.Size = new Size(86, 20);
            mnuTools.Text = "Herramientas";
            //
            // mnuVerify
            //
            mnuVerify.Name = "mnuVerify";
            mnuVerify.Size = new Size(200, 22);
            mnuVerify.Text = "Verificar capacidad real…";
            mnuVerify.Click += mnuVerify_Click;
            //
            // mnuEject
            //
            mnuEject.Name = "mnuEject";
            mnuEject.Size = new Size(200, 22);
            mnuEject.Text = "Expulsar unidad";
            mnuEject.Click += mnuEject_Click;
            //
            // mnuHistory
            //
            mnuHistory.Name = "mnuHistory";
            mnuHistory.Size = new Size(200, 22);
            mnuHistory.Text = "Ver historial";
            mnuHistory.Click += mnuHistory_Click;
            //
            // mnuConfig
            //
            mnuConfig.DropDownItems.AddRange(new ToolStripItem[] { mnuLang, mnuTheme, mnuPresets });
            mnuConfig.Name = "mnuConfig";
            mnuConfig.Size = new Size(95, 20);
            mnuConfig.Text = "Configuración";
            //
            // mnuLang
            //
            mnuLang.DropDownItems.AddRange(new ToolStripItem[] { mnuLangEs, mnuLangEn });
            mnuLang.Name = "mnuLang";
            mnuLang.Size = new Size(180, 22);
            mnuLang.Text = "Idioma";
            //
            // mnuLangEs
            //
            mnuLangEs.CheckOnClick = true;
            mnuLangEs.Checked = true;
            mnuLangEs.Name = "mnuLangEs";
            mnuLangEs.Size = new Size(180, 22);
            mnuLangEs.Text = "Español";
            mnuLangEs.Click += mnuLangEs_Click;
            //
            // mnuLangEn
            //
            mnuLangEn.CheckOnClick = true;
            mnuLangEn.Name = "mnuLangEn";
            mnuLangEn.Size = new Size(180, 22);
            mnuLangEn.Text = "Inglés";
            mnuLangEn.Click += mnuLangEn_Click;
            //
            // mnuTheme
            //
            mnuTheme.DropDownItems.AddRange(new ToolStripItem[] { mnuThemeLight, mnuThemeDark });
            mnuTheme.Name = "mnuTheme";
            mnuTheme.Size = new Size(180, 22);
            mnuTheme.Text = "Tema";
            //
            // mnuThemeLight
            //
            mnuThemeLight.CheckOnClick = true;
            mnuThemeLight.Checked = true;
            mnuThemeLight.Name = "mnuThemeLight";
            mnuThemeLight.Size = new Size(180, 22);
            mnuThemeLight.Text = "Claro";
            mnuThemeLight.Click += mnuThemeLight_Click;
            //
            // mnuThemeDark
            //
            mnuThemeDark.CheckOnClick = true;
            mnuThemeDark.Name = "mnuThemeDark";
            mnuThemeDark.Size = new Size(180, 22);
            mnuThemeDark.Text = "Oscuro";
            mnuThemeDark.Click += mnuThemeDark_Click;
            //
            // mnuPresets
            //
            mnuPresets.Name = "mnuPresets";
            mnuPresets.Size = new Size(180, 22);
            mnuPresets.Text = "Presets";
            //
            // mnuHelp
            //
            mnuHelp.DropDownItems.AddRange(new ToolStripItem[] { mnuAbout });
            mnuHelp.Name = "mnuHelp";
            mnuHelp.Size = new Size(53, 20);
            mnuHelp.Text = "Ayuda";
            //
            // mnuAbout
            //
            mnuAbout.Name = "mnuAbout";
            mnuAbout.Size = new Size(180, 22);
            mnuAbout.Text = "Acerca de…";
            mnuAbout.Click += mnuAbout_Click;
            //
            // pnlHeader
            //
            pnlHeader.BackColor = Color.FromArgb(0, 110, 180);
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);
            pnlHeader.Location = new Point(0, 24);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(457, 69);
            pnlHeader.TabIndex = 1;
            //
            // lblHeaderTitle
            //
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.BackColor = Color.FromArgb(0, 110, 180);
            lblHeaderTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(14, 9);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(146, 25);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "FormatDiskPro";
            //
            // lblHeaderSub
            //
            lblHeaderSub.BackColor = Color.FromArgb(0, 110, 180);
            lblHeaderSub.Font = new Font("Segoe UI", 9F);
            lblHeaderSub.ForeColor = Color.FromArgb(190, 225, 255);
            lblHeaderSub.Location = new Point(14, 40);
            lblHeaderSub.Name = "lblHeaderSub";
            lblHeaderSub.Size = new Size(430, 21);
            lblHeaderSub.TabIndex = 1;
            lblHeaderSub.Text = "Seleccione una unidad para formatear";
            //
            // lblDrive
            //
            lblDrive.AutoSize = true;
            lblDrive.Location = new Point(14, 104);
            lblDrive.Name = "lblDrive";
            lblDrive.Size = new Size(60, 20);
            lblDrive.TabIndex = 2;
            lblDrive.Text = "Unidad:";
            //
            // cboDrive
            //
            cboDrive.DrawMode = DrawMode.OwnerDrawFixed;
            cboDrive.DropDownStyle = ComboBoxStyle.DropDownList;
            cboDrive.Location = new Point(14, 128);
            cboDrive.Margin = new Padding(3, 4, 3, 4);
            cboDrive.Name = "cboDrive";
            cboDrive.Size = new Size(397, 28);
            cboDrive.TabIndex = 3;
            cboDrive.DrawItem += cboDrive_DrawItem;
            cboDrive.SelectedIndexChanged += cboDrive_SelectedIndexChanged;
            //
            // btnRefresh
            //
            btnRefresh.Location = new Point(416, 128);
            btnRefresh.Margin = new Padding(3, 4, 3, 4);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(27, 31);
            btnRefresh.TabIndex = 4;
            btnRefresh.Text = "↻";
            toolTip.SetToolTip(btnRefresh, "Actualizar lista de unidades");
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            //
            // pnlInfo
            //
            pnlInfo.BackColor = Color.FromArgb(242, 248, 255);
            pnlInfo.BorderStyle = BorderStyle.FixedSingle;
            pnlInfo.Controls.Add(lblInfoTotal);
            pnlInfo.Controls.Add(lblInfoFree);
            pnlInfo.Controls.Add(lblInfoFs);
            pnlInfo.Controls.Add(lblInfoType);
            pnlInfo.Controls.Add(lblInfoHealth);
            pnlInfo.Controls.Add(lblInfoBus);
            pnlInfo.Location = new Point(14, 171);
            pnlInfo.Name = "pnlInfo";
            pnlInfo.Size = new Size(429, 87);
            pnlInfo.TabIndex = 5;
            //
            // lblInfoTotal
            //
            lblInfoTotal.AutoSize = true;
            lblInfoTotal.Location = new Point(9, 7);
            lblInfoTotal.Name = "lblInfoTotal";
            lblInfoTotal.Size = new Size(57, 20);
            lblInfoTotal.TabIndex = 0;
            lblInfoTotal.Text = "Total: –";
            //
            // lblInfoFree
            //
            lblInfoFree.AutoSize = true;
            lblInfoFree.Location = new Point(9, 33);
            lblInfoFree.Name = "lblInfoFree";
            lblInfoFree.Size = new Size(57, 20);
            lblInfoFree.TabIndex = 1;
            lblInfoFree.Text = "Libre: –";
            //
            // lblInfoFs
            //
            lblInfoFs.AutoSize = true;
            lblInfoFs.Location = new Point(224, 7);
            lblInfoFs.Name = "lblInfoFs";
            lblInfoFs.Size = new Size(120, 20);
            lblInfoFs.TabIndex = 2;
            lblInfoFs.Text = "Sistema actual: –";
            //
            // lblInfoType
            //
            lblInfoType.AutoSize = true;
            lblInfoType.Location = new Point(224, 33);
            lblInfoType.Name = "lblInfoType";
            lblInfoType.Size = new Size(54, 20);
            lblInfoType.TabIndex = 3;
            lblInfoType.Text = "Tipo: –";
            //
            // lblInfoHealth
            //
            lblInfoHealth.AutoSize = true;
            lblInfoHealth.Location = new Point(9, 59);
            lblInfoHealth.Name = "lblInfoHealth";
            lblInfoHealth.Size = new Size(60, 20);
            lblInfoHealth.TabIndex = 4;
            lblInfoHealth.Text = "Salud: –";
            //
            // lblInfoBus
            //
            lblInfoBus.AutoSize = true;
            lblInfoBus.Location = new Point(224, 59);
            lblInfoBus.Name = "lblInfoBus";
            lblInfoBus.Size = new Size(80, 20);
            lblInfoBus.TabIndex = 5;
            lblInfoBus.Text = "Conexión: –";
            //
            // lblFileSystem
            //
            lblFileSystem.AutoSize = true;
            lblFileSystem.Location = new Point(14, 268);
            lblFileSystem.Name = "lblFileSystem";
            lblFileSystem.Size = new Size(140, 20);
            lblFileSystem.TabIndex = 6;
            lblFileSystem.Text = "Sistema de archivos";
            //
            // cboFileSystem
            //
            cboFileSystem.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFileSystem.Location = new Point(14, 292);
            cboFileSystem.Margin = new Padding(3, 4, 3, 4);
            cboFileSystem.Name = "cboFileSystem";
            cboFileSystem.Size = new Size(429, 28);
            cboFileSystem.TabIndex = 7;
            cboFileSystem.SelectedIndexChanged += cboFileSystem_SelectedIndexChanged;
            //
            // lblFsDesc
            //
            lblFsDesc.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblFsDesc.ForeColor = Color.DimGray;
            lblFsDesc.Location = new Point(16, 327);
            lblFsDesc.Name = "lblFsDesc";
            lblFsDesc.Size = new Size(430, 43);
            lblFsDesc.TabIndex = 8;
            //
            // lblAllocUnit
            //
            lblAllocUnit.AutoSize = true;
            lblAllocUnit.Location = new Point(14, 379);
            lblAllocUnit.Name = "lblAllocUnit";
            lblAllocUnit.Size = new Size(228, 20);
            lblAllocUnit.TabIndex = 9;
            lblAllocUnit.Text = "Tamaño de unidad de asignación";
            //
            // cboAllocUnit
            //
            cboAllocUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAllocUnit.Location = new Point(14, 403);
            cboAllocUnit.Margin = new Padding(3, 4, 3, 4);
            cboAllocUnit.Name = "cboAllocUnit";
            cboAllocUnit.Size = new Size(429, 28);
            cboAllocUnit.TabIndex = 10;
            //
            // btnRestore
            //
            btnRestore.Location = new Point(14, 445);
            btnRestore.Margin = new Padding(3, 4, 3, 4);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(430, 36);
            btnRestore.TabIndex = 11;
            btnRestore.Text = "Restaurar valores predeterminados";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            //
            // lblVolumeLabel
            //
            lblVolumeLabel.AutoSize = true;
            lblVolumeLabel.Location = new Point(14, 493);
            lblVolumeLabel.Name = "lblVolumeLabel";
            lblVolumeLabel.Size = new Size(153, 20);
            lblVolumeLabel.TabIndex = 12;
            lblVolumeLabel.Text = "Etiqueta del volumen:";
            //
            // txtLabel
            //
            txtLabel.Location = new Point(14, 517);
            txtLabel.Margin = new Padding(3, 4, 3, 4);
            txtLabel.MaxLength = 32;
            txtLabel.Name = "txtLabel";
            txtLabel.Size = new Size(429, 27);
            txtLabel.TabIndex = 13;
            //
            // grpOptions
            //
            grpOptions.Controls.Add(chkQuickFormat);
            grpOptions.Controls.Add(chkCompress);
            grpOptions.Controls.Add(chkSecureWipe);
            grpOptions.Location = new Point(14, 560);
            grpOptions.Name = "grpOptions";
            grpOptions.Size = new Size(430, 131);
            grpOptions.TabIndex = 14;
            grpOptions.TabStop = false;
            grpOptions.Text = "Opciones de formato";
            //
            // chkQuickFormat
            //
            chkQuickFormat.AutoSize = true;
            chkQuickFormat.Checked = true;
            chkQuickFormat.CheckState = CheckState.Checked;
            chkQuickFormat.Location = new Point(14, 29);
            chkQuickFormat.Name = "chkQuickFormat";
            chkQuickFormat.Size = new Size(135, 24);
            chkQuickFormat.TabIndex = 0;
            chkQuickFormat.Text = "Formato rápido";
            //
            // chkCompress
            //
            chkCompress.AutoSize = true;
            chkCompress.Enabled = false;
            chkCompress.Location = new Point(14, 61);
            chkCompress.Name = "chkCompress";
            chkCompress.Size = new Size(251, 24);
            chkCompress.TabIndex = 1;
            chkCompress.Text = "Habilitar compresión (sólo NTFS)";
            //
            // chkSecureWipe
            //
            chkSecureWipe.AutoSize = true;
            chkSecureWipe.Location = new Point(14, 93);
            chkSecureWipe.Name = "chkSecureWipe";
            chkSecureWipe.Size = new Size(280, 24);
            chkSecureWipe.TabIndex = 2;
            chkSecureWipe.Text = "Borrado seguro (sobrescribir espacio libre)";
            //
            // progressBar
            //
            progressBar.Location = new Point(14, 703);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(430, 29);
            progressBar.TabIndex = 15;
            //
            // lblStatus
            //
            lblStatus.Location = new Point(14, 742);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(429, 24);
            lblStatus.TabIndex = 16;
            //
            // lblElapsed
            //
            lblElapsed.ForeColor = Color.DimGray;
            lblElapsed.Location = new Point(336, 742);
            lblElapsed.Name = "lblElapsed";
            lblElapsed.Size = new Size(107, 24);
            lblElapsed.TabIndex = 17;
            lblElapsed.TextAlign = ContentAlignment.MiddleRight;
            //
            // btnStart
            //
            btnStart.Location = new Point(129, 776);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(91, 36);
            btnStart.TabIndex = 18;
            btnStart.Text = "Iniciar";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            //
            // btnClose
            //
            btnClose.Location = new Point(237, 776);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(91, 36);
            btnClose.TabIndex = 19;
            btnClose.Text = "Cerrar";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            //
            // timerElapsed
            //
            timerElapsed.Interval = 1000;
            timerElapsed.Tick += timerElapsed_Tick;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(457, 824);
            Controls.Add(pnlHeader);
            Controls.Add(lblDrive);
            Controls.Add(cboDrive);
            Controls.Add(btnRefresh);
            Controls.Add(pnlInfo);
            Controls.Add(lblFileSystem);
            Controls.Add(cboFileSystem);
            Controls.Add(lblFsDesc);
            Controls.Add(lblAllocUnit);
            Controls.Add(cboAllocUnit);
            Controls.Add(btnRestore);
            Controls.Add(lblVolumeLabel);
            Controls.Add(txtLabel);
            Controls.Add(grpOptions);
            Controls.Add(progressBar);
            Controls.Add(lblStatus);
            Controls.Add(lblElapsed);
            Controls.Add(btnStart);
            Controls.Add(btnClose);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            CancelButton = btnClose;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FormatDiskPro";
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlInfo.ResumeLayout(false);
            pnlInfo.PerformLayout();
            grpOptions.ResumeLayout(false);
            grpOptions.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip menuStrip;
        private ToolStripMenuItem mnuTools;
        private ToolStripMenuItem mnuVerify;
        private ToolStripMenuItem mnuEject;
        private ToolStripMenuItem mnuHistory;
        private ToolStripMenuItem mnuConfig;
        private ToolStripMenuItem mnuLang;
        private ToolStripMenuItem mnuLangEs;
        private ToolStripMenuItem mnuLangEn;
        private ToolStripMenuItem mnuTheme;
        private ToolStripMenuItem mnuThemeLight;
        private ToolStripMenuItem mnuThemeDark;
        private ToolStripMenuItem mnuPresets;
        private ToolStripMenuItem mnuHelp;
        private ToolStripMenuItem mnuAbout;
        private Panel pnlHeader;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Label lblDrive;
        private ComboBox cboDrive;
        private Button btnRefresh;
        private Panel pnlInfo;
        private Label lblInfoTotal;
        private Label lblInfoFree;
        private Label lblInfoFs;
        private Label lblInfoType;
        private Label lblInfoHealth;
        private Label lblInfoBus;
        private Label lblFileSystem;
        private ComboBox cboFileSystem;
        private Label lblFsDesc;
        private Label lblAllocUnit;
        private ComboBox cboAllocUnit;
        private Button btnRestore;
        private Label lblVolumeLabel;
        private TextBox txtLabel;
        private GroupBox grpOptions;
        private CheckBox chkQuickFormat;
        private CheckBox chkCompress;
        private CheckBox chkSecureWipe;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblElapsed;
        private Button btnStart;
        private Button btnClose;
        private System.Windows.Forms.Timer timerElapsed;
        private ToolTip toolTip;
    }
}
