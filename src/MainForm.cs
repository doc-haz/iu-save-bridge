using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace IUSaveBridge
{
    public class MainForm : Form
    {
        // Data & Session State
        private string m_recompRoot = null;
        private string m_activeRegion = SaveManager.RegionNtscU;
        private SavePayload m_currentPayload = null;
        private SaveSlotInfo m_activeSlot = null;
        private List<SaveSlotInfo> m_detectedSlots = new List<SaveSlotInfo>();
        private List<ItemData> m_allItems = null;
        private CharacterData m_selectedCharacter = null;
        private bool m_hasUnsavedChanges = false;

        // Visual Assets
        private Image m_menuArtwork = null;
        private Image m_appIconImage = null;

        // Header Controls
        private Panel m_pnlHeader;
        private PictureBox m_pbHeaderIcon;
        private Label m_lblHeaderTitle;
        private Label m_lblHeaderSubtitle;
        private Label m_lblActiveSaveBadge;
        private Label m_lblRegion;
        private ComboBox m_cbRegion;
        private Label m_lblLang;
        private ComboBox m_cbLanguage;

        // Tab Control
        private TabControl m_tabControl;
        private TabPage m_tabSaves;
        private TabPage m_tabCharacters;
        private TabPage m_tabInventory;
        private TabPage m_tabBackups;
        private TabPage m_tabSettings;

        public TabControl TabCtrl { get { return m_tabControl; } }
        public void SetLanguageDirect(string lang) { SwitchLanguage(lang); }
        public void SetRegionDirect(string reg) { SwitchRegion(reg); }

        // Saves Tab Controls
        private Panel m_pnlSavesCard;
        private Label m_lblGameLocationBanner;
        private ListView m_lvSlots;
        private PictureBox m_pbThumbnail;
        private Label m_lblSlotDetailTitle;
        private Label m_lblSlotDetailInfo;
        private GroupBox m_gbActiveSlot;
        private Label m_lblFol;
        private NumericUpDown m_numFol;
        private Button m_btnUpdateFol;
        private Button m_btnMaxFol;
        private Label m_lblChecksums;
        private Button m_btnOpenSave;
        private Button m_btnSaveChanges;
        private Button m_btnRefreshSaves;
        private Button m_btnOpenSaveFolder;
        private Button m_btnManualBackup;

        // Characters Tab Controls
        private Panel m_pnlCharsCard;
        private Label m_lblNoSaveChars;
        private GroupBox m_gbCharList;
        private ListBox m_lbCharacters;
        private Panel m_pnlCharEditor;
        private Label m_lblCharTitle;
        private CheckBox m_cbInParty;
        private CheckBox m_cbInActiveParty;
        private Label m_lblLevel;
        private Label m_lblExp;
        private Label m_lblCurHp;
        private Label m_lblMaxHp;
        private Label m_lblCurMp;
        private Label m_lblMaxMp;
        private Label m_lblAtk;
        private Label m_lblDef;
        private Label m_lblHit;
        private Label m_lblAgl;
        private Label m_lblInt;
        private Label m_lblAp;
        private NumericUpDown m_numLevel;
        private NumericUpDown m_numExp;
        private NumericUpDown m_numCurHp;
        private NumericUpDown m_numMaxHp;
        private NumericUpDown m_numCurMp;
        private NumericUpDown m_numMaxMp;
        private NumericUpDown m_numAtk;
        private NumericUpDown m_numDef;
        private NumericUpDown m_numHit;
        private NumericUpDown m_numAgl;
        private NumericUpDown m_numInt;
        private NumericUpDown m_numAp;
        private Button m_btnApplyChar;
        private Button m_btnMaxStats;

        // Inventory Tab Controls
        private Panel m_pnlItemsCard;
        private Label m_lblNoSaveItems;
        private Panel m_pnlItemFilter;
        private Label m_lblSearch;
        private TextBox m_txtItemSearch;
        private CheckBox m_cbItemsOnlyOwned;
        private Button m_btnItem99;
        private Button m_btnSetOwned99;
        private Button m_btnAll99;
        private Button m_btnApplyItems;
        private DataGridView m_dgvItems;

        // Backups Tab Controls
        private Panel m_pnlBackupsCard;
        private ListView m_lvBackups;
        private PictureBox m_pbBackupThumb;
        private Button m_btnRestoreBackup;
        private Button m_btnRefreshBackups;
        private Button m_btnOpenBackupsFolder;

        // Settings Tab Controls
        private Panel m_pnlSettingsCard;
        private GroupBox m_gbRecomp;
        private Label m_lblRecompPathTitle;
        private TextBox m_txtRecompPath;
        private Button m_btnBrowseRecomp;
        private Button m_btnOpenSavesDirSettings;
        private GroupBox m_gbPrefs;
        private Label m_lblPrefRegion;
        private ComboBox m_cbSettingsRegion;
        private Label m_lblPrefLang;
        private ComboBox m_cbSettingsLang;
        private Button m_btnOpenBackupsDirSettings;
        private GroupBox m_gbAbout;
        private Label m_lblAboutTitle;
        private Label m_lblAboutSubtitle;
        private Label m_lblAboutDesc;
        private LinkLabel m_linkAbout;

        // Bottom Log Panel
        private Panel m_pnlBottomLog;
        private Label m_lblLogHeader;
        private TextBox m_txtLog;

        public MainForm()
        {
            // Load preferred settings
            AppConfig cfg = SaveManager.LoadConfig();
            Loc.SetLanguage(cfg.Language);
            m_activeRegion = !string.IsNullOrEmpty(cfg.Region) ? cfg.Region : SaveManager.RegionNtscU;

            LoadVisualAssets();
            InitializeComponent();
            ApplyLocalization();

            // Detect or configure recomp installation
            m_recompRoot = SaveManager.FindRecompRoot();
            UpdateRecompStatus();

            this.Shown += (s, e) => {
                if (string.IsNullOrEmpty(m_recompRoot))
                {
                    PromptUserForRecompFolder(false);
                }
            };

            Log(Loc.Get("LogInitialized"));
        }

        private void LoadVisualAssets()
        {
            // 1. Icon
            try
            {
                Assembly asm = typeof(MainForm).Assembly;
                using (Stream s = asm.GetManifestResourceStream("IU_Recomp_Save_Editor.ico"))
                {
                    if (s != null) this.Icon = new Icon(s);
                }
            }
            catch { }

            if (this.Icon == null)
            {
                try
                {
                    string icoPath = Path.Combine(SaveManager.AppBaseDir, @"assets\IU_Recomp_Save_Editor.ico");
                    if (File.Exists(icoPath)) this.Icon = new Icon(icoPath);
                    else this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
                catch { }
            }

            // 2. Artwork PNG (Menu background)
            try
            {
                Assembly asm = typeof(MainForm).Assembly;
                using (Stream s = asm.GetManifestResourceStream("IU_Recomp_Save_Editor_Menu.png"))
                {
                    if (s != null) m_menuArtwork = Image.FromStream(s);
                }
            }
            catch { }

            if (m_menuArtwork == null)
            {
                try
                {
                    string imgPath = Path.Combine(SaveManager.AppBaseDir, @"assets\IU_Recomp_Save_Editor_Menu.png");
                    if (File.Exists(imgPath)) m_menuArtwork = Image.FromFile(imgPath);
                }
                catch { }
            }

            // 3. Logo/App Icon PNG
            try
            {
                Assembly asm = typeof(MainForm).Assembly;
                using (Stream s = asm.GetManifestResourceStream("IU_Recomp_Save_Editor.png"))
                {
                    if (s != null) m_appIconImage = Image.FromStream(s);
                }
            }
            catch { }

            if (m_appIconImage == null)
            {
                try
                {
                    string imgPath = Path.Combine(SaveManager.AppBaseDir, @"assets\IU_Recomp_Save_Editor.png");
                    if (File.Exists(imgPath)) m_appIconImage = Image.FromFile(imgPath);
                }
                catch { }
            }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1060, 780);
            this.MinimumSize = new Size(980, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(16, 26, 46); // Deep fantasy slate navy
            this.DoubleBuffered = true;

            // Optional background artwork for the entire window
            if (m_menuArtwork != null)
            {
                this.BackgroundImage = m_menuArtwork;
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // 1. Top Header Bar (Deep sapphire navy with subtle gold border)
            m_pnlHeader = new Panel();
            m_pnlHeader.Dock = DockStyle.Top;
            m_pnlHeader.Height = 56;
            m_pnlHeader.BackColor = Color.FromArgb(235, 14, 25, 48); // Translucent deep navy
            m_pnlHeader.Padding = new Padding(12, 6, 12, 6);
            m_pnlHeader.Paint += (s, e) => {
                // Gold bottom accent line
                using (Pen pen = new Pen(Color.FromArgb(198, 161, 91), 1.5f))
                {
                    e.Graphics.DrawLine(pen, 0, m_pnlHeader.Height - 1, m_pnlHeader.Width, m_pnlHeader.Height - 1);
                }
            };

            m_pbHeaderIcon = new PictureBox();
            m_pbHeaderIcon.Location = new Point(12, 8);
            m_pbHeaderIcon.Size = new Size(40, 40);
            m_pbHeaderIcon.SizeMode = PictureBoxSizeMode.Zoom;
            m_pbHeaderIcon.Image = m_appIconImage;

            m_lblHeaderTitle = new Label();
            m_lblHeaderTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            m_lblHeaderTitle.ForeColor = Color.FromArgb(245, 248, 255);
            m_lblHeaderTitle.Location = new Point(58, 8);
            m_lblHeaderTitle.Size = new Size(380, 22);

            m_lblHeaderSubtitle = new Label();
            m_lblHeaderSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            m_lblHeaderSubtitle.ForeColor = Color.FromArgb(170, 200, 240);
            m_lblHeaderSubtitle.Location = new Point(59, 30);
            m_lblHeaderSubtitle.Size = new Size(380, 18);

            // Active Save Status Pill
            m_lblActiveSaveBadge = new Label();
            m_lblActiveSaveBadge.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            m_lblActiveSaveBadge.TextAlign = ContentAlignment.MiddleCenter;
            m_lblActiveSaveBadge.Location = new Point(445, 14);
            m_lblActiveSaveBadge.Size = new Size(270, 28);
            m_lblActiveSaveBadge.BackColor = Color.FromArgb(40, 60, 95);
            m_lblActiveSaveBadge.ForeColor = Color.FromArgb(220, 230, 250);
            m_lblActiveSaveBadge.BorderStyle = BorderStyle.FixedSingle;

            // Region Selector
            m_lblRegion = new Label();
            m_lblRegion.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            m_lblRegion.ForeColor = Color.FromArgb(210, 225, 245);
            m_lblRegion.Location = new Point(725, 18);
            m_lblRegion.Size = new Size(55, 20);
            m_lblRegion.TextAlign = ContentAlignment.MiddleRight;

            m_cbRegion = new ComboBox();
            m_cbRegion.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cbRegion.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            m_cbRegion.Location = new Point(785, 16);
            m_cbRegion.Size = new Size(95, 24);
            m_cbRegion.Items.Add(SaveManager.RegionNtscU);
            m_cbRegion.Items.Add(SaveManager.RegionPal);
            m_cbRegion.SelectedIndex = (m_activeRegion == SaveManager.RegionPal) ? 1 : 0;
            m_cbRegion.SelectedIndexChanged += OnRegionDropdownChanged;

            // Language Selector
            m_lblLang = new Label();
            m_lblLang.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            m_lblLang.ForeColor = Color.FromArgb(210, 225, 245);
            m_lblLang.Location = new Point(885, 18);
            m_lblLang.Size = new Size(65, 20);
            m_lblLang.TextAlign = ContentAlignment.MiddleRight;

            m_cbLanguage = new ComboBox();
            m_cbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cbLanguage.Font = new Font("Segoe UI", 8.5F);
            m_cbLanguage.Location = new Point(955, 16);
            m_cbLanguage.Size = new Size(85, 24);
            m_cbLanguage.Items.Add("English");
            m_cbLanguage.Items.Add("Español");
            m_cbLanguage.SelectedIndex = (Loc.CurrentLanguage == Loc.SpanishLanguage) ? 1 : 0;
            m_cbLanguage.SelectedIndexChanged += OnLanguageDropdownChanged;

            m_pnlHeader.Controls.Add(m_pbHeaderIcon);
            m_pnlHeader.Controls.Add(m_lblHeaderTitle);
            m_pnlHeader.Controls.Add(m_lblHeaderSubtitle);
            m_pnlHeader.Controls.Add(m_lblActiveSaveBadge);
            m_pnlHeader.Controls.Add(m_lblRegion);
            m_pnlHeader.Controls.Add(m_cbRegion);
            m_pnlHeader.Controls.Add(m_lblLang);
            m_pnlHeader.Controls.Add(m_cbLanguage);

            // 2. Tab Control
            m_tabControl = new TabControl();
            m_tabControl.Dock = DockStyle.Fill;
            m_tabControl.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            m_tabControl.Padding = new Point(14, 6);

            m_tabSaves = new TabPage();
            m_tabCharacters = new TabPage();
            m_tabInventory = new TabPage();
            m_tabBackups = new TabPage();
            m_tabSettings = new TabPage();

            m_tabControl.TabPages.Add(m_tabSaves);
            m_tabControl.TabPages.Add(m_tabCharacters);
            m_tabControl.TabPages.Add(m_tabInventory);
            m_tabControl.TabPages.Add(m_tabBackups);
            m_tabControl.TabPages.Add(m_tabSettings);
            m_tabControl.SelectedIndexChanged += (s, e) => {
                if (m_tabControl.SelectedTab == m_tabBackups) RefreshBackupsList();
            };

            // 3. Bottom Log Panel
            m_pnlBottomLog = new Panel();
            m_pnlBottomLog.Dock = DockStyle.Bottom;
            m_pnlBottomLog.Height = 100;
            m_pnlBottomLog.BackColor = Color.FromArgb(240, 12, 18, 32); // Dark translucent navy
            m_pnlBottomLog.Padding = new Padding(8, 4, 8, 6);
            m_pnlBottomLog.Paint += (s, e) => {
                using (Pen pen = new Pen(Color.FromArgb(50, 75, 115), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, m_pnlBottomLog.Width, 0);
                }
            };

            m_lblLogHeader = new Label();
            m_lblLogHeader.Dock = DockStyle.Top;
            m_lblLogHeader.Height = 18;
            m_lblLogHeader.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            m_lblLogHeader.ForeColor = Color.FromArgb(180, 205, 235);

            m_txtLog = new TextBox();
            m_txtLog.Dock = DockStyle.Fill;
            m_txtLog.Multiline = true;
            m_txtLog.ScrollBars = ScrollBars.Vertical;
            m_txtLog.ReadOnly = true;
            m_txtLog.BackColor = Color.FromArgb(16, 24, 40);
            m_txtLog.ForeColor = Color.FromArgb(215, 230, 245);
            m_txtLog.Font = new Font("Consolas", 8.5F);
            m_txtLog.BorderStyle = BorderStyle.None;

            m_pnlBottomLog.Controls.Add(m_txtLog);
            m_pnlBottomLog.Controls.Add(m_lblLogHeader);

            // Assemble Form
            this.Controls.Add(m_tabControl);
            this.Controls.Add(m_pnlHeader);
            this.Controls.Add(m_pnlBottomLog);

            // Build Individual Tabs
            SetupSavesTab();
            SetupCharactersTab();
            SetupInventoryTab();
            SetupBackupsTab();
            SetupSettingsTab();
        }

        #region TAB 1: Saves
        private void SetupSavesTab()
        {
            m_tabSaves.BackColor = Color.FromArgb(235, 243, 246, 252);
            m_tabSaves.Padding = new Padding(12);

            m_pnlSavesCard = new Panel();
            m_pnlSavesCard.Dock = DockStyle.Fill;
            m_pnlSavesCard.BackColor = Color.FromArgb(248, 250, 253);
            m_pnlSavesCard.Padding = new Padding(14);
            m_pnlSavesCard.BorderStyle = BorderStyle.FixedSingle;

            // Location banner
            m_lblGameLocationBanner = new Label();
            m_lblGameLocationBanner.Location = new Point(14, 12);
            m_lblGameLocationBanner.Size = new Size(990, 22);
            m_lblGameLocationBanner.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            m_lblGameLocationBanner.ForeColor = Color.FromArgb(30, 50, 90);

            // Left: Slots ListView
            m_lvSlots = new ListView();
            m_lvSlots.View = View.Details;
            m_lvSlots.FullRowSelect = true;
            m_lvSlots.GridLines = true;
            m_lvSlots.Location = new Point(14, 40);
            m_lvSlots.Size = new Size(650, 260);
            m_lvSlots.BackColor = Color.White;
            m_lvSlots.Font = new Font("Segoe UI", 9F);
            m_lvSlots.Columns.Add("Slot", 55);
            m_lvSlots.Columns.Add("Folder", 160);
            m_lvSlots.Columns.Add("Modified", 130);
            m_lvSlots.Columns.Add("Fol", 95);
            m_lvSlots.Columns.Add("CRC Status", 100);
            m_lvSlots.Columns.Add("SHA-256", 85);
            m_lvSlots.SelectedIndexChanged += OnSlotSelectionChanged;
            m_lvSlots.DoubleClick += (s, e) => OpenSelectedSlot();

            // Right: Thumbnail & Details Card
            Panel pnlThumbBox = new Panel();
            pnlThumbBox.Location = new Point(680, 40);
            pnlThumbBox.Size = new Size(320, 260);
            pnlThumbBox.BackColor = Color.FromArgb(240, 244, 250);
            pnlThumbBox.BorderStyle = BorderStyle.FixedSingle;
            pnlThumbBox.Padding = new Padding(10);

            m_pbThumbnail = new PictureBox();
            m_pbThumbnail.Location = new Point(12, 10);
            m_pbThumbnail.Size = new Size(294, 155);
            m_pbThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
            m_pbThumbnail.BackColor = Color.FromArgb(20, 25, 35);
            m_pbThumbnail.BorderStyle = BorderStyle.FixedSingle;

            m_lblSlotDetailTitle = new Label();
            m_lblSlotDetailTitle.Location = new Point(12, 172);
            m_lblSlotDetailTitle.Size = new Size(294, 20);
            m_lblSlotDetailTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_lblSlotDetailTitle.ForeColor = Color.FromArgb(25, 45, 80);

            m_lblSlotDetailInfo = new Label();
            m_lblSlotDetailInfo.Location = new Point(12, 195);
            m_lblSlotDetailInfo.Size = new Size(294, 55);
            m_lblSlotDetailInfo.Font = new Font("Segoe UI", 8.5F);
            m_lblSlotDetailInfo.ForeColor = Color.FromArgb(60, 70, 85);

            pnlThumbBox.Controls.Add(m_pbThumbnail);
            pnlThumbBox.Controls.Add(m_lblSlotDetailTitle);
            pnlThumbBox.Controls.Add(m_lblSlotDetailInfo);

            // Active Slot & Fol Edit Box
            m_gbActiveSlot = new GroupBox();
            m_gbActiveSlot.Location = new Point(14, 312);
            m_gbActiveSlot.Size = new Size(986, 170);
            m_gbActiveSlot.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_gbActiveSlot.ForeColor = Color.FromArgb(25, 45, 80);
            m_gbActiveSlot.Text = Loc.Get("GbActiveSlotDetails");

            m_lblFol = new Label();
            m_lblFol.Location = new Point(20, 32);
            m_lblFol.Size = new Size(130, 26);
            m_lblFol.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            m_numFol = new NumericUpDown();
            m_numFol.Location = new Point(160, 30);
            m_numFol.Size = new Size(160, 26);
            m_numFol.Maximum = 99999999;
            m_numFol.ThousandsSeparator = true;
            m_numFol.Enabled = false;

            m_btnUpdateFol = new Button();
            m_btnUpdateFol.Location = new Point(335, 28);
            m_btnUpdateFol.Size = new Size(110, 30);
            m_btnUpdateFol.Font = new Font("Segoe UI", 9F);
            m_btnUpdateFol.Enabled = false;
            m_btnUpdateFol.Click += (s, e) => {
                if (m_currentPayload != null)
                {
                    m_currentPayload.SetFol((uint)m_numFol.Value);
                    m_hasUnsavedChanges = true;
                    UpdateChecksumDisplay();
                    UpdateActiveBadge();
                    Log(Loc.Format("LogFolUpdated", m_numFol.Value));
                }
            };

            m_btnMaxFol = new Button();
            m_btnMaxFol.Location = new Point(455, 28);
            m_btnMaxFol.Size = new Size(170, 30);
            m_btnMaxFol.Font = new Font("Segoe UI", 9F);
            m_btnMaxFol.Enabled = false;
            m_btnMaxFol.Click += (s, e) => {
                m_numFol.Value = 99999999;
                if (m_currentPayload != null)
                {
                    m_currentPayload.SetFol(99999999);
                    m_hasUnsavedChanges = true;
                    UpdateChecksumDisplay();
                    UpdateActiveBadge();
                    Log(Loc.Get("LogFolMax"));
                }
            };

            m_lblChecksums = new Label();
            m_lblChecksums.Location = new Point(20, 72);
            m_lblChecksums.Size = new Size(940, 38);
            m_lblChecksums.Font = new Font("Consolas", 8.5F);
            m_lblChecksums.ForeColor = Color.FromArgb(50, 60, 75);

            // Action Buttons
            m_btnOpenSave = new Button();
            m_btnOpenSave.Location = new Point(20, 120);
            m_btnOpenSave.Size = new Size(150, 36);
            m_btnOpenSave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_btnOpenSave.BackColor = Color.FromArgb(37, 99, 235);
            m_btnOpenSave.ForeColor = Color.White;
            m_btnOpenSave.FlatStyle = FlatStyle.Flat;
            m_btnOpenSave.Click += (s, e) => OpenSelectedSlot();

            m_btnSaveChanges = new Button();
            m_btnSaveChanges.Location = new Point(180, 120);
            m_btnSaveChanges.Size = new Size(240, 36);
            m_btnSaveChanges.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_btnSaveChanges.BackColor = Color.FromArgb(34, 150, 70);
            m_btnSaveChanges.ForeColor = Color.White;
            m_btnSaveChanges.FlatStyle = FlatStyle.Flat;
            m_btnSaveChanges.Enabled = false;
            m_btnSaveChanges.Click += (s, e) => SaveCurrentSlotToFile();

            m_btnRefreshSaves = new Button();
            m_btnRefreshSaves.Location = new Point(430, 120);
            m_btnRefreshSaves.Size = new Size(140, 36);
            m_btnRefreshSaves.Font = new Font("Segoe UI", 9F);
            m_btnRefreshSaves.Click += (s, e) => RefreshSaveSlots();

            m_btnOpenSaveFolder = new Button();
            m_btnOpenSaveFolder.Location = new Point(580, 120);
            m_btnOpenSaveFolder.Size = new Size(180, 36);
            m_btnOpenSaveFolder.Font = new Font("Segoe UI", 9F);
            m_btnOpenSaveFolder.Click += (s, e) => OpenCurrentRegionSaveFolder();

            m_btnManualBackup = new Button();
            m_btnManualBackup.Location = new Point(770, 120);
            m_btnManualBackup.Size = new Size(190, 36);
            m_btnManualBackup.Font = new Font("Segoe UI", 9F);
            m_btnManualBackup.Click += (s, e) => CreateManualBackupFromSelection();

            m_gbActiveSlot.Controls.Add(m_lblFol);
            m_gbActiveSlot.Controls.Add(m_numFol);
            m_gbActiveSlot.Controls.Add(m_btnUpdateFol);
            m_gbActiveSlot.Controls.Add(m_btnMaxFol);
            m_gbActiveSlot.Controls.Add(m_lblChecksums);
            m_gbActiveSlot.Controls.Add(m_btnOpenSave);
            m_gbActiveSlot.Controls.Add(m_btnSaveChanges);
            m_gbActiveSlot.Controls.Add(m_btnRefreshSaves);
            m_gbActiveSlot.Controls.Add(m_btnOpenSaveFolder);
            m_gbActiveSlot.Controls.Add(m_btnManualBackup);

            m_pnlSavesCard.Controls.Add(m_lblGameLocationBanner);
            m_pnlSavesCard.Controls.Add(m_lvSlots);
            m_pnlSavesCard.Controls.Add(pnlThumbBox);
            m_pnlSavesCard.Controls.Add(m_gbActiveSlot);

            m_tabSaves.Controls.Add(m_pnlSavesCard);
        }
        #endregion

        #region TAB 2: Characters
        private void SetupCharactersTab()
        {
            m_tabCharacters.BackColor = Color.FromArgb(235, 243, 246, 252);
            m_tabCharacters.Padding = new Padding(12);

            m_pnlCharsCard = new Panel();
            m_pnlCharsCard.Dock = DockStyle.Fill;
            m_pnlCharsCard.BackColor = Color.FromArgb(248, 250, 253);
            m_pnlCharsCard.Padding = new Padding(14);
            m_pnlCharsCard.BorderStyle = BorderStyle.FixedSingle;

            // Placeholder when no save loaded
            m_lblNoSaveChars = new Label();
            m_lblNoSaveChars.Location = new Point(20, 20);
            m_lblNoSaveChars.Size = new Size(950, 30);
            m_lblNoSaveChars.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            m_lblNoSaveChars.ForeColor = Color.FromArgb(160, 60, 60);

            // Left panel: 18 character names
            m_gbCharList = new GroupBox();
            m_gbCharList.Location = new Point(14, 55);
            m_gbCharList.Size = new Size(200, 420);
            m_gbCharList.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_gbCharList.ForeColor = Color.FromArgb(25, 45, 80);
            m_gbCharList.Text = Loc.Get("GbCharList");

            m_lbCharacters = new ListBox();
            m_lbCharacters.Dock = DockStyle.Fill;
            m_lbCharacters.Font = new Font("Segoe UI", 9.5F);
            foreach (string name in CharacterData.Names)
            {
                m_lbCharacters.Items.Add(name);
            }
            m_lbCharacters.SelectedIndex = 0;
            m_lbCharacters.SelectedIndexChanged += OnCharacterSelectionChanged;
            m_gbCharList.Controls.Add(m_lbCharacters);

            // Right panel: Character editor
            m_pnlCharEditor = new Panel();
            m_pnlCharEditor.Location = new Point(230, 55);
            m_pnlCharEditor.Size = new Size(770, 420);
            m_pnlCharEditor.BackColor = Color.White;
            m_pnlCharEditor.BorderStyle = BorderStyle.FixedSingle;
            m_pnlCharEditor.Padding = new Padding(18);

            m_lblCharTitle = new Label();
            m_lblCharTitle.Location = new Point(20, 16);
            m_lblCharTitle.Size = new Size(400, 26);
            m_lblCharTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            m_lblCharTitle.ForeColor = Color.FromArgb(20, 40, 75);

            m_cbInParty = new CheckBox();
            m_cbInParty.Location = new Point(25, 52);
            m_cbInParty.Size = new Size(160, 24);
            m_cbInParty.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            m_cbInActiveParty = new CheckBox();
            m_cbInActiveParty.Location = new Point(210, 52);
            m_cbInActiveParty.Size = new Size(220, 24);
            m_cbInActiveParty.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            int startY = 88;
            int gap = 34;
            int col1 = 25, col2 = 115, col3 = 260, col4 = 350;

            // Row 1: Level & EXP
            m_lblLevel = AddCharLabel(m_pnlCharEditor, "Level:", col1, startY);
            m_numLevel = AddCharNum(m_pnlCharEditor, col2, startY, 1, 255);
            m_lblExp = AddCharLabel(m_pnlCharEditor, "EXP:", col3, startY);
            m_numExp = AddCharNum(m_pnlCharEditor, col4, startY, 0, 99999999);

            // Row 2: HP Actual & Max HP
            startY += gap;
            m_lblCurHp = AddCharLabel(m_pnlCharEditor, "Current HP:", col1, startY);
            m_numCurHp = AddCharNum(m_pnlCharEditor, col2, startY, 1, 99999);
            m_lblMaxHp = AddCharLabel(m_pnlCharEditor, "Max HP:", col3, startY);
            m_numMaxHp = AddCharNum(m_pnlCharEditor, col4, startY, 1, 99999);

            // Row 3: MP Actual & Max MP
            startY += gap;
            m_lblCurMp = AddCharLabel(m_pnlCharEditor, "Current MP:", col1, startY);
            m_numCurMp = AddCharNum(m_pnlCharEditor, col2, startY, 0, 99999);
            m_lblMaxMp = AddCharLabel(m_pnlCharEditor, "Max MP:", col3, startY);
            m_numMaxMp = AddCharNum(m_pnlCharEditor, col4, startY, 0, 99999);

            // Row 4: ATK & DEF
            startY += gap;
            m_lblAtk = AddCharLabel(m_pnlCharEditor, "ATK:", col1, startY);
            m_numAtk = AddCharNum(m_pnlCharEditor, col2, startY, 1, 99999);
            m_lblDef = AddCharLabel(m_pnlCharEditor, "DEF:", col3, startY);
            m_numDef = AddCharNum(m_pnlCharEditor, col4, startY, 1, 99999);

            // Row 5: HIT & AGL
            startY += gap;
            m_lblHit = AddCharLabel(m_pnlCharEditor, "HIT:", col1, startY);
            m_numHit = AddCharNum(m_pnlCharEditor, col2, startY, 1, 99999);
            m_lblAgl = AddCharLabel(m_pnlCharEditor, "AGL:", col3, startY);
            m_numAgl = AddCharNum(m_pnlCharEditor, col4, startY, 1, 99999);

            // Row 6: INT & AP
            startY += gap;
            m_lblInt = AddCharLabel(m_pnlCharEditor, "INT:", col1, startY);
            m_numInt = AddCharNum(m_pnlCharEditor, col2, startY, 1, 99999);
            m_lblAp = AddCharLabel(m_pnlCharEditor, "AP:", col3, startY);
            m_numAp = AddCharNum(m_pnlCharEditor, col4, startY, 0, 30000);

            // Buttons
            startY += gap + 16;
            m_btnApplyChar = new Button();
            m_btnApplyChar.Location = new Point(col1, startY);
            m_btnApplyChar.Size = new Size(220, 36);
            m_btnApplyChar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_btnApplyChar.BackColor = Color.FromArgb(37, 99, 235);
            m_btnApplyChar.ForeColor = Color.White;
            m_btnApplyChar.FlatStyle = FlatStyle.Flat;
            m_btnApplyChar.Click += OnApplyCharacterClicked;

            m_btnMaxStats = new Button();
            m_btnMaxStats.Location = new Point(col1 + 240, startY);
            m_btnMaxStats.Size = new Size(200, 36);
            m_btnMaxStats.Font = new Font("Segoe UI", 9F);
            m_btnMaxStats.Click += OnMaxStatsClicked;

            m_pnlCharEditor.Controls.Add(m_lblCharTitle);
            m_pnlCharEditor.Controls.Add(m_cbInParty);
            m_pnlCharEditor.Controls.Add(m_cbInActiveParty);
            m_pnlCharEditor.Controls.Add(m_btnApplyChar);
            m_pnlCharEditor.Controls.Add(m_btnMaxStats);

            m_pnlCharsCard.Controls.Add(m_lblNoSaveChars);
            m_pnlCharsCard.Controls.Add(m_gbCharList);
            m_pnlCharsCard.Controls.Add(m_pnlCharEditor);

            m_tabCharacters.Controls.Add(m_pnlCharsCard);
        }

        private Label AddCharLabel(Panel p, string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(x, y + 3);
            lbl.Size = new Size(85, 20);
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(40, 50, 70);
            p.Controls.Add(lbl);
            return lbl;
        }

        private NumericUpDown AddCharNum(Panel p, int x, int y, decimal min, decimal max)
        {
            NumericUpDown num = new NumericUpDown();
            num.Location = new Point(x, y);
            num.Size = new Size(125, 24);
            num.Minimum = min;
            num.Maximum = max;
            num.ThousandsSeparator = true;
            p.Controls.Add(num);
            return num;
        }
        #endregion

        #region TAB 3: Inventory
        private void SetupInventoryTab()
        {
            m_tabInventory.BackColor = Color.FromArgb(235, 243, 246, 252);
            m_tabInventory.Padding = new Padding(12);

            m_pnlItemsCard = new Panel();
            m_pnlItemsCard.Dock = DockStyle.Fill;
            m_pnlItemsCard.BackColor = Color.FromArgb(248, 250, 253);
            m_pnlItemsCard.Padding = new Padding(14);
            m_pnlItemsCard.BorderStyle = BorderStyle.FixedSingle;

            // Placeholder when no save loaded
            m_lblNoSaveItems = new Label();
            m_lblNoSaveItems.Location = new Point(20, 15);
            m_lblNoSaveItems.Size = new Size(950, 24);
            m_lblNoSaveItems.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            m_lblNoSaveItems.ForeColor = Color.FromArgb(160, 60, 60);

            // Filter & Action Toolbar
            m_pnlItemFilter = new Panel();
            m_pnlItemFilter.Location = new Point(14, 45);
            m_pnlItemFilter.Size = new Size(986, 42);
            m_pnlItemFilter.BackColor = Color.FromArgb(240, 244, 250);
            m_pnlItemFilter.BorderStyle = BorderStyle.FixedSingle;

            m_lblSearch = new Label();
            m_lblSearch.Location = new Point(10, 11);
            m_lblSearch.Size = new Size(55, 20);
            m_lblSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            m_txtItemSearch = new TextBox();
            m_txtItemSearch.Location = new Point(70, 8);
            m_txtItemSearch.Size = new Size(170, 24);
            m_txtItemSearch.TextChanged += (s, e) => FilterItems();

            m_cbItemsOnlyOwned = new CheckBox();
            m_cbItemsOnlyOwned.Location = new Point(255, 9);
            m_cbItemsOnlyOwned.Size = new Size(160, 24);
            m_cbItemsOnlyOwned.Font = new Font("Segoe UI", 9F);
            m_cbItemsOnlyOwned.CheckedChanged += (s, e) => FilterItems();

            m_btnItem99 = new Button();
            m_btnItem99.Location = new Point(430, 6);
            m_btnItem99.Size = new Size(105, 28);
            m_btnItem99.Click += (s, e) => SetCurrentItemAmount(99);

            m_btnSetOwned99 = new Button();
            m_btnSetOwned99.Location = new Point(545, 6);
            m_btnSetOwned99.Size = new Size(130, 28);
            m_btnSetOwned99.Click += OnSetOwnedItems99Clicked;

            m_btnAll99 = new Button();
            m_btnAll99.Location = new Point(685, 6);
            m_btnAll99.Size = new Size(140, 28);
            m_btnAll99.Click += OnSetAllItems99Clicked;

            m_btnApplyItems = new Button();
            m_btnApplyItems.Location = new Point(835, 6);
            m_btnApplyItems.Size = new Size(140, 28);
            m_btnApplyItems.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            m_btnApplyItems.BackColor = Color.FromArgb(37, 99, 235);
            m_btnApplyItems.ForeColor = Color.White;
            m_btnApplyItems.FlatStyle = FlatStyle.Flat;
            m_btnApplyItems.Click += OnApplyInventoryClicked;

            m_pnlItemFilter.Controls.Add(m_lblSearch);
            m_pnlItemFilter.Controls.Add(m_txtItemSearch);
            m_pnlItemFilter.Controls.Add(m_cbItemsOnlyOwned);
            m_pnlItemFilter.Controls.Add(m_btnItem99);
            m_pnlItemFilter.Controls.Add(m_btnSetOwned99);
            m_pnlItemFilter.Controls.Add(m_btnAll99);
            m_pnlItemFilter.Controls.Add(m_btnApplyItems);

            // DataGridView for Items
            m_dgvItems = new DataGridView();
            m_dgvItems.Location = new Point(14, 95);
            m_dgvItems.Size = new Size(986, 380);
            m_dgvItems.AutoGenerateColumns = false;
            m_dgvItems.AllowUserToAddRows = false;
            m_dgvItems.AllowUserToDeleteRows = false;
            m_dgvItems.RowHeadersVisible = false;
            m_dgvItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            m_dgvItems.BackgroundColor = Color.White;

            DataGridViewTextBoxColumn colId = new DataGridViewTextBoxColumn();
            colId.HeaderText = "ID";
            colId.DataPropertyName = "Id";
            colId.Width = 65;
            colId.ReadOnly = true;

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.HeaderText = "Item Name";
            colName.DataPropertyName = "Name";
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colName.ReadOnly = true;

            DataGridViewTextBoxColumn colAmount = new DataGridViewTextBoxColumn();
            colAmount.HeaderText = "Quantity (0-99)";
            colAmount.DataPropertyName = "Amount";
            colAmount.Width = 140;
            colAmount.ReadOnly = false;

            m_dgvItems.Columns.Add(colId);
            m_dgvItems.Columns.Add(colName);
            m_dgvItems.Columns.Add(colAmount);
            m_dgvItems.CellEndEdit += (s, e) => { m_hasUnsavedChanges = true; };

            m_pnlItemsCard.Controls.Add(m_lblNoSaveItems);
            m_pnlItemsCard.Controls.Add(m_pnlItemFilter);
            m_pnlItemsCard.Controls.Add(m_dgvItems);

            m_tabInventory.Controls.Add(m_pnlItemsCard);
        }
        #endregion

        #region TAB 4: Backups
        private void SetupBackupsTab()
        {
            m_tabBackups.BackColor = Color.FromArgb(235, 243, 246, 252);
            m_tabBackups.Padding = new Padding(12);

            m_pnlBackupsCard = new Panel();
            m_pnlBackupsCard.Dock = DockStyle.Fill;
            m_pnlBackupsCard.BackColor = Color.FromArgb(248, 250, 253);
            m_pnlBackupsCard.Padding = new Padding(14);
            m_pnlBackupsCard.BorderStyle = BorderStyle.FixedSingle;

            // List of backups
            m_lvBackups = new ListView();
            m_lvBackups.View = View.Details;
            m_lvBackups.FullRowSelect = true;
            m_lvBackups.GridLines = true;
            m_lvBackups.Location = new Point(14, 15);
            m_lvBackups.Size = new Size(720, 410);
            m_lvBackups.BackColor = Color.White;
            m_lvBackups.Font = new Font("Segoe UI", 9F);
            m_lvBackups.Columns.Add("Backup File", 260);
            m_lvBackups.Columns.Add("Region", 70);
            m_lvBackups.Columns.Add("Slot", 60);
            m_lvBackups.Columns.Add("Date & Time", 140);
            m_lvBackups.Columns.Add("Fol", 90);
            m_lvBackups.Columns.Add("Size", 80);
            m_lvBackups.SelectedIndexChanged += OnBackupSelectionChanged;

            // Right: Backup thumbnail preview & info
            Panel pnlBkpSide = new Panel();
            pnlBkpSide.Location = new Point(750, 15);
            pnlBkpSide.Size = new Size(250, 410);
            pnlBkpSide.BackColor = Color.FromArgb(240, 244, 250);
            pnlBkpSide.BorderStyle = BorderStyle.FixedSingle;
            pnlBkpSide.Padding = new Padding(10);

            m_pbBackupThumb = new PictureBox();
            m_pbBackupThumb.Location = new Point(10, 10);
            m_pbBackupThumb.Size = new Size(228, 140);
            m_pbBackupThumb.SizeMode = PictureBoxSizeMode.Zoom;
            m_pbBackupThumb.BackColor = Color.FromArgb(20, 25, 35);
            m_pbBackupThumb.BorderStyle = BorderStyle.FixedSingle;

            pnlBkpSide.Controls.Add(m_pbBackupThumb);

            // Bottom Buttons
            m_btnRestoreBackup = new Button();
            m_btnRestoreBackup.Location = new Point(14, 435);
            m_btnRestoreBackup.Size = new Size(220, 36);
            m_btnRestoreBackup.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_btnRestoreBackup.BackColor = Color.FromArgb(180, 50, 50);
            m_btnRestoreBackup.ForeColor = Color.White;
            m_btnRestoreBackup.FlatStyle = FlatStyle.Flat;
            m_btnRestoreBackup.Click += OnRestoreBackupClicked;

            m_btnRefreshBackups = new Button();
            m_btnRefreshBackups.Location = new Point(245, 435);
            m_btnRefreshBackups.Size = new Size(160, 36);
            m_btnRefreshBackups.Click += (s, e) => RefreshBackupsList();

            m_btnOpenBackupsFolder = new Button();
            m_btnOpenBackupsFolder.Location = new Point(415, 435);
            m_btnOpenBackupsFolder.Size = new Size(200, 36);
            m_btnOpenBackupsFolder.Click += (s, e) => {
                string dir = SaveManager.GetBackupsDirForRegion(m_activeRegion);
                if (Directory.Exists(dir)) System.Diagnostics.Process.Start("explorer.exe", dir);
            };

            m_pnlBackupsCard.Controls.Add(m_lvBackups);
            m_pnlBackupsCard.Controls.Add(pnlBkpSide);
            m_pnlBackupsCard.Controls.Add(m_btnRestoreBackup);
            m_pnlBackupsCard.Controls.Add(m_btnRefreshBackups);
            m_pnlBackupsCard.Controls.Add(m_btnOpenBackupsFolder);

            m_tabBackups.Controls.Add(m_pnlBackupsCard);
        }
        #endregion

        #region TAB 5: Settings
        private void SetupSettingsTab()
        {
            m_tabSettings.BackColor = Color.FromArgb(235, 243, 246, 252);
            m_tabSettings.Padding = new Padding(12);

            m_pnlSettingsCard = new Panel();
            m_pnlSettingsCard.Dock = DockStyle.Fill;
            m_pnlSettingsCard.BackColor = Color.FromArgb(248, 250, 253);
            m_pnlSettingsCard.Padding = new Padding(18);
            m_pnlSettingsCard.BorderStyle = BorderStyle.FixedSingle;

            // Group 1: Recomp Location
            m_gbRecomp = new GroupBox();
            m_gbRecomp.Location = new Point(14, 15);
            m_gbRecomp.Size = new Size(986, 120);
            m_gbRecomp.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_gbRecomp.ForeColor = Color.FromArgb(25, 45, 80);
            m_gbRecomp.Text = Loc.Get("GbRecomp");

            m_lblRecompPathTitle = new Label();
            m_lblRecompPathTitle.Location = new Point(15, 28);
            m_lblRecompPathTitle.Size = new Size(300, 20);
            m_lblRecompPathTitle.Font = new Font("Segoe UI", 9F);

            m_txtRecompPath = new TextBox();
            m_txtRecompPath.Location = new Point(18, 50);
            m_txtRecompPath.Size = new Size(650, 26);
            m_txtRecompPath.ReadOnly = true;
            m_txtRecompPath.BackColor = Color.White;

            m_btnBrowseRecomp = new Button();
            m_btnBrowseRecomp.Location = new Point(680, 48);
            m_btnBrowseRecomp.Size = new Size(180, 30);
            m_btnBrowseRecomp.Font = new Font("Segoe UI", 9F);
            m_btnBrowseRecomp.Click += (s, e) => PromptUserForRecompFolder(true);

            m_btnOpenSavesDirSettings = new Button();
            m_btnOpenSavesDirSettings.Location = new Point(18, 82);
            m_btnOpenSavesDirSettings.Size = new Size(220, 28);
            m_btnOpenSavesDirSettings.Font = new Font("Segoe UI", 9F);
            m_btnOpenSavesDirSettings.Click += (s, e) => OpenCurrentRegionSaveFolder();

            m_btnOpenBackupsDirSettings = new Button();
            m_btnOpenBackupsDirSettings.Location = new Point(250, 82);
            m_btnOpenBackupsDirSettings.Size = new Size(200, 28);
            m_btnOpenBackupsDirSettings.Font = new Font("Segoe UI", 9F);
            m_btnOpenBackupsDirSettings.Click += (s, e) => {
                string dir = SaveManager.BackupsBaseDir;
                if (Directory.Exists(dir)) System.Diagnostics.Process.Start("explorer.exe", dir);
            };

            m_gbRecomp.Controls.Add(m_lblRecompPathTitle);
            m_gbRecomp.Controls.Add(m_txtRecompPath);
            m_gbRecomp.Controls.Add(m_btnBrowseRecomp);
            m_gbRecomp.Controls.Add(m_btnOpenSavesDirSettings);
            m_gbRecomp.Controls.Add(m_btnOpenBackupsDirSettings);

            // Group 2: Preferences
            m_gbPrefs = new GroupBox();
            m_gbPrefs.Location = new Point(14, 145);
            m_gbPrefs.Size = new Size(986, 95);
            m_gbPrefs.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_gbPrefs.ForeColor = Color.FromArgb(25, 45, 80);
            m_gbPrefs.Text = Loc.Get("GbPreferences");

            m_lblPrefRegion = new Label();
            m_lblPrefRegion.Location = new Point(18, 30);
            m_lblPrefRegion.Size = new Size(160, 20);
            m_lblPrefRegion.Font = new Font("Segoe UI", 9F);

            m_cbSettingsRegion = new ComboBox();
            m_cbSettingsRegion.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cbSettingsRegion.Location = new Point(18, 52);
            m_cbSettingsRegion.Size = new Size(160, 24);
            m_cbSettingsRegion.Items.Add(SaveManager.RegionNtscU);
            m_cbSettingsRegion.Items.Add(SaveManager.RegionPal);
            m_cbSettingsRegion.SelectedIndex = (m_activeRegion == SaveManager.RegionPal) ? 1 : 0;
            m_cbSettingsRegion.SelectedIndexChanged += (s, e) => {
                string reg = (m_cbSettingsRegion.SelectedIndex == 1) ? SaveManager.RegionPal : SaveManager.RegionNtscU;
                SwitchRegion(reg);
            };

            m_lblPrefLang = new Label();
            m_lblPrefLang.Location = new Point(220, 30);
            m_lblPrefLang.Size = new Size(160, 20);
            m_lblPrefLang.Font = new Font("Segoe UI", 9F);

            m_cbSettingsLang = new ComboBox();
            m_cbSettingsLang.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cbSettingsLang.Location = new Point(220, 52);
            m_cbSettingsLang.Size = new Size(160, 24);
            m_cbSettingsLang.Items.Add("English");
            m_cbSettingsLang.Items.Add("Español");
            m_cbSettingsLang.SelectedIndex = (Loc.CurrentLanguage == Loc.SpanishLanguage) ? 1 : 0;
            m_cbSettingsLang.SelectedIndexChanged += (s, e) => {
                string lang = (m_cbSettingsLang.SelectedIndex == 1) ? Loc.SpanishLanguage : Loc.DefaultLanguage;
                SwitchLanguage(lang);
            };

            m_gbPrefs.Controls.Add(m_lblPrefRegion);
            m_gbPrefs.Controls.Add(m_cbSettingsRegion);
            m_gbPrefs.Controls.Add(m_lblPrefLang);
            m_gbPrefs.Controls.Add(m_cbSettingsLang);

            // Group 3: About
            m_gbAbout = new GroupBox();
            m_gbAbout.Location = new Point(14, 250);
            m_gbAbout.Size = new Size(986, 215);
            m_gbAbout.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            m_gbAbout.ForeColor = Color.FromArgb(25, 45, 80);
            m_gbAbout.Text = Loc.Get("GbAbout");

            m_lblAboutTitle = new Label();
            m_lblAboutTitle.Location = new Point(18, 28);
            m_lblAboutTitle.Size = new Size(500, 24);
            m_lblAboutTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            m_lblAboutTitle.ForeColor = Color.FromArgb(20, 45, 85);

            m_lblAboutSubtitle = new Label();
            m_lblAboutSubtitle.Location = new Point(18, 54);
            m_lblAboutSubtitle.Size = new Size(500, 20);
            m_lblAboutSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            m_lblAboutSubtitle.ForeColor = Color.FromArgb(80, 100, 130);

            m_lblAboutDesc = new Label();
            m_lblAboutDesc.Location = new Point(18, 80);
            m_lblAboutDesc.Size = new Size(900, 60);
            m_lblAboutDesc.Font = new Font("Segoe UI", 9F);
            m_lblAboutDesc.ForeColor = Color.FromArgb(40, 50, 65);

            m_linkAbout = new LinkLabel();
            m_linkAbout.Location = new Point(18, 150);
            m_linkAbout.Size = new Size(600, 24);
            m_linkAbout.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            m_linkAbout.Text = "https://github.com/doc-haz/infinite-undiscovery-recomp";
            m_linkAbout.LinkClicked += (s, e) => {
                try { System.Diagnostics.Process.Start("https://github.com/doc-haz/infinite-undiscovery-recomp"); } catch { }
            };

            m_gbAbout.Controls.Add(m_lblAboutTitle);
            m_gbAbout.Controls.Add(m_lblAboutSubtitle);
            m_gbAbout.Controls.Add(m_lblAboutDesc);
            m_gbAbout.Controls.Add(m_linkAbout);

            m_pnlSettingsCard.Controls.Add(m_gbRecomp);
            m_pnlSettingsCard.Controls.Add(m_gbPrefs);
            m_pnlSettingsCard.Controls.Add(m_gbAbout);

            m_tabSettings.Controls.Add(m_pnlSettingsCard);
        }
        #endregion

        #region Localization & UI Refresh
        public void ApplyLocalization()
        {
            this.Text = Loc.Get("AppTitle");
            m_lblHeaderTitle.Text = Loc.Get("AppTitle");
            m_lblHeaderSubtitle.Text = Loc.Get("AppSubtitle");
            m_lblLang.Text = Loc.Get("LangLabel");
            m_lblRegion.Text = Loc.Get("RegionLabel");

            // Tabs
            m_tabSaves.Text = Loc.Get("TabSaves");
            m_tabCharacters.Text = Loc.Get("TabCharacters");
            m_tabInventory.Text = Loc.Get("TabInventory");
            m_tabBackups.Text = Loc.Get("TabBackups");
            m_tabSettings.Text = Loc.Get("TabSettings");

            // Tab 1: Saves
            if (m_gbActiveSlot != null) m_gbActiveSlot.Text = Loc.Get("GbActiveSlotDetails");
            if (m_lvSlots.Columns.Count >= 6)
            {
                m_lvSlots.Columns[0].Text = Loc.Get("ColSlot");
                m_lvSlots.Columns[1].Text = Loc.Get("ColFolderName");
                m_lvSlots.Columns[2].Text = Loc.Get("ColModified");
                m_lvSlots.Columns[3].Text = Loc.Get("ColFol");
                m_lvSlots.Columns[4].Text = Loc.Get("ColCrc");
                m_lvSlots.Columns[5].Text = Loc.Get("ColSha256");
            }
            m_lblFol.Text = Loc.Get("LblFol");
            m_btnUpdateFol.Text = Loc.Get("BtnUpdateFol");
            m_btnMaxFol.Text = Loc.Get("BtnMaxFol");
            m_btnOpenSave.Text = Loc.Get("BtnOpenSave");
            m_btnSaveChanges.Text = Loc.Get("BtnSaveChanges");
            m_btnRefreshSaves.Text = Loc.Get("BtnRefreshSaves");
            m_btnOpenSaveFolder.Text = Loc.Get("BtnOpenFolder");
            m_btnManualBackup.Text = Loc.Get("BtnCreateBackupSlot");

            // Tab 2: Characters
            if (m_gbCharList != null) m_gbCharList.Text = Loc.Get("GbCharList");
            m_lblNoSaveChars.Text = Loc.Get("NoSaveForChars");
            m_cbInParty.Text = Loc.Get("CbInParty");
            m_cbInActiveParty.Text = Loc.Get("CbInActiveParty");
            m_lblLevel.Text = Loc.Get("LblLevel");
            m_lblExp.Text = Loc.Get("LblExp");
            m_lblCurHp.Text = Loc.Get("LblCurHp");
            m_lblMaxHp.Text = Loc.Get("LblMaxHp");
            m_lblCurMp.Text = Loc.Get("LblCurMp");
            m_lblMaxMp.Text = Loc.Get("LblMaxMp");
            m_lblAtk.Text = Loc.Get("LblAtk");
            m_lblDef.Text = Loc.Get("LblDef");
            m_lblHit.Text = Loc.Get("LblHit");
            m_lblAgl.Text = Loc.Get("LblAgl");
            m_lblInt.Text = Loc.Get("LblInt");
            m_lblAp.Text = Loc.Get("LblAp");
            m_btnApplyChar.Text = Loc.Get("BtnApplyChar");
            m_btnMaxStats.Text = Loc.Get("BtnMaxStats");

            // Tab 3: Inventory
            m_lblNoSaveItems.Text = Loc.Get("NoSaveForItems");
            m_lblSearch.Text = Loc.Get("LblSearch");
            m_cbItemsOnlyOwned.Text = Loc.Get("CbOnlyOwned");
            m_btnItem99.Text = Loc.Get("BtnItem99");
            m_btnSetOwned99.Text = Loc.Get("BtnSetOwned99");
            m_btnAll99.Text = Loc.Get("BtnAll99");
            m_btnApplyItems.Text = Loc.Get("BtnApplyItems");
            if (m_dgvItems.Columns.Count >= 3)
            {
                m_dgvItems.Columns[0].HeaderText = Loc.Get("ColItemId");
                m_dgvItems.Columns[1].HeaderText = Loc.Get("ColItemName");
                m_dgvItems.Columns[2].HeaderText = Loc.Get("ColItemAmount");
            }

            // Tab 4: Backups
            if (m_lvBackups.Columns.Count >= 6)
            {
                m_lvBackups.Columns[0].Text = Loc.Get("ColBkpFile");
                m_lvBackups.Columns[1].Text = Loc.Get("ColBkpRegion");
                m_lvBackups.Columns[2].Text = Loc.Get("ColBkpSlot");
                m_lvBackups.Columns[3].Text = Loc.Get("ColBkpDate");
                m_lvBackups.Columns[4].Text = Loc.Get("ColBkpFol");
                m_lvBackups.Columns[5].Text = Loc.Get("ColBkpSize");
            }
            m_btnRestoreBackup.Text = Loc.Get("BtnRestoreBackup");
            m_btnRefreshBackups.Text = Loc.Get("BtnRefreshBackups");
            m_btnOpenBackupsFolder.Text = Loc.Get("BtnOpenBackupsFolder");

            // Tab 5: Settings
            if (m_gbRecomp != null) m_gbRecomp.Text = Loc.Get("GbRecomp");
            m_lblRecompPathTitle.Text = Loc.Get("LblRecompPath");
            m_btnBrowseRecomp.Text = Loc.Get("BtnChangeLocation");
            m_btnOpenSavesDirSettings.Text = Loc.Get("BtnOpenSaveDir");
            m_btnOpenBackupsDirSettings.Text = Loc.Get("BtnOpenBackupsFolder");
            if (m_gbPrefs != null) m_gbPrefs.Text = Loc.Get("GbPreferences");
            m_lblPrefRegion.Text = Loc.Get("LblPreferredRegion");
            m_lblPrefLang.Text = Loc.Get("LblPreferredLanguage");
            if (m_gbAbout != null) m_gbAbout.Text = Loc.Get("GbAbout");
            m_lblAboutTitle.Text = Loc.Get("AboutAppName");
            m_lblAboutSubtitle.Text = Loc.Get("AboutSubtitle");
            m_lblAboutDesc.Text = Loc.Get("AboutDesc");

            // Bottom Log
            m_lblLogHeader.Text = Loc.Get("LogHeader");

            UpdateActiveBadge();
            UpdateChecksumDisplay();
            UpdateRecompStatus();
        }

        private void SwitchLanguage(string lang)
        {
            Loc.SetLanguage(lang);
            SaveManager.SaveLanguagePreference(lang);

            int desiredIdx = (lang == Loc.SpanishLanguage) ? 1 : 0;
            if (m_cbLanguage.SelectedIndex != desiredIdx) m_cbLanguage.SelectedIndex = desiredIdx;
            if (m_cbSettingsLang.SelectedIndex != desiredIdx) m_cbSettingsLang.SelectedIndex = desiredIdx;

            ApplyLocalization();
        }

        private void SwitchRegion(string region)
        {
            if (m_activeRegion == region) return;
            m_activeRegion = region;

            AppConfig cfg = SaveManager.LoadConfig();
            cfg.Region = region;
            SaveManager.SaveConfig(cfg);

            int desiredIdx = (region == SaveManager.RegionPal) ? 1 : 0;
            if (m_cbRegion.SelectedIndex != desiredIdx) m_cbRegion.SelectedIndex = desiredIdx;
            if (m_cbSettingsRegion.SelectedIndex != desiredIdx) m_cbSettingsRegion.SelectedIndex = desiredIdx;

            Log(Loc.Format("LogRegionChanged", region));
            RefreshSaveSlots();
            RefreshBackupsList();
            UpdateRecompStatus();
        }

        private void OnLanguageDropdownChanged(object sender, EventArgs e)
        {
            string lang = (m_cbLanguage.SelectedIndex == 1) ? Loc.SpanishLanguage : Loc.DefaultLanguage;
            if (lang != Loc.CurrentLanguage) SwitchLanguage(lang);
        }

        private void OnRegionDropdownChanged(object sender, EventArgs e)
        {
            string reg = (m_cbRegion.SelectedIndex == 1) ? SaveManager.RegionPal : SaveManager.RegionNtscU;
            if (reg != m_activeRegion) SwitchRegion(reg);
        }

        private void UpdateActiveBadge()
        {
            if (m_currentPayload == null || m_activeSlot == null)
            {
                m_lblActiveSaveBadge.Text = Loc.Get("BadgeNoSave");
                m_lblActiveSaveBadge.BackColor = Color.FromArgb(40, 50, 70);
                m_lblActiveSaveBadge.ForeColor = Color.FromArgb(170, 185, 205);
                m_btnSaveChanges.Enabled = false;
            }
            else
            {
                string tag = Loc.Format("BadgeActiveSave", m_activeSlot.Region, m_activeSlot.SlotNumber);
                if (m_hasUnsavedChanges) tag += " *";
                m_lblActiveSaveBadge.Text = tag;
                m_lblActiveSaveBadge.BackColor = Color.FromArgb(20, 80, 50);
                m_lblActiveSaveBadge.ForeColor = Color.FromArgb(210, 255, 220);
                m_btnSaveChanges.Enabled = true;
            }
        }

        private void UpdateChecksumDisplay()
        {
            if (m_currentPayload == null)
            {
                m_lblChecksums.Text = "Magic: ---- | Version: ---- | TitleID: ---- | CRC1: ---- | CRC2: ---- | SHA256: ----";
                return;
            }

            bool crc1Ok = (m_currentPayload.StoredCrc1 == m_currentPayload.CalculatedCrc1);
            bool crc2Ok = (m_currentPayload.StoredCrc2 == m_currentPayload.CalculatedCrc2);
            string status1 = crc1Ok ? Loc.Get("StatusValid") : Loc.Get("StatusInvalid");
            string status2 = crc2Ok ? Loc.Get("StatusValid") : Loc.Get("StatusInvalid");

            m_lblChecksums.Text = Loc.Format(
                "MetadataFormat",
                m_currentPayload.Magic, m_currentPayload.Version, m_currentPayload.TitleId,
                m_currentPayload.StoredCrc1, status1,
                m_currentPayload.StoredCrc2, status2,
                m_currentPayload.Sha256Hash);
        }

        private void UpdateRecompStatus()
        {
            if (string.IsNullOrEmpty(m_recompRoot) || !Directory.Exists(m_recompRoot))
            {
                m_lblGameLocationBanner.Text = "Game: Infinite Undiscovery Recomp (Location Not Configured) | Region: " + m_activeRegion;
                m_txtRecompPath.Text = "";
            }
            else
            {
                string savesDir = SaveManager.GetRegionSavesDir(m_recompRoot, m_activeRegion);
                m_lblGameLocationBanner.Text = string.Format("Game: Infinite Undiscovery Recomp | Region: {0} | Saves: {1}", m_activeRegion, savesDir);
                m_txtRecompPath.Text = m_recompRoot;
            }
        }
        #endregion

        #region Recomp Location Configuration
        private void PromptUserForRecompFolder(bool isUserInitiated)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = Loc.Get("MsgSelectRecompFolder");
                if (Directory.Exists(SaveManager.AppBaseDir))
                {
                    fbd.SelectedPath = SaveManager.AppBaseDir;
                }

                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    string selected = SaveManager.NormalizeRecompRoot(fbd.SelectedPath);
                    if (selected != null)
                    {
                        m_recompRoot = selected;
                        AppConfig cfg = SaveManager.LoadConfig();
                        cfg.RecompPath = selected;

                        // Check available regions
                        var regions = SaveManager.GetAvailableRegions(selected);
                        if (regions.Count == 1)
                        {
                            m_activeRegion = regions[0];
                            cfg.Region = m_activeRegion;
                        }
                        SaveManager.SaveConfig(cfg);

                        UpdateRecompStatus();
                        RefreshSaveSlots();
                        Log(Loc.Format("LogRecompDetected", selected));
                    }
                    else
                    {
                        MessageBox.Show(this, Loc.Get("MsgInvalidRecompFolder"), Loc.Get("TitleError"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (!isUserInitiated)
                {
                    Log(Loc.Get("MsgNoRecompFound"));
                }
            }
        }

        private void OpenCurrentRegionSaveFolder()
        {
            if (!string.IsNullOrEmpty(m_recompRoot))
            {
                string savesDir = SaveManager.GetRegionSavesDir(m_recompRoot, m_activeRegion);
                if (Directory.Exists(savesDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", savesDir);
                    return;
                }
            }
            MessageBox.Show(this, Loc.Get("MsgNoRecompFound"), Loc.Get("TitleNotice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Save Slots Operations
        public void RefreshSaveSlots()
        {
            m_lvSlots.Items.Clear();
            m_detectedSlots.Clear();

            if (string.IsNullOrEmpty(m_recompRoot))
            {
                m_recompRoot = SaveManager.FindRecompRoot();
            }

            if (!string.IsNullOrEmpty(m_recompRoot))
            {
                m_detectedSlots = SaveManager.ScanSlots(m_recompRoot, m_activeRegion);
            }

            foreach (var slot in m_detectedSlots)
            {
                ListViewItem lvi = new ListViewItem(string.Format("Slot {0}", slot.SlotNumber));
                lvi.SubItems.Add(slot.SlotName);
                lvi.SubItems.Add(slot.LastModified.ToString("yyyy-MM-dd HH:mm"));
                lvi.SubItems.Add(slot.Fol.ToString("N0"));
                lvi.SubItems.Add(string.Format("{0} / {1}", slot.Crc1Valid ? "OK" : "ERR", slot.Crc2Valid ? "OK" : "ERR"));
                lvi.SubItems.Add(slot.Sha256 != null && slot.Sha256.Length >= 8 ? slot.Sha256.Substring(0, 8) + "..." : "---");
                lvi.Tag = slot;
                m_lvSlots.Items.Add(lvi);
            }

            if (m_lvSlots.Items.Count > 0)
            {
                m_lvSlots.Items[0].Selected = true;
            }
            else
            {
                ClearThumbnailPreview();
            }

            Log(Loc.Format("LogScanCompleted", m_detectedSlots.Count, m_activeRegion));
        }

        private void OnSlotSelectionChanged(object sender, EventArgs e)
        {
            if (m_lvSlots.SelectedItems.Count == 0)
            {
                ClearThumbnailPreview();
                return;
            }

            SaveSlotInfo slot = (SaveSlotInfo)m_lvSlots.SelectedItems[0].Tag;
            m_lblSlotDetailTitle.Text = string.Format("{0} — {1}", slot.SlotName, slot.Region);
            m_lblSlotDetailInfo.Text = string.Format(
                "Fol: {0:N0}\nModified: {1:yyyy-MM-dd HH:mm:ss}\nCRC1: {2} | CRC2: {3}",
                slot.Fol, slot.LastModified, slot.Crc1Valid ? "OK" : "ERR", slot.Crc2Valid ? "OK" : "ERR");

            if (slot.HasThumbnail && File.Exists(slot.ThumbnailPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(slot.ThumbnailPath, FileMode.Open, FileAccess.Read))
                    {
                        m_pbThumbnail.Image = Image.FromStream(fs);
                    }
                }
                catch { ClearThumbnailPreview(); }
            }
            else
            {
                ClearThumbnailPreview();
            }
        }

        private void ClearThumbnailPreview()
        {
            m_pbThumbnail.Image = null;
            m_lblSlotDetailTitle.Text = "";
            m_lblSlotDetailInfo.Text = "";
        }

        public void OpenSelectedSlot()
        {
            if (m_lvSlots.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, Loc.Get("MsgSelectSlot"), Loc.Get("TitleNotice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveSlotInfo slot = (SaveSlotInfo)m_lvSlots.SelectedItems[0].Tag;
            LoadSavePayload(slot);
        }

        public void LoadSavePayload(SaveSlotInfo slot)
        {
            if (slot == null || !File.Exists(slot.PayloadPath)) return;

            try
            {
                SavePayload payload = SavePayload.FromFile(slot.PayloadPath);
                m_currentPayload = payload;
                m_activeSlot = slot;
                m_hasUnsavedChanges = false;

                // Tab 1 Fol
                m_numFol.Value = Math.Min(m_currentPayload.Fol, 99999999);
                m_numFol.Enabled = true;
                m_btnUpdateFol.Enabled = true;
                m_btnMaxFol.Enabled = true;
                m_btnSaveChanges.Enabled = true;

                UpdateChecksumDisplay();
                UpdateActiveBadge();

                // Tab 2 Characters
                m_lblNoSaveChars.Visible = false;
                m_pnlCharEditor.Enabled = true;
                m_selectedCharacter = m_currentPayload.GetCharacter(m_lbCharacters.SelectedIndex >= 0 ? m_lbCharacters.SelectedIndex : 0);
                DisplayCharacter(m_selectedCharacter);

                // Tab 3 Inventory
                m_lblNoSaveItems.Visible = false;
                m_pnlItemFilter.Enabled = true;
                m_dgvItems.Enabled = true;
                m_allItems = m_currentPayload.GetAllItems();
                FilterItems();

                Log(Loc.Format("LogSaveLoaded", slot.SlotName, payload.Fol, payload.Sha256Hash));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.Get("TitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("[ERROR] " + ex.Message);
            }
        }

        public void SaveCurrentSlotToFile()
        {
            if (m_currentPayload == null || m_activeSlot == null) return;

            try
            {
                // Commit any active character modifications
                if (m_selectedCharacter != null && m_pnlCharEditor.Enabled)
                {
                    CommitCurrentCharacterValues();
                }

                // Direct write with auto-backup and atomic verification
                string bkp = SaveManager.CreateBackup(m_activeSlot.PayloadPath, m_activeSlot.Region, m_activeSlot.SlotName, m_activeSlot.SlotNumber);
                Log(Loc.Format("LogAutoBackupCreated", Path.GetFileName(bkp)));

                SaveManager.SavePayloadDirect(m_currentPayload, m_activeSlot.PayloadPath, m_activeSlot.Region, m_activeSlot.SlotName, m_activeSlot.SlotNumber);

                m_hasUnsavedChanges = false;
                UpdateChecksumDisplay();
                UpdateActiveBadge();

                RefreshSaveSlots();
                RefreshBackupsList();

                Log(Loc.Format("LogSaveSaved", Path.GetFileName(bkp)));
                MessageBox.Show(this, Loc.Format("MsgSaveSuccess", Path.GetFileName(bkp)), Loc.Get("TitleSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.Get("TitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("[ERROR SAVE] " + ex.Message);
            }
        }

        private void CreateManualBackupFromSelection()
        {
            if (m_lvSlots.SelectedItems.Count == 0) return;
            SaveSlotInfo slot = (SaveSlotInfo)m_lvSlots.SelectedItems[0].Tag;

            try
            {
                string bkp = SaveManager.CreateBackup(slot.PayloadPath, slot.Region, slot.SlotName, slot.SlotNumber);
                RefreshBackupsList();
                Log(Loc.Format("LogAutoBackupCreated", Path.GetFileName(bkp)));
                MessageBox.Show(this, Loc.Format("MsgBackupCreated", Path.GetFileName(bkp)), Loc.Get("TitleBackupCreated"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.Get("TitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Characters Logic
        private void OnCharacterSelectionChanged(object sender, EventArgs e)
        {
            if (m_currentPayload == null || m_lbCharacters.SelectedIndex < 0) return;
            m_selectedCharacter = m_currentPayload.GetCharacter(m_lbCharacters.SelectedIndex);
            DisplayCharacter(m_selectedCharacter);
        }

        private void DisplayCharacter(CharacterData c)
        {
            if (c == null) return;
            m_lblCharTitle.Text = Loc.Format("CharTitleFormat", c.Name, c.Offset);
            m_cbInParty.Checked = c.InParty;
            m_cbInActiveParty.Checked = c.InActiveParty;
            m_numLevel.Value = Math.Max(1, Math.Min(c.Level, 255));
            m_numExp.Value = Math.Min(c.Exp, 99999999);
            m_numCurHp.Value = Math.Max(1, Math.Min(c.CurrentHp, 99999));
            m_numMaxHp.Value = Math.Max(1, Math.Min(c.MaxHp, 99999));
            m_numCurMp.Value = Math.Min(c.CurrentMp, 99999);
            m_numMaxMp.Value = Math.Min(c.MaxMp, 99999);
            m_numAtk.Value = Math.Max(1, Math.Min(c.Atk, 99999));
            m_numDef.Value = Math.Max(1, Math.Min(c.Def, 99999));
            m_numHit.Value = Math.Max(1, Math.Min(c.Hit, 99999));
            m_numAgl.Value = Math.Max(1, Math.Min(c.Agl, 99999));
            m_numInt.Value = Math.Max(1, Math.Min(c.Int, 99999));
            m_numAp.Value = Math.Max(0, Math.Min(c.Ap, 30000));
        }

        private void CommitCurrentCharacterValues()
        {
            if (m_selectedCharacter == null || m_currentPayload == null) return;

            m_selectedCharacter.InParty = m_cbInParty.Checked;
            m_selectedCharacter.InActiveParty = m_cbInActiveParty.Checked;
            m_selectedCharacter.Level = (uint)m_numLevel.Value;
            m_selectedCharacter.Exp = (uint)m_numExp.Value;
            m_selectedCharacter.CurrentHp = (uint)m_numCurHp.Value;
            m_selectedCharacter.MaxHp = (uint)m_numMaxHp.Value;
            m_selectedCharacter.CurrentMp = (uint)m_numCurMp.Value;
            m_selectedCharacter.MaxMp = (uint)m_numMaxMp.Value;
            m_selectedCharacter.Atk = (uint)m_numAtk.Value;
            m_selectedCharacter.Def = (uint)m_numDef.Value;
            m_selectedCharacter.Hit = (uint)m_numHit.Value;
            m_selectedCharacter.Agl = (uint)m_numAgl.Value;
            m_selectedCharacter.Int = (uint)m_numInt.Value;
            m_selectedCharacter.Ap = (int)m_numAp.Value;

            m_currentPayload.SaveCharacter(m_selectedCharacter);
            m_hasUnsavedChanges = true;
        }

        private void OnApplyCharacterClicked(object sender, EventArgs e)
        {
            if (m_currentPayload == null || m_selectedCharacter == null) return;

            CommitCurrentCharacterValues();
            UpdateChecksumDisplay();
            UpdateActiveBadge();

            Log(Loc.Format("LogCharSaved", m_selectedCharacter.Name, m_selectedCharacter.Level, m_selectedCharacter.MaxHp, m_selectedCharacter.Atk));
            MessageBox.Show(this, Loc.Format("MsgCharApplied", m_selectedCharacter.Name), Loc.Get("TitleCharUpdated"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnMaxStatsClicked(object sender, EventArgs e)
        {
            m_numCurHp.Value = 9999;
            m_numMaxHp.Value = 9999;
            m_numCurMp.Value = 9999;
            m_numMaxMp.Value = 9999;
            m_numAtk.Value = 999;
            m_numDef.Value = 999;
            m_numHit.Value = 999;
            m_numAgl.Value = 999;
            m_numInt.Value = 999;
            m_numAp.Value = 10000;
        }
        #endregion

        #region Inventory Logic
        private void FilterItems()
        {
            if (m_allItems == null) return;
            string filter = (m_txtItemSearch.Text ?? "").Trim().ToLowerInvariant();
            bool onlyOwned = m_cbItemsOnlyOwned.Checked;

            List<ItemData> filtered = new List<ItemData>();
            foreach (var item in m_allItems)
            {
                if (onlyOwned && item.Amount == 0) continue;
                if (!string.IsNullOrEmpty(filter))
                {
                    if (!item.Name.ToLowerInvariant().Contains(filter) && !item.Id.ToString().Contains(filter))
                        continue;
                }
                filtered.Add(item);
            }

            m_dgvItems.DataSource = null;
            m_dgvItems.DataSource = filtered;
        }

        private void SetCurrentItemAmount(ushort amount)
        {
            if (m_dgvItems.CurrentRow != null && m_dgvItems.CurrentRow.DataBoundItem is ItemData)
            {
                ItemData item = (ItemData)m_dgvItems.CurrentRow.DataBoundItem;
                item.Amount = amount;
                m_hasUnsavedChanges = true;
                m_dgvItems.Refresh();
            }
        }

        private void OnSetOwnedItems99Clicked(object sender, EventArgs e)
        {
            if (m_allItems == null) return;
            DialogResult dr = MessageBox.Show(this, Loc.Get("MsgConfirmOwned99"), Loc.Get("TitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                int modifiedCount = 0;
                foreach (var it in m_allItems)
                {
                    if (it.Amount > 0)
                    {
                        it.Amount = 99;
                        modifiedCount++;
                    }
                }
                m_hasUnsavedChanges = true;
                FilterItems();
                Log(Loc.Get("LogOwnedItems99"));
            }
        }

        private void OnSetAllItems99Clicked(object sender, EventArgs e)
        {
            if (m_allItems == null) return;
            DialogResult dr = MessageBox.Show(this, Loc.Get("MsgConfirmAll99"), Loc.Get("TitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
            {
                foreach (var it in m_allItems) it.Amount = 99;
                m_hasUnsavedChanges = true;
                FilterItems();
                Log(Loc.Get("LogAllItems99"));
            }
        }

        private void OnApplyInventoryClicked(object sender, EventArgs e)
        {
            if (m_currentPayload == null || m_allItems == null) return;

            m_currentPayload.SaveItems(m_allItems);
            m_hasUnsavedChanges = true;
            UpdateChecksumDisplay();
            UpdateActiveBadge();

            Log(Loc.Get("LogInventorySaved"));
            MessageBox.Show(this, Loc.Get("MsgItemsApplied"), Loc.Get("TitleInventorySaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Backups Logic
        public void RefreshBackupsList()
        {
            m_lvBackups.Items.Clear();
            List<BackupItemInfo> backups = SaveManager.ScanBackups(m_activeRegion);

            foreach (var b in backups)
            {
                ListViewItem lvi = new ListViewItem(b.FileName);
                lvi.SubItems.Add(b.Region);
                lvi.SubItems.Add(b.SlotNumber > 0 ? b.SlotNumber.ToString() : "-");
                lvi.SubItems.Add(b.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                lvi.SubItems.Add(b.Fol > 0 ? b.Fol.ToString("N0") : "---");
                lvi.SubItems.Add(string.Format("{0:N0} KB", b.FileSize / 1024));
                lvi.Tag = b;
                m_lvBackups.Items.Add(lvi);
            }

            if (m_lvBackups.Items.Count > 0)
            {
                m_lvBackups.Items[0].Selected = true;
            }
            else
            {
                m_pbBackupThumb.Image = null;
            }
        }

        private void OnBackupSelectionChanged(object sender, EventArgs e)
        {
            if (m_lvBackups.SelectedItems.Count == 0)
            {
                m_pbBackupThumb.Image = null;
                return;
            }

            BackupItemInfo b = (BackupItemInfo)m_lvBackups.SelectedItems[0].Tag;
            if (b.HasThumbnail && File.Exists(b.ThumbnailPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(b.ThumbnailPath, FileMode.Open, FileAccess.Read))
                    {
                        m_pbBackupThumb.Image = Image.FromStream(fs);
                    }
                }
                catch { m_pbBackupThumb.Image = null; }
            }
            else
            {
                m_pbBackupThumb.Image = null;
            }
        }

        private void OnRestoreBackupClicked(object sender, EventArgs e)
        {
            if (m_lvBackups.SelectedItems.Count == 0) return;
            BackupItemInfo b = (BackupItemInfo)m_lvBackups.SelectedItems[0].Tag;

            // Target slot
            if (m_activeSlot == null)
            {
                MessageBox.Show(this, Loc.Get("MsgSelectSlot"), Loc.Get("TitleNotice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult dr = MessageBox.Show(
                this,
                Loc.Format("MsgConfirmRestore", b.FileName, m_activeSlot.Region, m_activeSlot.SlotNumber),
                Loc.Get("TitleConfirm"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            try
            {
                SaveManager.RestoreBackup(b.FullPath, m_activeSlot.PayloadPath, m_activeSlot.Region, m_activeSlot.SlotName, m_activeSlot.SlotNumber);
                Log(Loc.Format("LogBackupRestored", b.FileName, m_activeSlot.SlotNumber));

                // Reload active payload
                LoadSavePayload(m_activeSlot);
                RefreshSaveSlots();
                RefreshBackupsList();

                MessageBox.Show(this, Loc.Format("MsgRestoreSuccess", m_activeSlot.SlotNumber), Loc.Get("TitleRestoreSuccess"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.Get("TitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("[ERROR RESTORE] " + ex.Message);
            }
        }
        #endregion

        #region Helpers & Logging
        public void Log(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            string ts = DateTime.Now.ToString("HH:mm:ss");
            m_txtLog.AppendText(string.Format("[{0}] {1}\r\n", ts, msg));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (m_hasUnsavedChanges)
            {
                DialogResult dr = MessageBox.Show(
                    this,
                    "You have unsaved changes in memory. Do you want to save before closing?",
                    Loc.Get("TitleConfirm"),
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    SaveCurrentSlotToFile();
                }
                else if (dr == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }
        #endregion
    }
}
