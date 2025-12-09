using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RdpManager.Models;
using RdpManager.Services;

namespace RdpManager
{
    public partial class MainForm : Form
    {
        private readonly ConnectionStore _store = new ConnectionStore();
        private readonly SettingsStore _settingsStore = new SettingsStore();
        private List<Connection> _connections = new();
        private bool _isListView = true;
        private string _sortField = "Name"; // or "Host"
        private bool _sortAsc = true;
        private UserSettings _settings = new UserSettings();
        private readonly AutoCompleteStringCollection _quickConnectAutoComplete = new AutoCompleteStringCollection();

        public MainForm()
        {
            InitializeComponent();
            // Load persisted settings
            _settings = _settingsStore.Load();
            _settings.QuickConnectHistory ??= new List<string>();
            if (quickConnectTextBox != null)
            {
                quickConnectTextBox.AutoCompleteCustomSource = _quickConnectAutoComplete;
            }
            UpdateQuickConnectAutocomplete();
            _isListView = _settings.IsListView;
            _sortField = string.Equals(_settings.SortField, "Host", StringComparison.OrdinalIgnoreCase) ? "Host" : "Name";
            _sortAsc = _settings.SortAsc;

            LoadConnections();
            // Re-layout cards when the container is resized
            try { flowConnections.SizeChanged += (s, e) => LayoutCards(); } catch { }
            try { lvConnections.SizeChanged += (s, e) => LayoutListColumns(); } catch { }
            UpdateSortMenuChecks();
            ToggleView(_isListView);
        }

        private void LoadConnections()
        {
            _connections = _store.Load();
        }

        private void SaveConnections()
        {
            _store.Save(_connections);
        }

