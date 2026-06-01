using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using RdpManager.Models;
using RdpManager.Services;

namespace RdpManager
{
    public partial class MainForm : Form
    {
        // P/Invoke to hide/show scrollbars when necessary
        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private readonly ConnectionStore _store = new ConnectionStore();
        private readonly SettingsStore _settingsStore = new SettingsStore();
        private List<Connection> _connections = new();
        private string _sortField = "Name"; // or "Host"
        private bool _sortAsc = true;
        private UserSettings _settings = new UserSettings();
        private readonly AutoCompleteStringCollection _quickConnectAutoComplete = new AutoCompleteStringCollection();

        public MainForm()
        {
            InitializeComponent();
            // Load persisted settings
            _settings = _settingsStore.Load();
            // Apply initial column widths (saved or percentage fallback)
            try { ApplyInitialColumnWidths(); } catch { }
            _settings.QuickConnectHistory ??= new List<string>();
            if (quickConnectTextBox != null)
            {
                quickConnectTextBox.AutoCompleteCustomSource = _quickConnectAutoComplete;
            }
            UpdateQuickConnectAutocomplete();
            _sortField = string.Equals(_settings.SortField, "Host", StringComparison.OrdinalIgnoreCase) ? "Host" : "Name";
            _sortAsc = _settings.SortAsc;

            LoadConnections();
            InitializeTabs();
            // Re-layout when the container is resized
            try { lvConnections.SizeChanged += (s, e) => LayoutListColumns(); } catch { }
            UpdateSortMenuChecks();
            ToggleView(true);
            // Persist column widths on close in case the ColumnWidthChanged event wasn't fired
            try { this.FormClosing += MainForm_FormClosing; } catch { }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // Save whatever the current column widths are and the auto-layout preference
                if (colName != null)
                {
                    _settings.ColumnWidthName = colName.Width;
                }
                if (colHost != null)
                {
                    _settings.ColumnWidthHost = colHost.Width;
                }
                if (colDescription != null)
                {
                    _settings.ColumnWidthDescription = colDescription.Width;
                }
                _settings.AutoLayoutColumns = _autoLayoutColumns;
                SaveSettings();
            }
            catch { }
        }

        private void LoadConnections()
        {
            _connections = _store.Load();
            // Migrate existing connections to have a tab name
            foreach (var connection in _connections)
            {
                if (string.IsNullOrEmpty(connection.TabName))
                {
                    connection.TabName = "General";
                }
            }
        }

