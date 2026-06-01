
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RdpManager.Models;

namespace RdpManager
{
    public partial class ImportFromADForm : Form
    {
        private readonly List<string> _tabs;
        private readonly Action<List<Connection>> _onImport;
        private List<ADComputer> _computers = new();
        // AD debug logging disabled by default for production builds; can be toggled via UI
        private bool _enableLogging = false;
        private readonly string _logFile = Path.Combine(Path.GetTempPath(), "RdpManager_AD_Debug.log");

        // Control references (nullable until InitializeComponent runs)
        private TextBox? _domainTextBox;
        private TextBox? _searchTextBox;
        private ListView? _listView;
        private ComboBox? _tabComboBox;
        private TextBox? _usernameTextBox;
        private TextBox? _connectionDomainTextBox;
        private TextBox? _portTextBox;
        private TextBox? _screenWidthTextBox;
        private TextBox? _screenHeightTextBox;

        public ImportFromADForm(List<string> tabs, Action<List<Connection>> onImport)
        {
            _tabs = tabs;
            _onImport = onImport;

            Log($"AD Import Form initialized. Log file: {_logFile}");
            InitializeComponent();
            LoadTabs();
        }

        private void Log(string message)
        {
            if (!_enableLogging) return;
            try
            {
                File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n");
            }
            catch { } // Ignore logging errors
        }

