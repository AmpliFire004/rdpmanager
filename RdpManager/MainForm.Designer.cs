using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace RdpManager
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private FlowLayoutPanel flowConnections;
        private ContextMenuStrip mainMenu;
        private ToolStripMenuItem addMenuItem;
        private ToolStripMenuItem importMenuItem;
        private ToolStripMenuItem exportMenuItem;
        private ToolStripMenuItem connectMenuItem;
        private ToolStripMenuItem editMenuItem;
        private ToolStripMenuItem copyMenuItem;
        private ToolStripMenuItem removeMenuItem;
        private ListView lvConnections;
        private ColumnHeader colName;
        private ColumnHeader colHost;
        private ContextMenuStrip listMenu;
        private MenuStrip menuStrip;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuEdit;
        private ToolStripMenuItem menuView;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem miHelpAbout;
        private ToolStripMenuItem miFileAdd;
        private ToolStripMenuItem miFileQuickConnect;
        private ToolStripMenuItem miFileImport;
        private ToolStripMenuItem miFileExport;
        private ToolStripMenuItem miViewButtons;
        private ToolStripMenuItem miViewList;
        private ToolStripMenuItem miViewSort;
        private ToolStripMenuItem miSortNameAsc;
        private ToolStripMenuItem miSortNameDesc;
        private ToolStripMenuItem miSortHostAsc;
        private ToolStripMenuItem miSortHostDesc;
        private Panel contentPanel;
        private Panel connectionsPanel;
        private FlowLayoutPanel quickConnectPanel;
        
        private ComboBox quickConnectTextBox;
        private Button quickConnectButton;
        private Label quickConnectHeader;
        private Label connectionListHeader;

        /// <summary>
        ///  Clean up any resources being used.
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

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.Text = "RDP Manager";
            this.Width = 640;
            this.Height = 480;
            // Prefer the embedded icon so published single-file builds retain it.
            using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RdpManager.Assets.AppIcon.ico");
            if (iconStream != null)
            {
                this.Icon = new Icon(iconStream);
            }
            else
            {
                var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    this.Icon = appIcon;
                }
            }

            // Main context menu (right-click on empty space)
            mainMenu = new ContextMenuStrip();
            addMenuItem = new ToolStripMenuItem("Add Connection");
            addMenuItem.Click += btnAdd_Click;
            connectMenuItem = new ToolStripMenuItem("Connect");
            connectMenuItem.Click += (s, e) => ConnectSelectedIfAny();
            editMenuItem = new ToolStripMenuItem("Edit");
            editMenuItem.Click += (s, e) => EditSelectedIfAny();
            copyMenuItem = new ToolStripMenuItem("Copy Connection");
            copyMenuItem.Click += (s, e) => CopySelectedIfAny();
            removeMenuItem = new ToolStripMenuItem("Remove");
            removeMenuItem.Click += (s, e) => RemoveSelectedIfAny();
            exportMenuItem = new ToolStripMenuItem("Export Connections...");
            exportMenuItem.Click += (s, e) => ExportConnections();
            importMenuItem = new ToolStripMenuItem("Import Connections...");
            importMenuItem.Click += (s, e) => ImportConnections();
            mainMenu.Items.AddRange(new ToolStripItem[] {
                addMenuItem,
                connectMenuItem,
                editMenuItem,
                copyMenuItem,
                removeMenuItem,
                new ToolStripSeparator(),
                exportMenuItem,
                importMenuItem
            });
            this.ContextMenuStrip = mainMenu;

            // Menu bar (File/View)
            menuStrip = new MenuStrip();
            // Ensure menu docks to the top of the window
            menuStrip.Dock = DockStyle.Top;
            // Make menu background match the rest of the app
            menuStrip.BackColor = SystemColors.Window;
            try { menuStrip.RenderMode = ToolStripRenderMode.System; } catch { }
            menuFile = new ToolStripMenuItem("File");
            menuEdit = new ToolStripMenuItem("Edit");
            menuView = new ToolStripMenuItem("View");

            miFileAdd = new ToolStripMenuItem("Add Connection");
            miFileAdd.ShortcutKeys = Keys.Control | Keys.N;
            miFileAdd.ShowShortcutKeys = true;
            miFileAdd.Click += btnAdd_Click;
            miFileQuickConnect = new ToolStripMenuItem("Quick Connect");
            miFileQuickConnect.ShortcutKeys = Keys.Control | Keys.Q;
            miFileQuickConnect.ShowShortcutKeys = true;
            miFileQuickConnect.Click += (s, e) =>
            {
                try { quickConnectTextBox.Focus(); quickConnectTextBox.SelectAll(); } catch { }
            };
            miFileImport = new ToolStripMenuItem("Import Connections...");
            miFileImport.Click += (s, e) => ImportConnections();
            miFileExport = new ToolStripMenuItem("Export Connections...");
            miFileExport.Click += (s, e) => ExportConnections();
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { miFileAdd, miFileQuickConnect, new ToolStripSeparator(), miFileImport, miFileExport });

            // Edit menu
            var miEditQuickConnectSettings = new ToolStripMenuItem("Quick Connect Settings...");
            miEditQuickConnectSettings.Click += (s, e) => ShowQuickConnectSettings();
            var miEditClearQuickConnectHistory = new ToolStripMenuItem("Clear Quick Connect History");
            miEditClearQuickConnectHistory.Click += (s, e) => ClearQuickConnectHistory();
            menuEdit.DropDownItems.Add(miEditQuickConnectSettings);
            menuEdit.DropDownItems.Add(miEditClearQuickConnectHistory);

            miViewButtons = new ToolStripMenuItem("Buttons View");
            miViewButtons.Click += (s, e) => ToggleView(isList: false);
            miViewList = new ToolStripMenuItem("List View");
            miViewList.Click += (s, e) => ToggleView(isList: true);
            miViewSort = new ToolStripMenuItem("Sort");
            miSortNameAsc = new ToolStripMenuItem("Name ▲"); miSortNameAsc.Click += (s, e) => SetSort("Name", true);
            miSortNameDesc = new ToolStripMenuItem("Name ▼"); miSortNameDesc.Click += (s, e) => SetSort("Name", false);
            miSortHostAsc = new ToolStripMenuItem("Host ▲"); miSortHostAsc.Click += (s, e) => SetSort("Host", true);
            miSortHostDesc = new ToolStripMenuItem("Host ▼"); miSortHostDesc.Click += (s, e) => SetSort("Host", false);
            miViewSort.DropDownItems.AddRange(new ToolStripItem[] { miSortNameAsc, miSortNameDesc, miSortHostAsc, miSortHostDesc });
            menuView.DropDownItems.AddRange(new ToolStripItem[] { miViewButtons, miViewList, new ToolStripSeparator(), miViewSort });

            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuEdit, menuView });
            // Help menu (About)
            menuHelp = new ToolStripMenuItem("Help");
            miHelpAbout = new ToolStripMenuItem("About");
            miHelpAbout.Click += (s, e) => { try { ShowAbout(); } catch { } };
            menuHelp.DropDownItems.Add(miHelpAbout);
            menuStrip.Items.Add(menuHelp);
            this.MainMenuStrip = menuStrip;

            flowConnections = new FlowLayoutPanel();
            flowConnections.Dock = DockStyle.Fill;
            flowConnections.AutoScroll = true;
            flowConnections.Padding = new Padding(12);
            flowConnections.WrapContents = true;
            flowConnections.ContextMenuStrip = mainMenu;
            flowConnections.Visible = false;
            flowConnections.BackColor = SystemColors.Window;

            lvConnections = new ListView();
            lvConnections.Dock = DockStyle.Fill;
            lvConnections.View = View.Details;
            lvConnections.FullRowSelect = true;
            lvConnections.HideSelection = false;
            lvConnections.Visible = true;
            lvConnections.BackColor = SystemColors.Window;
            colName = new ColumnHeader(); colName.Text = "Name"; colName.Width = 220;
            colHost = new ColumnHeader(); colHost.Text = "Hostname"; colHost.Width = 320;
            lvConnections.Columns.AddRange(new ColumnHeader[] { colName, colHost });
            lvConnections.ColumnClick += (s, e) => ListView_ColumnClick(e.Column);
            lvConnections.ItemActivate += (s, e) => ConnectSelectedFromList();
            lvConnections.SizeChanged += (s, e) => LayoutListColumns();

            listMenu = new ContextMenuStrip();
            var listAdd = new ToolStripMenuItem("Add Connection"); listAdd.Click += btnAdd_Click;
            var listConnect = new ToolStripMenuItem("Connect"); listConnect.Click += (s, e) => ConnectSelectedFromList();
            var listEdit = new ToolStripMenuItem("Edit"); listEdit.Click += (s, e) => EditSelectedFromList();
            var listCopy = new ToolStripMenuItem("Copy Connection"); listCopy.Click += (s, e) => CopySelectedFromList();
            var listRemove = new ToolStripMenuItem("Remove"); listRemove.Click += (s, e) => RemoveSelectedFromList();
            var listExport = new ToolStripMenuItem("Export Connections..."); listExport.Click += (s, e) => ExportConnections();
            var listImport = new ToolStripMenuItem("Import Connections..."); listImport.Click += (s, e) => ImportConnections();
            listMenu.Items.AddRange(new ToolStripItem[] {
                listAdd,
                listConnect,
                listEdit,
                listCopy,
                listRemove,
                new ToolStripSeparator(),
                listExport,
                listImport
            });
            lvConnections.ContextMenuStrip = listMenu;

            quickConnectPanel = new FlowLayoutPanel();
            quickConnectPanel.Dock = DockStyle.Top;
            quickConnectPanel.Padding = new Padding(12, 2, 12, 0);
            quickConnectPanel.Height = 36;
            quickConnectPanel.WrapContents = false;
            quickConnectPanel.AutoSize = false;
            quickConnectPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            quickConnectPanel.FlowDirection = FlowDirection.LeftToRight;
            // match the content background so headers/panel don't appear as a different band
            quickConnectPanel.BackColor = SystemColors.Window;

            quickConnectButton = new Button();
            quickConnectButton.Text = "Go";
            quickConnectButton.AutoSize = true;
            quickConnectButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            quickConnectButton.Margin = new Padding(0);
            quickConnectButton.Padding = new Padding(10, 2, 10, 2);
            quickConnectButton.Click += (s, e) => QuickConnect();

            quickConnectTextBox = new ComboBox();
            quickConnectTextBox.Width = 240;
            quickConnectTextBox.Margin = new Padding(0, 4, 8, 0);
            quickConnectTextBox.DropDownStyle = ComboBoxStyle.DropDown;
            quickConnectTextBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            quickConnectTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            quickConnectTextBox.Enter += QuickConnectTextBox_Enter;
            quickConnectTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    QuickConnect();
                }
            };

            quickConnectPanel.Controls.Add(quickConnectTextBox);
            quickConnectPanel.Controls.Add(quickConnectButton);

            // Place header above the quick-connect panel (header is created below)

            // Ensure menu is at the top like a normal window
            // Content panel to add spacing below the menu bar
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(12, 10, 12, 12);
            contentPanel.BackColor = SystemColors.Window;

            connectionListHeader = new Label();
            connectionListHeader.Text = "Connection List";
            connectionListHeader.Dock = DockStyle.Top;
            connectionListHeader.Padding = new Padding(0, 2, 0, 4);
            connectionListHeader.Font = new Font(Font, FontStyle.Bold);
            connectionListHeader.BackColor = SystemColors.Window;

            // Quick connect header (matches connection list header style)
            quickConnectHeader = new Label();
            quickConnectHeader.Text = "Quick Connect";
            quickConnectHeader.Dock = DockStyle.Top;
            quickConnectHeader.Padding = new Padding(12, 4, 0, 4);
            quickConnectHeader.Font = new Font(Font, FontStyle.Bold);
            quickConnectHeader.BackColor = SystemColors.Window;

            connectionsPanel = new Panel();
            connectionsPanel.Dock = DockStyle.Fill;
            connectionsPanel.Padding = new Padding(0, 8, 0, 0); // breathing room under header
            connectionsPanel.BackColor = SystemColors.Window;

            // Add views into content panel
            connectionsPanel.Controls.Add(lvConnections);
            connectionsPanel.Controls.Add(flowConnections);
            contentPanel.Controls.Add(connectionsPanel);

            // Use a TableLayoutPanel to enforce exact stacking order and sizing
            var mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.BackColor = SystemColors.Window;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 5;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // menu
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // quick connect header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // quick connect panel
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // list header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // content

            // Add controls to the layout in the requested order
            mainLayout.Controls.Add(menuStrip, 0, 0);
            mainLayout.Controls.Add(quickConnectHeader, 0, 1);
            mainLayout.Controls.Add(quickConnectPanel, 0, 2);
            mainLayout.Controls.Add(connectionListHeader, 0, 3);
            mainLayout.Controls.Add(contentPanel, 0, 4);

            this.Controls.Add(mainLayout);
        }

        #endregion
    }
}