        private void InitializeTabs()
        {
            // Ensure we have at least one tab
            if (_settings.Tabs == null || _settings.Tabs.Count == 0)
            {
                _settings.Tabs = new List<string> { "General" };
                _settings.SelectedTab = "General";
            }

            // Create tabs
            tabControl.TabPages.Clear();
            foreach (var tabName in _settings.Tabs)
            {
                var tabPage = new TabPage(tabName);
                // Create a new panel for each tab to hold the views
                var tabPanel = new Panel();
                tabPanel.Dock = DockStyle.Fill;
                // Add horizontal padding so the connections list has space on left/right
                tabPanel.Padding = new Padding(12, 8, 12, 0);
                tabPanel.BackColor = SystemColors.Window;

                // Don't add controls here - they will be added when the tab is selected
                tabPage.Controls.Add(tabPanel);
                tabControl.TabPages.Add(tabPage);
            }

            // Select the saved tab
            var selectedIndex = _settings.Tabs.IndexOf(_settings.SelectedTab);
            if (selectedIndex >= 0)
            {
                tabControl.SelectedIndex = selectedIndex;
            }
            else
            {
                tabControl.SelectedIndex = 0;
                _settings.SelectedTab = _settings.Tabs[0];
            }

            // Update the view for the selected tab
            UpdateCurrentTabView();
        }

        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                _settings.SelectedTab = tabControl.SelectedTab.Text;
                SaveSettings();
                UpdateCurrentTabView();
                RenderConnections();
                // Ensure layout is updated for the newly visible tab
                LayoutListColumns();
            }
        }

        private void AddTab()
        {
            using var inputDialog = new Form();
            inputDialog.Text = "Add Tab";
            inputDialog.Width = 300;
            inputDialog.Height = 150;
            inputDialog.StartPosition = FormStartPosition.CenterParent;

            var label = new Label();
            label.Text = "Tab Name:";
            label.Location = new Point(10, 20);
            label.AutoSize = true;

            var textBox = new TextBox();
            textBox.Location = new Point(10, 40);
            textBox.Width = 260;

            var okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(110, 70);
            okButton.DialogResult = DialogResult.OK;

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(190, 70);
            cancelButton.DialogResult = DialogResult.Cancel;

            inputDialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            inputDialog.AcceptButton = okButton;
            inputDialog.CancelButton = cancelButton;

            if (inputDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var tabName = textBox.Text.Trim();
                if (!_settings.Tabs.Contains(tabName))
                {
                    _settings.Tabs.Add(tabName);
                    _settings.SelectedTab = tabName; // Select the new tab
                    SaveSettings();
                    InitializeTabs();
                }
                else
                {
                    MessageBox.Show(this, "A tab with this name already exists.", "Duplicate Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void RemoveTab()
        {
            if (_settings.Tabs.Count <= 1)
            {
                MessageBox.Show(this, "Cannot remove the last tab.", "Cannot Remove Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var currentTab = _settings.SelectedTab;
            var confirm = MessageBox.Show(
                this,
                $"Remove tab '{currentTab}' and all its connections?",
                "Confirm Remove Tab",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                // Remove all connections in this tab
                _connections.RemoveAll(c => c.TabName == currentTab);

                // Remove the tab
                _settings.Tabs.Remove(currentTab);

                // Select the first remaining tab
                _settings.SelectedTab = _settings.Tabs[0];

                SaveSettings();
                SaveConnections();
                InitializeTabs();
            }
        }

        private void RenameTab()
        {
            var currentTab = _settings.SelectedTab;
            using var inputDialog = new Form();
            inputDialog.Text = "Rename Tab";
            inputDialog.Width = 300;
            inputDialog.Height = 150;
            inputDialog.StartPosition = FormStartPosition.CenterParent;

            var label = new Label();
            label.Text = "New tab name:";
            label.Location = new Point(10, 20);
            label.AutoSize = true;

            var textBox = new TextBox();
            textBox.Location = new Point(10, 40);
            textBox.Width = 260;
            textBox.Text = currentTab;

            var okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(110, 70);
            okButton.DialogResult = DialogResult.OK;

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(190, 70);
            cancelButton.DialogResult = DialogResult.Cancel;

            inputDialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            inputDialog.AcceptButton = okButton;
            inputDialog.CancelButton = cancelButton;

            if (inputDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var newTabName = textBox.Text.Trim();
                if (newTabName == currentTab)
                {
                    return; // No change
                }

                if (_settings.Tabs.Contains(newTabName))
                {
                    MessageBox.Show(this, "A tab with this name already exists.", "Duplicate Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Update the tab name in settings
                var tabIndex = _settings.Tabs.IndexOf(currentTab);
                _settings.Tabs[tabIndex] = newTabName;
                _settings.SelectedTab = newTabName;

                // Update all connections that belong to this tab
                foreach (var connection in _connections)
                {
                    if (connection.TabName == currentTab)
                    {
                        connection.TabName = newTabName;
                    }
                }

                SaveSettings();
                SaveConnections();
                InitializeTabs();
            }
        }

        private void SaveConnections()
        {
            _store.Save(_connections);
        }

        private void RenderConnections()
        {
            var allConnections = _connections.Where(c => c.TabName == _settings.SelectedTab).ToList();
            var list = GetSorted(allConnections);
            lvConnections.BeginUpdate();
            try
            {
                lvConnections.Items.Clear();
                foreach (var c in list)
                {
                    var lvi = new ListViewItem(c.Name);
                    lvi.SubItems.Add(FormatHost(c));
                    lvi.SubItems.Add(c.Description ?? string.Empty);
                    lvi.Tag = c;
                    lvConnections.Items.Add(lvi);
                }
            }
            finally
            {
                lvConnections.EndUpdate();
            }
            LayoutListColumns();
        }

        private void btnAdd_Click(object? sender, EventArgs e)
        {
            using var dlg = new AddConnectionForm();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.NewConnection != null)
            {
                dlg.NewConnection.TabName = _settings.SelectedTab;
                _connections.Add(dlg.NewConnection);
                SaveConnections();
                RenderConnections();
            }
        }

        private void RemoveConnection(Connection c)
        {
            var confirm = MessageBox.Show(
                this,
                $"Remove connection '{c.Name}'?",
                "Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _connections.RemoveAll(x => x.Id == c.Id);
                SaveConnections();
                RenderConnections();
            }
        }

        private void EditConnection(Connection c)
        {
            using var dlg = new AddConnectionForm(c);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.NewConnection != null)
            {
                // Copy values back to existing connection
                c.Name = dlg.NewConnection.Name;
                c.Address = dlg.NewConnection.Address;
                c.Port = dlg.NewConnection.Port;
                c.Domain = dlg.NewConnection.Domain;
                c.Username = dlg.NewConnection.Username;
                c.Description = dlg.NewConnection.Description;
                c.ScreenWidth = dlg.NewConnection.ScreenWidth;
                c.ScreenHeight = dlg.NewConnection.ScreenHeight;

                SaveConnections();
                RenderConnections();
            }
        }

        private void CopyConnection(Connection source)
        {
            if (source == null) return;

            var clone = new Connection
            {
                Name = BuildCopyName(source.Name),
                Address = source.Address,
                Description = source.Description,
                Port = source.Port,
                Domain = source.Domain,
                Username = source.Username,
                ScreenWidth = source.ScreenWidth,
                ScreenHeight = source.ScreenHeight,
                TabName = source.TabName // Keep the same tab
            };

            _connections.Add(clone);
            SaveConnections();
            RenderConnections();
        }

        private string BuildCopyName(string original)
        {
            var baseName = string.IsNullOrWhiteSpace(original) ? "Connection" : original.Trim();
            var candidate = $"{baseName} (Copy)";
            int suffix = 2;

            while (_connections.Any(c => string.Equals(c.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = $"{baseName} (Copy {suffix})";
                suffix++;
            }

            return candidate;
        }

        private void ExportConnections()
        {
            try
            {
                using var dlg = new SaveFileDialog
                {
                    Title = "Export Connections and Tabs",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = "rdpmanager_backup.json",
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var exportData = new ExportData
                    {
                        Connections = _connections,
                        Tabs = _settings.Tabs,
                        SelectedTab = _settings.SelectedTab
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(dlg.FileName, json);
                    MessageBox.Show(this, "Connections and tabs exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportConnections()
        {
            try
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "Import Connections and Tabs",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Multiselect = false
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var json = System.IO.File.ReadAllText(dlg.FileName);

                    // Try to import as new format (with tabs)
                    ExportData? importData = null;
                    try
                    {
                        importData = System.Text.Json.JsonSerializer.Deserialize<ExportData>(json);
                    }
                    catch
                    {
                        // Fall back to old format (connections only)
                        var connections = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Connection>>(json) ?? new System.Collections.Generic.List<Connection>();
                        importData = new ExportData { Connections = connections };
                    }

                    if (importData == null || importData.Connections.Count == 0)
                    {
                        MessageBox.Show(this, "No connections found in the file.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var replaceConnections = MessageBox.Show(this,
                        $"Import {importData.Connections.Count} connections?\nChoose Yes to replace existing connections, No to merge.",
                        "Import Connections",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (replaceConnections == DialogResult.Cancel) return;

                    var replaceTabs = DialogResult.No;
                    if (importData.Tabs.Count > 1 || importData.Tabs[0] != "General")
                    {
                        replaceTabs = MessageBox.Show(this,
                            $"Import {importData.Tabs.Count} tabs (including '{importData.SelectedTab}')?\nChoose Yes to replace existing tabs, No to keep current tabs.",
                            "Import Tabs",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                    }

                    if (replaceConnections == DialogResult.Yes)
                    {
                        _connections = importData.Connections;
                    }
                    else
                    {
                        // Merge connections
                        foreach (var conn in importData.Connections)
                        {
                            // Avoid duplicates by name and address
                            if (!_connections.Any(c => c.Name == conn.Name && c.Address == conn.Address))
                            {
                                _connections.Add(conn);
                            }
                        }
                    }

                    if (replaceTabs == DialogResult.Yes)
                    {
                        _settings.Tabs = importData.Tabs;
                        _settings.SelectedTab = importData.SelectedTab;
                        SaveSettings();
                        InitializeTabs();
                    }

                    SaveConnections();
                    RenderConnections();
                    MessageBox.Show(this, "Import completed.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchRdp(Connection c)
        {
            try
            {
                if (TryLaunchEmbeddedRdp(c))
                {
                    return;
                }

                LaunchRdpWithMstsc(c);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to start RDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TryLaunchEmbeddedRdp(Connection c)
        {
            try
            {
                var sessionForm = new RdpSessionForm(c, () => LaunchRdpWithMstsc(c));
                sessionForm.Show(this);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LaunchRdpWithMstsc(Connection c)
        {
            var host = c.Address;
            if (c.Port.HasValue && c.Port.Value > 0)
            {
                host = $"{host}:{c.Port.Value}";
            }

            // Build .rdp file content to support username/domain and resolution
            var lines = new System.Collections.Generic.List<string>();
            lines.Add($"full address:s:{host}");

            if (!string.IsNullOrWhiteSpace(c.Username))
            {
                var user = c.Username!;
                if (!string.IsNullOrWhiteSpace(c.Domain))
                {
                    user = $"{c.Domain}\\{user}";
                }
                lines.Add($"username:s:{user}");
            }

            // Hint to prompt for credentials when needed
            lines.Add("prompt for credentials:i:1");

            var tempDir = System.IO.Path.GetTempPath();
            var safeName = SanitizeFileName(string.IsNullOrWhiteSpace(c.Name) ? "RdpManager" : c.Name);
            var rdpPath = System.IO.Path.Combine(tempDir, $"{safeName}.rdp");
            System.IO.File.WriteAllLines(rdpPath, lines);

            var psi = new ProcessStartInfo
            {
                FileName = "mstsc.exe",
                Arguments = $"\"{rdpPath}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        

        private void QuickConnect()
        {
            var rawInput = quickConnectTextBox?.Text ?? string.Empty;
            var input = rawInput.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show(this, "Enter a hostname or IP to quick connect.", "Quick Connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                try { quickConnectTextBox?.Focus(); } catch { }
                return;
            }

            var parsed = TrySplitHostPort(input, out var hostOnly, out var port);
            var targetHost = parsed ? hostOnly : input;

            var tempConnection = new Connection
            {
                Name = targetHost,
                Address = targetHost,
                Port = port
            };
            // Apply quick-connect defaults from user settings (username, resolution)
            try
            {
                if (!string.IsNullOrWhiteSpace(_settings.QuickConnectUsername))
                {
                    tempConnection.Username = _settings.QuickConnectUsername;
                }

                if (_settings.QuickConnectScreenWidth.HasValue && _settings.QuickConnectScreenHeight.HasValue
                    && _settings.QuickConnectScreenWidth.Value > 0 && _settings.QuickConnectScreenHeight.Value > 0)
                {
                    tempConnection.ScreenWidth = _settings.QuickConnectScreenWidth.Value;
                    tempConnection.ScreenHeight = _settings.QuickConnectScreenHeight.Value;
                }
            }
            catch { }

                LaunchRdp(tempConnection);
            RememberQuickConnectEntry(input);
            try { quickConnectTextBox?.SelectAll(); } catch { }
        }

        // Opens the Quick Connect Settings dialog and persists changes
        private void ShowQuickConnectSettings()
        {
            try
            {
                using var dlg = new QuickConnectSettingsForm();
                dlg.LoadSettings(_settings.QuickConnectUsername, _settings.QuickConnectScreenWidth, _settings.QuickConnectScreenHeight);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _settings.QuickConnectUsername = dlg.Username;
                    _settings.QuickConnectScreenWidth = dlg.ScreenWidth;
                    _settings.QuickConnectScreenHeight = dlg.ScreenHeight;
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Quick Connect Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Opens the Manage Connections dialog
        private void ShowManageConnections()
        {
            try
            {
                using var dlg = new ManageConnectionsForm(_connections, _settings.Tabs, () =>
                {
                    SaveConnections();
                    SaveSettings();
                    InitializeTabs();
                    RenderConnections();
                });
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Manage Connections", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Opens the Import from Active Directory dialog
        private void ShowImportFromAD()
        {
            try
            {
                using var dlg = new ImportFromADForm(_settings.Tabs, (connections) =>
                {
                    // Add imported connections, avoiding duplicates
                    foreach (var conn in connections)
                    {
                        if (!_connections.Any(c => c.Name == conn.Name && c.Address == conn.Address))
                        {
                            _connections.Add(conn);
                        }
                    }
                    SaveConnections();

                    // If the import introduced or used a tab, persist tabs and select the imported tab
                    try
                    {
                        if (connections.Count > 0 && !string.IsNullOrWhiteSpace(connections[0].TabName))
                        {
                            _settings.SelectedTab = connections[0].TabName;
                        }
                        // Persist any changes to the shared tabs list
                        SaveSettings();
                        InitializeTabs();
                    }
                    catch { }

                    RenderConnections();
                    MessageBox.Show(this, $"Imported {connections.Count} computer(s) from Active Directory.", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Import from Active Directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Show About dialog
        private void ShowAbout()
        {
            try
            {
                using var dlg = new AboutForm();
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "About", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Clear persisted quick connect history after confirmation
        private void ClearQuickConnectHistory()
        {
            try
            {
                if (_settings.QuickConnectHistory == null || _settings.QuickConnectHistory.Count == 0)
                {
                    MessageBox.Show(this, "No quick connect history to clear.", "Clear Quick Connect History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show(this,
                    "Clear quick connect history? This cannot be undone.",
                    "Clear Quick Connect History",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                _settings.QuickConnectHistory.Clear();
                SaveSettings();
                UpdateQuickConnectAutocomplete();
                MessageBox.Show(this, "Quick connect history cleared.", "Clear Quick Connect History", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Clear Quick Connect History", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RememberQuickConnectEntry(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;
            entry = entry.Trim();
            _settings.QuickConnectHistory ??= new List<string>();
            _settings.QuickConnectHistory.RemoveAll(h => string.Equals(h, entry, StringComparison.OrdinalIgnoreCase));
            _settings.QuickConnectHistory.Insert(0, entry);
            if (_settings.QuickConnectHistory.Count > 10)
            {
                _settings.QuickConnectHistory.RemoveRange(10, _settings.QuickConnectHistory.Count - 10);
            }
            SaveSettings();
            UpdateQuickConnectAutocomplete();
        }

        private void UpdateQuickConnectAutocomplete()
        {
            if (quickConnectTextBox == null) return;
            // Always clear the ComboBox items and autocomplete source so clearing
            // the history properly empties the control.
            quickConnectTextBox.Items.Clear();
            quickConnectTextBox.Text = string.Empty;
            _quickConnectAutoComplete.Clear();
            if (_settings.QuickConnectHistory == null || _settings.QuickConnectHistory.Count == 0) return;
            foreach (var entry in _settings.QuickConnectHistory.Take(10))
            {
                _quickConnectAutoComplete.Add(entry);
                quickConnectTextBox.Items.Add(entry);
            }
        }

        // Reset column widths to the default proportional layout and re-enable auto-layout
        private void ResetColumnWidths(object? sender, EventArgs e)
        {
            _autoLayoutColumns = true;
            try
            {
                // Clear persisted widths so defaults are used
                _settings.ColumnWidthName = null;
                _settings.ColumnWidthHost = null;
                _settings.ColumnWidthDescription = null;
                _settings.AutoLayoutColumns = true;
                SaveSettings();
            }
            catch { }
            LayoutListColumns();
            try { MessageBox.Show(this, "Column widths reset to defaults.", "Reset Columns", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { }
        }

        // When true, the app will auto-layout column widths. If the user manually
        // resizes a column, auto-layout is disabled until the user resets it.
        private bool _autoLayoutColumns = true;

        private void OnConnectionColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            // User adjusted a column width; stop automatic layout so their preference sticks
            _autoLayoutColumns = false;
            try
            {
                // Persist the user's chosen widths
                _settings.ColumnWidthName = colName.Width;
                _settings.ColumnWidthHost = colHost.Width;
                _settings.ColumnWidthDescription = colDescription.Width;
                _settings.AutoLayoutColumns = false;
                SaveSettings();
            }
            catch { }
        }

        private static bool TrySplitHostPort(string input, out string host, out int? port)
        {
            host = input;
            port = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();

            if (input.StartsWith("[", StringComparison.Ordinal))
            {
                var closing = input.IndexOf(']');
                if (closing > 0 && closing < input.Length - 2 && input[closing + 1] == ':')
                {
                    var portPart = input[(closing + 2)..];
                    if (int.TryParse(portPart, out var ipv6Port) && ipv6Port > 0 && ipv6Port <= 65535)
                    {
                        host = input.Substring(1, closing - 1);
                        port = ipv6Port;
                        return true;
                    }
                }
                return false;
            }

            var colonCount = input.Count(ch => ch == ':');
            if (colonCount == 1)
            {
                var idx = input.LastIndexOf(':');
                if (idx > 0 && idx < input.Length - 1)
                {
                    var hostPart = input[..idx];
                    var portPart = input[(idx + 1)..];
                    if (int.TryParse(portPart, out var parsedPort) && parsedPort > 0 && parsedPort <= 65535)
                    {
                        host = hostPart;
                        port = parsedPort;
                        return true;
                    }
                }
            }

            return false;
        }

        private void QuickConnectTextBox_Enter(object? sender, EventArgs e)
        {
            if (quickConnectTextBox == null) return;
            try
            {
                quickConnectTextBox.SelectAll();
                // Show the dropdown after focus settles so user sees history items
                BeginInvoke(new Action(() => { try { quickConnectTextBox.DroppedDown = true; } catch { } }));
            }
            catch { }
        }
        

        private void ToggleView(bool isList)
        {
            // Always use list view
            lvConnections.Visible = true;
            // update menu checks if menu exists
            try { miViewList.Checked = true; } catch { }
            // persist
            SaveSettings();
            RenderConnections();
            LayoutListColumns();
        }

        private void SetSort(string field, bool asc)
        {
            _sortField = field;
            _sortAsc = asc;
            UpdateSortMenuChecks();
            // persist
            _settings.SortField = _sortField;
            _settings.SortAsc = _sortAsc;
            SaveSettings();
            RenderConnections();
        }

        private void ListView_ColumnClick(int columnIndex)
        {
            var newField = columnIndex == 0 ? "Name" : "Host";
            if (_sortField == newField)
            {
                _sortAsc = !_sortAsc;
            }
            else
            {
                _sortField = newField;
                _sortAsc = true;
            }
            UpdateSortMenuChecks();
            RenderConnections();
        }

        private void UpdateSortMenuChecks()
        {
            try
            {
                miSortNameAsc.Checked = _sortField == "Name" && _sortAsc;
                miSortNameDesc.Checked = _sortField == "Name" && !_sortAsc;
                miSortHostAsc.Checked = _sortField == "Host" && _sortAsc;
                miSortHostDesc.Checked = _sortField == "Host" && !_sortAsc;
            }
            catch { }
        }

        private void SaveSettings()
        {
            try { _settingsStore.Save(_settings); } catch { }
        }

        private void ToggleViewMode(bool isList)
        {
            // Always use list view - no toggling needed
            SaveSettings();

            // Update menu checks
            try { miViewList.Checked = true; } catch { }

            // Update the current tab's panel
            UpdateCurrentTabView();
            RenderConnections();
        }

        private void UpdateCurrentTabView()
        {
            if (tabControl.SelectedTab != null && tabControl.SelectedTab.Controls.Count > 0)
            {
                var tabPanel = (Panel)tabControl.SelectedTab.Controls[0];
                tabPanel.Controls.Clear();
                tabPanel.Controls.Add(lvConnections);
            }
        }

        private List<Connection> GetSorted(List<Connection> src)
        {
            IEnumerable<Connection> q;
            if (_sortField == "Host")
            {
                q = _sortAsc
                    ? src.OrderBy(c => c.Address, StringComparer.OrdinalIgnoreCase)
                    : src.OrderByDescending(c => c.Address, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                q = _sortAsc
                    ? src.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    : src.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase);
            }
            return q.ToList();
        }

        private string FormatHost(Connection c)
        {
            return c.Address;
        }

        private void EditSelectedFromList()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            EditConnection(c);
        }

        private void CopySelectedFromList()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            CopyConnection(c);
        }

        private void RemoveSelectedFromList()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            RemoveConnection(c);
        }

        private void ConnectSelectedFromList()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            LaunchRdp(c);
        }

        // Context actions for generic surface menu (operate on selected row in list view)
        private void ConnectSelectedIfAny()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            LaunchRdp(c);
        }

        private void EditSelectedIfAny()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            EditConnection(c);
        }

        private void CopySelectedIfAny()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            CopyConnection(c);
        }

        private void RemoveSelectedIfAny()
        {
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            RemoveConnection(c);
        }

        private void LayoutListColumns()
        {
            if (lvConnections == null) return;

            int total = lvConnections.ClientSize.Width;
            if (total <= 0) return;

            total = Math.Max(0, total - 4); // small buffer

            int nameMin = 120;
            int hostMin = 140;
            int descMin = 100;

            if (_autoLayoutColumns)
            {
                int nameWidth = Math.Max(nameMin, (int)Math.Round(total * 0.25));
                int hostWidth = Math.Max(hostMin, (int)Math.Round(total * 0.25));
                int descWidth = Math.Max(0, total - nameWidth - hostWidth);

                int overflow = (nameWidth + hostWidth + descWidth) - total;
                if (overflow > 0)
                {
                    int cut = Math.Min(overflow, hostWidth - hostMin);
                    hostWidth -= cut;
                    overflow -= cut;

                    if (overflow > 0)
                    {
                        cut = Math.Min(overflow, nameWidth - nameMin);
                        nameWidth -= cut;
                    }

                    descWidth = Math.Max(0, total - nameWidth - hostWidth);
                }

                try
                {
                    colName.Width = nameWidth;
                    colHost.Width = hostWidth;
                    colDescription.Width = Math.Max(descMin, descWidth);
                    // Ensure flush right
                    colDescription.Width = Math.Max(0, total - colName.Width - colHost.Width);
                }
                catch { }

                return;
            }

            // Manual/saved mode: keep current widths, but never overflow and keep flush right.
            try
            {
                int currentSum = colName.Width + colHost.Width + colDescription.Width;
                if (currentSum > total)
                {
                    int overflow = currentSum - total;

                    int take = Math.Min(overflow, Math.Max(0, colDescription.Width - descMin));
                    colDescription.Width -= take;
                    overflow -= take;

                    if (overflow > 0)
                    {
                        take = Math.Min(overflow, Math.Max(0, colHost.Width - hostMin));
                        colHost.Width -= take;
                        overflow -= take;
                    }

                    if (overflow > 0)
                    {
                        take = Math.Min(overflow, Math.Max(0, colName.Width - nameMin));
                        colName.Width -= take;
                        overflow -= take;
                    }
                }

                // Always make Description fill remaining so right edge is flush
                colDescription.Width = Math.Max(0, total - colName.Width - colHost.Width);
            }
            catch { }
        }

        private static string SanitizeFileName(string input)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var chars = new System.Text.StringBuilder(input.Length);
            foreach (var ch in input)
            {
                chars.Append(Array.IndexOf(invalid, ch) >= 0 ? '-' : ch);
            }
            var result = chars.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result)) result = "RdpManager";
            // Avoid extremely long filenames
            return result.Length > 100 ? result.Substring(0, 100) : result;
        }

        private void ApplyInitialColumnWidths()
        {
            // If we have sane saved widths, use them. Otherwise fall back to percentage.
            if (_settings.ColumnWidthName is int savedName && savedName > 0
                && _settings.ColumnWidthHost is int savedHost && savedHost > 0
                && _settings.ColumnWidthDescription is int savedDescription && savedDescription > 0)
            {
                try
                {
                    colName.Width = savedName;
                    colHost.Width = savedHost;
                    colDescription.Width = savedDescription;
                    _autoLayoutColumns = false;
                    _settings.AutoLayoutColumns = false;
                    SaveSettings();
                }
                catch { }
            }
            else
            {
                _autoLayoutColumns = true;
                _settings.AutoLayoutColumns = true;
                // Layout will compute percentages
                LayoutListColumns();
            }

            // Make sure it fits the current control width
            try { LayoutListColumns(); } catch { }
        }
    }
}
