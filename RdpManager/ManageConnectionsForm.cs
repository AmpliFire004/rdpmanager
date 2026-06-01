using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RdpManager.Models;

namespace RdpManager
{
    public partial class ManageConnectionsForm : Form
    {
        private readonly List<Connection> _connections;
        private readonly List<string> _tabs;
        private readonly Action _onSave;

        public ManageConnectionsForm(List<Connection> connections, List<string> tabs, Action onSave)
        {
            _connections = connections ?? new List<Connection>();
            _tabs = tabs ?? new List<string>();
            _onSave = onSave ?? (() => { });

            InitializeComponent();
            LoadConnections();
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Connections";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create controls
            var listView = new ListView();
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.Columns.Add("Name", 150);
            listView.Columns.Add("Host", 150);
            listView.Columns.Add("Tab", 100);
            listView.Columns.Add("Username", 100);
            listView.Columns.Add("Domain", 100);

            // Wrap the ListView in a panel to provide left/right padding
            var listPanel = new Panel();
            listPanel.Dock = DockStyle.Top;
            listPanel.Height = 450;
            listPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            listView.Dock = DockStyle.Fill;
            listPanel.Controls.Add(listView);

            var tabComboBox = new ComboBox();
            tabComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            tabComboBox.Items.AddRange((_tabs ?? new List<string>()).ToArray());
            tabComboBox.Location = new System.Drawing.Point(10, 470);
            tabComboBox.Width = 150;

            var changeTabButton = new Button();
            changeTabButton.Text = "Change Tab";
            changeTabButton.Location = new System.Drawing.Point(170, 468);
            changeTabButton.Click += (s, e) => ChangeSelectedTab(listView, tabComboBox);
            // Context menu for the list view (right-click actions)
            var listContext = new ContextMenuStrip();
            var deleteSelectedMenu = new ToolStripMenuItem("Delete Selected");
            deleteSelectedMenu.Click += (s, e) => DeleteSelected(listView);
            listContext.Items.Add(deleteSelectedMenu);
            listView.ContextMenuStrip = listContext;

            var addTabButton = new Button();
            addTabButton.Text = "Add Tab";
            addTabButton.Location = new System.Drawing.Point(280, 468);
            addTabButton.Click += (s, e) => AddNewTab(tabComboBox);

            var removeTabButton = new Button();
            removeTabButton.Text = "Remove Tab";
            removeTabButton.Location = new System.Drawing.Point(360, 468);
            removeTabButton.Click += (s, e) => RemoveTab(tabComboBox);

            var saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new System.Drawing.Point(600, 468);
            saveButton.Click += (s, e) => { _onSave(); this.Close(); };

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(690, 468);
            cancelButton.Click += (s, e) => this.Close();

            this.Controls.Add(listPanel);
            this.Controls.Add(tabComboBox);
            this.Controls.Add(changeTabButton);
            this.Controls.Add(addTabButton);
            this.Controls.Add(removeTabButton);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);

            // Store reference for event handlers
            this.Tag = listView;
        }

        private void LoadConnections()
        {
            var listView = (ListView)this.Tag!;
            listView.Items.Clear();

            foreach (var connection in _connections.OrderBy(c => c.TabName).ThenBy(c => c.Name))
            {
                var item = new ListViewItem(connection.Name);
                item.SubItems.Add(FormatHost(connection));
                item.SubItems.Add(connection.TabName);
                item.SubItems.Add(connection.Username ?? "");
                item.SubItems.Add(connection.Domain ?? "");
                item.Tag = connection;
                listView.Items.Add(item);
            }
        }

        private void ChangeSelectedTab(ListView listView, ComboBox tabComboBox)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a connection first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (tabComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a tab first.", "No Tab Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (tabComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a tab first.", "No Tab Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var newTab = tabComboBox.SelectedItem?.ToString() ?? (_tabs.Count > 0 ? _tabs[0] : "General");
            foreach (ListViewItem item in listView.SelectedItems)
            {
                var connection = (Connection)item.Tag!;
                connection.TabName = newTab;
                item.SubItems[2].Text = newTab;
            }
        }

        private void AddNewTab(ComboBox tabComboBox)
        {
            using var inputDialog = new Form();
            inputDialog.Text = "Add New Tab";
            inputDialog.Size = new System.Drawing.Size(300, 150);
            inputDialog.StartPosition = FormStartPosition.CenterParent;
            inputDialog.FormBorderStyle = FormBorderStyle.FixedDialog;

            var label = new Label();
            label.Text = "Tab Name:";
            label.Location = new System.Drawing.Point(10, 20);
            label.AutoSize = true;

            var textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(10, 40);
            textBox.Width = 260;

            var okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new System.Drawing.Point(110, 70);
            okButton.DialogResult = DialogResult.OK;

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(190, 70);
            cancelButton.DialogResult = DialogResult.Cancel;

            inputDialog.Controls.Add(label);
            inputDialog.Controls.Add(textBox);
            inputDialog.Controls.Add(okButton);
            inputDialog.Controls.Add(cancelButton);
            inputDialog.AcceptButton = okButton;
            inputDialog.CancelButton = cancelButton;

            if (inputDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var newTab = textBox.Text.Trim();
                if (!_tabs.Contains(newTab))
                {
                    _tabs.Add(newTab);
                    tabComboBox.Items.Add(newTab);
                    tabComboBox.SelectedItem = newTab;
                }
                else
                {
                    MessageBox.Show("Tab already exists.", "Duplicate Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void RemoveTab(ComboBox tabComboBox)
        {
            if (tabComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a tab to remove.", "No Tab Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tabToRemove = tabComboBox.SelectedItem.ToString();

            // Don't allow removing the last tab
            if (_tabs.Count <= 1)
            {
                MessageBox.Show("Cannot remove the last tab.", "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if tab has connections
            var connectionsInTab = _connections.Count(c => c.TabName == tabToRemove);
            if (connectionsInTab > 0)
            {
                var result = MessageBox.Show(
                    $"Tab '{tabToRemove}' contains {connectionsInTab} connection(s). Move them to '{_tabs[0]}'?",
                    "Tab Contains Connections",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel) return;
                if (result == DialogResult.Yes)
                {
                    // Move connections to first tab
                    foreach (var conn in _connections.Where(c => c.TabName == tabToRemove))
                    {
                        conn.TabName = _tabs[0];
                    }
                }
            }

            _tabs.Remove(tabToRemove!);
            tabComboBox.Items.Remove(tabToRemove!);
            if (tabComboBox.Items.Count > 0)
            {
                tabComboBox.SelectedIndex = 0;
            }
        }

        private void DeleteSelected(ListView listView)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more connections to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var count = listView.SelectedItems.Count;
            var confirm = MessageBox.Show($"Delete {count} selected connection(s)? This cannot be undone.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            var idsToRemove = new List<Guid>();
            foreach (ListViewItem item in listView.SelectedItems)
            {
                if (item.Tag is Connection c)
                {
                    idsToRemove.Add(c.Id);
                }
            }

            if (idsToRemove.Count == 0)
            {
                MessageBox.Show("No valid connections selected.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _connections.RemoveAll(c => idsToRemove.Contains(c.Id));
            LoadConnections();
        }

        private static string FormatHost(Connection c)
        {
            var host = c.Address;
            if (c.Port.HasValue && c.Port.Value > 0)
            {
                host = $"{host}:{c.Port.Value}";
            }
            return host;
        }
    }
}