        private void InitializeComponent()
        {
            Log("InitializeComponent started");
            this.Text = "Import from Active Directory";
            this.Size = new System.Drawing.Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Domain selection
            var domainLabel = new Label();
            domainLabel.Text = "Domain (leave empty for current):";
            domainLabel.Location = new System.Drawing.Point(10, 10);
            domainLabel.AutoSize = true;

            _domainTextBox = new TextBox();
            _domainTextBox.Location = new System.Drawing.Point(10, 30);
            _domainTextBox.Width = 200;

            // Search controls
            var searchLabel = new Label();
            searchLabel.Text = "Search computers (leave empty for all):";
            searchLabel.Location = new System.Drawing.Point(220, 10);
            searchLabel.AutoSize = true;

            _searchTextBox = new TextBox();
            _searchTextBox.Location = new System.Drawing.Point(220, 30);
            _searchTextBox.Width = 200;

            var searchButton = new Button();
            searchButton.Text = "Search";
            searchButton.Location = new System.Drawing.Point(430, 28);
            searchButton.Click += (s, e) =>
            {
                Log("Search button clicked");
                SearchComputers(_domainTextBox?.Text ?? string.Empty, _searchTextBox?.Text ?? string.Empty);
            };

            // Computer list
            _listView = new ListView();
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.CheckBoxes = true;
            _listView.GridLines = true;
            _listView.Columns.Add("Select", 60);
            _listView.Columns.Add("Name", 150);
            _listView.Columns.Add("DNS Hostname", 200);
            _listView.Columns.Add("Description", 200);
            _listView.Location = new System.Drawing.Point(10, 70);
            _listView.Size = new System.Drawing.Size(860, 300);
            Log($"ListView created: {_listView != null}");

            // Connection settings
            var settingsGroup = new GroupBox();
            settingsGroup.Text = "Connection Settings";
            settingsGroup.Location = new System.Drawing.Point(10, 380);
            settingsGroup.Size = new System.Drawing.Size(860, 120);

            var tabLabel = new Label();
            tabLabel.Text = "Tab:";
            tabLabel.Location = new System.Drawing.Point(10, 25);
            tabLabel.AutoSize = true;

            _tabComboBox = new ComboBox();
            // Allow typing so users can enter a new tab name; non-existing tabs will be created on import
            _tabComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            _tabComboBox.Location = new System.Drawing.Point(40, 22);
            _tabComboBox.Width = 150;

            var usernameLabel = new Label();
            usernameLabel.Text = "Username:";
            usernameLabel.Location = new System.Drawing.Point(210, 25);
            usernameLabel.AutoSize = true;

            _usernameTextBox = new TextBox();
            _usernameTextBox.Location = new System.Drawing.Point(280, 22);
            _usernameTextBox.Width = 120;

            var connectionDomainLabel = new Label();
            connectionDomainLabel.Text = "Domain:";
            connectionDomainLabel.Location = new System.Drawing.Point(420, 25);
            connectionDomainLabel.AutoSize = true;

            _connectionDomainTextBox = new TextBox();
            _connectionDomainTextBox.Location = new System.Drawing.Point(470, 22);
            _connectionDomainTextBox.Width = 120;

            var portLabel = new Label();
            portLabel.Text = "Port (leave empty for default):";
            portLabel.Location = new System.Drawing.Point(10, 55);
            portLabel.AutoSize = true;

            _portTextBox = new TextBox();
            _portTextBox.Location = new System.Drawing.Point(180, 52);
            _portTextBox.Width = 80;

            var screenWidthLabel = new Label();
            screenWidthLabel.Text = "Screen Width:";
            screenWidthLabel.Location = new System.Drawing.Point(280, 55);
            screenWidthLabel.AutoSize = true;

            _screenWidthTextBox = new TextBox();
            _screenWidthTextBox.Location = new System.Drawing.Point(360, 52);
            _screenWidthTextBox.Width = 80;
            _screenWidthTextBox.Text = "1920";

            var screenHeightLabel = new Label();
            screenHeightLabel.Text = "Screen Height:";
            screenHeightLabel.Location = new System.Drawing.Point(460, 55);
            screenHeightLabel.AutoSize = true;

            _screenHeightTextBox = new TextBox();
            _screenHeightTextBox.Location = new System.Drawing.Point(550, 52);
            _screenHeightTextBox.Width = 80;
            _screenHeightTextBox.Text = "1080";

            settingsGroup.Controls.Add(tabLabel);
            settingsGroup.Controls.Add(_tabComboBox);
            settingsGroup.Controls.Add(usernameLabel);
            settingsGroup.Controls.Add(_usernameTextBox);
            settingsGroup.Controls.Add(connectionDomainLabel);
            settingsGroup.Controls.Add(_connectionDomainTextBox);
            settingsGroup.Controls.Add(portLabel);
            settingsGroup.Controls.Add(_portTextBox);
            settingsGroup.Controls.Add(screenWidthLabel);
            settingsGroup.Controls.Add(_screenWidthTextBox);
            settingsGroup.Controls.Add(screenHeightLabel);
            settingsGroup.Controls.Add(_screenHeightTextBox);

            // Buttons
            var importButton = new Button();
            importButton.Text = "Import Selected";
            importButton.Location = new System.Drawing.Point(650, 510);
            importButton.Click += (s, e) => ImportSelected(_listView, _tabComboBox, _usernameTextBox, _connectionDomainTextBox, _portTextBox, _screenWidthTextBox, _screenHeightTextBox);

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(750, 510);
            cancelButton.Click += (s, e) => this.Close();

            // Logging toggle - allow user to enable AD debug logging when needed
            var loggingCheckbox = new CheckBox();
            loggingCheckbox.Text = "Enable AD debug logging";
            loggingCheckbox.Location = new System.Drawing.Point(10, 510);
            loggingCheckbox.AutoSize = true;
            loggingCheckbox.Checked = _enableLogging;
            loggingCheckbox.CheckedChanged += (s, e) =>
            {
                _enableLogging = loggingCheckbox.Checked;
                if (_enableLogging)
                {
                    MessageBox.Show($"AD debug logging enabled. Log file: {_logFile}", "Logging Enabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            this.Controls.Add(domainLabel);
            this.Controls.Add(_domainTextBox);
            this.Controls.Add(searchLabel);
            this.Controls.Add(_searchTextBox);
            this.Controls.Add(searchButton);
            this.Controls.Add(_listView);
            this.Controls.Add(settingsGroup);
            this.Controls.Add(loggingCheckbox);
            this.Controls.Add(importButton);
            this.Controls.Add(cancelButton);

            Log($"InitializeComponent completed. ListView is null: {_listView == null}");

            // Load initial data - no automatic search, user must click search button
        }

        private void LoadTabs()
        {
            if (_tabComboBox != null)
            {
                _tabComboBox.Items.AddRange(_tabs.ToArray());
                if (_tabs.Count > 0)
                {
                    _tabComboBox.SelectedIndex = 0;
                }
            }
        }

        private void SearchComputers(string domain, string searchTerm)
        {
            try
            {
                Log($"Starting AD search - Domain: '{domain}', SearchTerm: '{searchTerm}'");
                Log($"_listView is null: {_listView == null}");
                _computers.Clear();
                if (_listView == null)
                {
                    Log("ERROR: ListView control not found");
                    return;
                }

                _listView.Items.Clear();

                string ldapPath;
                if (string.IsNullOrWhiteSpace(domain))
                {
                    Log("Using current domain");
                    // Use current domain
                    using (var entry = new DirectoryEntry("LDAP://RootDSE"))
                    {
                        var defaultNamingContext = entry.Properties["defaultNamingContext"]?.Value?.ToString();
                        if (string.IsNullOrEmpty(defaultNamingContext))
                        {
                            Log("ERROR: Unable to determine current domain");
                            MessageBox.Show("Unable to determine current domain. Please specify a domain name or ensure this computer is domain-joined.", "Domain Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        ldapPath = $"LDAP://{defaultNamingContext}";
                        Log($"LDAP Path: {ldapPath}");
                    }
                }
                else
                {
                    Log($"Using specified domain: {domain}");
                    // Use specified domain
                    ldapPath = $"LDAP://DC={domain.Replace(".", ",DC=")}";
                    Log($"LDAP Path: {ldapPath}");
                }

                using (var searchRoot = new DirectoryEntry(ldapPath))
                using (var searcher = new DirectorySearcher(searchRoot))
                {
                    // Fix search filter for empty search terms
                    string filter;
                    if (string.IsNullOrWhiteSpace(searchTerm))
                    {
                        filter = "(objectClass=computer)";
                        Log("Search filter: (objectClass=computer) - searching all computers");
                    }
                    else
                    {
                        filter = $"(&(objectClass=computer)(name=*{searchTerm}*))";
                        Log($"Search filter: {filter}");
                    }
                    searcher.Filter = filter;
                    searcher.PropertiesToLoad.Add("name");
                    searcher.PropertiesToLoad.Add("dNSHostName");
                    searcher.PropertiesToLoad.Add("description");
                    searcher.Sort = new SortOption("name", SortDirection.Ascending);
                    searcher.PageSize = 1000; // Handle large result sets

                    Log("Executing search...");
                    using (var results = searcher.FindAll())
                    {
                        Log($"Search completed. Found {results.Count} results.");
                        // Use manual enumerator so we can catch exceptions during MoveNext (some AD entries may be malformed)
                        var enumerator = results.GetEnumerator();
                        int resultIndex = 0;
                        while (true)
                        {
                            bool hasNext;
                            try
                            {
                                hasNext = enumerator.MoveNext();
                            }
                            catch (Exception mex)
                            {
                                Log($"Enumerator MoveNext error at index {resultIndex}: {mex.Message}\n{mex.StackTrace}");
                                break;
                            }

                            if (!hasNext) break;

                            var result = (SearchResult)enumerator.Current;
                            resultIndex++;
                            try
                            {
                                // Log property names and counts to help diagnose malformed entries
                                foreach (string propName in result.Properties.PropertyNames)
                                {
                                    try
                                    {
                                        Log($"Result property: {propName} count={result.Properties[propName].Count}");
                                    }
                                    catch (Exception pex)
                                    {
                                        Log($"Failed to enumerate property '{propName}': {pex.Message}");
                                    }
                                }

                                string name = "";
                                if (result.Properties.Contains("name"))
                                {
                                    foreach (var v in result.Properties["name"])
                                    {
                                        name = v?.ToString() ?? "";
                                        break;
                                    }
                                }

                                string dnsHostname = "";
                                if (result.Properties.Contains("dNSHostName"))
                                {
                                    foreach (var v in result.Properties["dNSHostName"])
                                    {
                                        dnsHostname = v?.ToString() ?? "";
                                        break;
                                    }
                                }

                                string description = "";
                                if (result.Properties.Contains("description"))
                                {
                                    foreach (var v in result.Properties["description"])
                                    {
                                        description = v?.ToString() ?? "";
                                        break;
                                    }
                                }

                                Log($"Computer found: Name='{name}', DNS='{dnsHostname}', Desc='{description}'");

                                // Only add if we have at least one identifying value (prefer name)
                                if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(dnsHostname))
                                {
                                    var computer = new ADComputer
                                    {
                                        Name = name,
                                        DNSHostname = dnsHostname,
                                        Description = description
                                    };

                                    _computers.Add(computer);

                                    var item = new ListViewItem("");
                                    item.SubItems.Add(computer.Name);
                                    item.SubItems.Add(computer.DNSHostname);
                                    item.SubItems.Add(computer.Description);
                                    item.Tag = computer;
                                    _listView.Items.Add(item);
                                }
                            }
                            catch (Exception rex)
                            {
                                Log($"Per-result processing error: {rex.Message}\n{rex.StackTrace}");
                                // continue processing remaining results
                                continue;
                            }
                        }
                    }
                }

                Log($"Search completed successfully. Total computers found: {_computers.Count}");

                // Show a message when no computers were found so the user gets feedback
                if (_computers.Count == 0)
                {
                    Log("No computers found");
                    MessageBox.Show("No computers were found matching the search criteria.", "No Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Log($"Found {_computers.Count} computers");
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx) when (comEx.ErrorCode == -2147016646) // 0x8007203A - server not operational
            {
                Log($"COM Exception (server not operational): {comEx.Message}");
                MessageBox.Show("Cannot connect to the domain controller. Please check:\n• Domain name is correct\n• Network connectivity\n• Domain controller is reachable", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log($"Unauthorized Access Exception: {ex.Message}");
                MessageBox.Show("Access denied. Please ensure you have permissions to query Active Directory.", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log($"General Exception: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error searching Active Directory: {ex.Message}", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportSelected(ListView? listView, ComboBox? tabComboBox, TextBox? usernameTextBox, TextBox? domainTextBox, TextBox? portTextBox, TextBox? screenWidthTextBox, TextBox? screenHeightTextBox)
        {
            if (listView == null || tabComboBox == null)
            {
                MessageBox.Show("Internal error: required controls not available.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedComputers = new List<ADComputer>();
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Checked && item.Tag is ADComputer computer)
                {
                    selectedComputers.Add(computer);
                }
            }

            if (selectedComputers.Count == 0)
            {
                MessageBox.Show("Please select at least one computer to import.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Allow either selecting an existing tab or typing a new tab name
            if (string.IsNullOrWhiteSpace(tabComboBox.Text))
            {
                MessageBox.Show("Please select or enter a tab for the imported connections.", "No Tab Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var connections = new List<Connection>();
            var tabName = tabComboBox.SelectedItem?.ToString() ?? tabComboBox.Text?.Trim() ?? "General";

            // If the user typed a new tab name, add it to the tab list and combo box so it persists in-memory
            try
            {
                if (!string.IsNullOrWhiteSpace(tabName) && !_tabs.Contains(tabName))
                {
                    _tabs.Add(tabName);
                    try { tabComboBox.Items.Add(tabName); } catch { }
                    try { tabComboBox.SelectedItem = tabName; } catch { }
                }
            }
            catch { }

            foreach (var computer in selectedComputers)
            {
                var connection = new Connection
                {
                    Name = computer.Name,
                    Address = computer.DNSHostname,
                    Description = string.IsNullOrWhiteSpace(computer.Description) ? null : computer.Description,
                    TabName = tabName,
                    Username = string.IsNullOrWhiteSpace(usernameTextBox?.Text ?? string.Empty) ? null : usernameTextBox?.Text,
                    Domain = string.IsNullOrWhiteSpace(domainTextBox?.Text ?? string.Empty) ? null : domainTextBox?.Text
                };

                if (int.TryParse(portTextBox?.Text ?? string.Empty, out var port) && port > 0)
                {
                    connection.Port = port;
                }

                if (int.TryParse(screenWidthTextBox?.Text ?? string.Empty, out var width) && width > 0)
                {
                    connection.ScreenWidth = width;
                }

                if (int.TryParse(screenHeightTextBox?.Text ?? string.Empty, out var height) && height > 0)
                {
                    connection.ScreenHeight = height;
                }

                connections.Add(connection);
            }

            _onImport(connections);
            this.Close();
        }

        private class ADComputer
        {
            public string Name { get; set; } = "";
            public string DNSHostname { get; set; } = "";
            public string Description { get; set; } = "";
        }
    }
}