        private void RenderConnections()
        {
            var list = GetSorted(_connections);
            if (!_isListView)
            {
                flowConnections.SuspendLayout();
                try
                {
                    flowConnections.Controls.Clear();
                    foreach (var c in list)
                    {
                        var btn = new Button();
                        btn.Text = c.Name;
                        btn.AutoSize = false;
                        btn.MinimumSize = new Size(50, 44);
                        btn.Width = 75; // will be resized in LayoutCards()
                        btn.Height = 44;
                        btn.Margin = new Padding(8);
                        btn.Tag = c;
                        btn.BackColor = Color.White;
                        btn.Click += (s, e) => LaunchRdp((Connection)btn.Tag!);

                    var menu = new ContextMenuStrip();
                    var addItem = new ToolStripMenuItem("Add Connection"); addItem.Click += btnAdd_Click;
                    var connectItem = new ToolStripMenuItem("Connect"); connectItem.Click += (s, e) => LaunchRdp((Connection)btn.Tag!);
                    var editItem = new ToolStripMenuItem("Edit"); editItem.Click += (s, e) => EditConnection((Connection)btn.Tag!);
                    var copyItem = new ToolStripMenuItem("Copy Connection"); copyItem.Click += (s, e) => CopyConnection((Connection)btn.Tag!);
                    var removeItem = new ToolStripMenuItem("Remove"); removeItem.Click += (s, e) => RemoveConnection((Connection)btn.Tag!);
                    var exportItem = new ToolStripMenuItem("Export Connections..."); exportItem.Click += (s, e) => ExportConnections();
                    var importItem = new ToolStripMenuItem("Import Connections..."); importItem.Click += (s, e) => ImportConnections();
                    menu.Items.AddRange(new ToolStripItem[] {
                        addItem,
                        connectItem,
                        editItem,
                        copyItem,
                        removeItem,
                        new ToolStripSeparator(),
                        exportItem,
                        importItem
                    });
                    btn.ContextMenuStrip = menu;

                        flowConnections.Controls.Add(btn);
                    }
                }
                finally
                {
                    flowConnections.ResumeLayout();
                }
                LayoutCards();
            }
            else
            {
                lvConnections.BeginUpdate();
                try
                {
                    lvConnections.Items.Clear();
                    foreach (var c in list)
                    {
                        var lvi = new ListViewItem(c.Name);
                        lvi.SubItems.Add(FormatHost(c));
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
        }

        private void btnAdd_Click(object? sender, EventArgs e)
        {
            using var dlg = new AddConnectionForm();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.NewConnection != null)
            {
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
                Port = source.Port,
                Domain = source.Domain,
                Username = source.Username,
                ScreenWidth = source.ScreenWidth,
                ScreenHeight = source.ScreenHeight
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
                    Title = "Export Connections",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = "connections.json",
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(_connections, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(dlg.FileName, json);
                    MessageBox.Show(this, "Connections exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    Title = "Import Connections",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Multiselect = false
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var json = System.IO.File.ReadAllText(dlg.FileName);
                    var imported = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Connection>>(json) ?? new System.Collections.Generic.List<Connection>();

                    if (imported.Count == 0)
                    {
                        MessageBox.Show(this, "No connections found in the file.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var replace = MessageBox.Show(this,
                        $"Import {imported.Count} connections and replace existing list?\nChoose No to cancel.",
                        "Import Connections",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (replace == DialogResult.Yes)
                    {
                        _connections = imported;
                        SaveConnections();
                        RenderConnections();
                        MessageBox.Show(this, "Connections imported.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
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

                if (c.ScreenWidth.HasValue && c.ScreenHeight.HasValue)
                {
                    lines.Add($"desktopwidth:i:{c.ScreenWidth.Value}");
                    lines.Add($"desktopheight:i:{c.ScreenHeight.Value}");
                    // Ensure windowed mode so explicit resolution is honored
                    lines.Add("screen mode id:i:1");
                    lines.Add("use multimon:i:0");
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
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to start RDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            _isListView = isList;
            flowConnections.Visible = !isList;
            lvConnections.Visible = isList;
            // update menu checks if menu exists
            try { miViewButtons.Checked = !isList; miViewList.Checked = isList; } catch { }
            // persist
            _settings.IsListView = _isListView;
            SaveSettings();
            RenderConnections();
            if (!isList) LayoutCards();
            else LayoutListColumns();
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
            var host = c.Address;
            if (c.Port.HasValue && c.Port.Value > 0) host = $"{host}:{c.Port.Value}";
            return host;
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
            if (!_isListView) return;
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            LaunchRdp(c);
        }

        private void EditSelectedIfAny()
        {
            if (!_isListView) return;
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            EditConnection(c);
        }

        private void CopySelectedIfAny()
        {
            if (!_isListView) return;
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            CopyConnection(c);
        }

        private void RemoveSelectedIfAny()
        {
            if (!_isListView) return;
            if (lvConnections.SelectedItems.Count == 0) return;
            var c = (Connection)lvConnections.SelectedItems[0].Tag!;
            RemoveConnection(c);
        }

        private void LayoutCards()
        {
            if (flowConnections == null) return;
            if (flowConnections.Controls.Count == 0) return;
            int client = flowConnections.ClientSize.Width - flowConnections.Padding.Left - flowConnections.Padding.Right;
            if (client <= 0) return;

            // Determine margins from first button
            var first = flowConnections.Controls[0];
            int mLeft = first.Margin.Left;
            int mRight = first.Margin.Right;
            int edge = mLeft + mRight; // total edge margins
            int spacing = mLeft + mRight; // spacing between items (approx)

            int minCardWidth = 75;
            int inner = client - edge;
            if (inner <= minCardWidth) inner = client; // fallback

            int cols = Math.Max(1, (inner + spacing) / (minCardWidth + spacing));
            int cardWidth = Math.Max(minCardWidth, (inner - (cols - 1) * spacing) / cols);

            foreach (Control ctrl in flowConnections.Controls)
            {
                ctrl.Width = cardWidth;
            }
        }

        private void LayoutListColumns()
        {
            if (lvConnections == null) return;
            int total = lvConnections.ClientSize.Width;
            if (total <= 0) return;
            // Leave a small buffer for vertical scrollbar and border
            int buffer = 6;
            total = Math.Max(0, total - buffer);

            // Proportional widths: Name 45%, Host 55%, with sensible minimums
            int nameMin = 150;
            int hostMin = 200;
            int nameWidth = Math.Max(nameMin, (int)(total * 0.45));
            int hostWidth = Math.Max(hostMin, total - nameWidth);

            // If vertical scrollbar appears, Windows may reduce visible width; guard again
            if (nameWidth + hostWidth > lvConnections.ClientSize.Width)
            {
                hostWidth = Math.Max(hostMin, lvConnections.ClientSize.Width - nameWidth - buffer);
            }

            try
            {
                colName.Width = nameWidth;
                colHost.Width = hostWidth;
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
    }
}
