using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using Jint;
using Jint.Runtime;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace advancedRenamer
{
    public partial class Form1 : Form
    {
        private const string AppDisplayName = "Advanced Javascript Renamer";
        private const string TemplateFileName = "script-templates.json";
        private const string SortTemplateFileName = "sort-templates.json";
        private readonly List<FileEntry> _entries = new List<FileEntry>();
        private readonly List<RenameOperation> _lastRenameOperations = new List<RenameOperation>();
        private List<FileEntry> _sortPreviewOriginalOrder;
        private ListView _listView;
        private TextBox _staticScriptTextBox;
        private TextBox _sortScriptTextBox;
        private TextBox _dynamicScriptTextBox;
        private Button _addButton;
        private Button _simulateButton;
        private Button _applyButton;
        private Button _undoButton;
        private Button _previewSortButton;
        private Button _applySortButton;
        private Button _cancelSortButton;
        private Button _loadSortTemplateButton;
        private Button _saveSortTemplateButton;
        private Button _loadTemplateButton;
        private Button _saveTemplateButton;
        private CheckBox _contextMenuCheckBox;
        private Label _languageLabel;
        private ComboBox _languageComboBox;
        private Label _countLabel;
        private SplitContainer _mainSplit;
        private SplitContainer _editorSplit;
        private TabPage _staticTab;
        private TabPage _sortTab;
        private TabPage _dynamicTab;
        private TextBox _guideTextBox;
        private bool _loadingLanguageList;
        private GroupBox _itemCountGroup;
        private GroupBox _fileOperationsGroup;
        private GroupBox _sortOperationsGroup;
        private GroupBox _templatesGroup;
        private GroupBox _settingsGroup;

        public Form1() : this(new string[0])
        {
        }

        public Form1(string[] startupPaths)
        {
            LanguageManager.EnsureLanguageSelected();
            InitializeComponent();
            if (startupPaths != null && startupPaths.Length > 0)
            {
                AddPaths(startupPaths);
            }
        }

        private void InitializeComponent()
        {
            Text = AppDisplayName;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 700);
            Size = new Size(1250, 820);
            AllowDrop = true;

            DragEnter += Form_DragEnter;
            DragDrop += Form_DragDrop;
            Shown += Form1_Shown;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(8, 6, 8, 4)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _addButton = new Button { Text = TextOf("AddFilesFolders"), AutoSize = true, Height = 28 };
            _simulateButton = new Button { Text = TextOf("SimulatePreview"), AutoSize = true, Height = 28 };
            _applyButton = new Button { Text = TextOf("ApplyChanges"), AutoSize = true, Height = 28 };
            _undoButton = new Button { Text = TextOf("UndoLast"), AutoSize = true, Height = 28, Enabled = false };
            _previewSortButton = new Button { Text = TextOf("PreviewSort"), AutoSize = true, Height = 28 };
            _applySortButton = new Button { Text = TextOf("ApplySort"), AutoSize = true, Height = 28, Enabled = false };
            _cancelSortButton = new Button { Text = TextOf("CancelSort"), AutoSize = true, Height = 28, Enabled = false };
            _loadSortTemplateButton = new Button { Text = TextOf("LoadSortTemplate"), AutoSize = true, Height = 28 };
            _saveSortTemplateButton = new Button { Text = TextOf("SaveSortTemplate"), AutoSize = true, Height = 28 };
            _loadTemplateButton = new Button { Text = TextOf("LoadTemplate"), AutoSize = true, Height = 28 };
            _saveTemplateButton = new Button { Text = TextOf("SaveTemplate"), AutoSize = true, Height = 28 };
            _contextMenuCheckBox = new CheckBox { Text = TextOf("AddToContextMenu"), AutoSize = true, Height = 28, Margin = new Padding(3, 6, 3, 3) };
            _languageLabel = new Label { Text = TextOf("LanguageShortLabel"), AutoSize = true, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 8, 3, 3) };
            _languageComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 23, Margin = new Padding(3, 5, 3, 3) };
            _countLabel = new Label { Text = FormatItemCount(0), AutoSize = true, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 8, 3, 3) };

            _addButton.Click += AddButton_Click;
            _simulateButton.Click += SimulateButton_Click;
            _applyButton.Click += ApplyButton_Click;
            _undoButton.Click += UndoButton_Click;
            _previewSortButton.Click += PreviewSortButton_Click;
            _applySortButton.Click += ApplySortButton_Click;
            _cancelSortButton.Click += CancelSortButton_Click;
            _loadSortTemplateButton.Click += LoadSortTemplateButton_Click;
            _saveSortTemplateButton.Click += SaveSortTemplateButton_Click;
            _loadTemplateButton.Click += LoadTemplateButton_Click;
            _saveTemplateButton.Click += SaveTemplateButton_Click;
            _contextMenuCheckBox.CheckedChanged += ContextMenuCheckBox_CheckedChanged;
            _contextMenuCheckBox.Checked = RegistryHelper.IsContextMenuInstalled();
            LoadLanguageComboBox();
            _languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;

            _itemCountGroup = CreateToolbarGroup(TextOf("ItemCountGroupTitle"), _countLabel);
            _fileOperationsGroup = CreateToolbarGroup(TextOf("FileOperationsGroupTitle"), _addButton, _simulateButton, _applyButton, _undoButton);
            _sortOperationsGroup = CreateToolbarGroup(TextOf("SortOperationsGroupTitle"), _previewSortButton, _applySortButton, _cancelSortButton, _loadSortTemplateButton, _saveSortTemplateButton);
            _templatesGroup = CreateToolbarGroup(TextOf("TemplatesGroupTitle"), _loadTemplateButton, _saveTemplateButton);
            _settingsGroup = CreateToolbarGroup(TextOf("SettingsGroupTitle"), _contextMenuCheckBox, _languageLabel, _languageComboBox);

            toolbar.Controls.Add(_itemCountGroup, 0, 0);
            toolbar.Controls.Add(_fileOperationsGroup, 1, 0);
            toolbar.Controls.Add(_sortOperationsGroup, 2, 0);
            toolbar.Controls.Add(_templatesGroup, 3, 0);
            toolbar.Controls.Add(_settingsGroup, 4, 0);

            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                AllowDrop = true
            };
            _listView.Columns.Add(TextOf("CurrentNameColumn"), 210);
            _listView.Columns.Add(TextOf("NewNameColumn"), 250);
            _listView.Columns.Add(TextOf("PathColumn"), 330);
            _listView.Columns.Add(TextOf("SizeColumn"), 90);
            _listView.Columns.Add(TextOf("TypeColumn"), 90);
            _listView.Columns.Add(TextOf("StatusColumn"), 220);
            _listView.DragEnter += Form_DragEnter;
            _listView.DragDrop += Form_DragDrop;

            _editorSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };

            var scriptTabs = new TabControl { Dock = DockStyle.Fill };
            _staticTab = new TabPage(TextOf("StaticTab"));
            _sortTab = new TabPage(TextOf("SortTab"));
            _dynamicTab = new TabPage(TextOf("DynamicTab"));

            _staticScriptTextBox = CreateScriptTextBox(GetDefaultStaticScript());
            _sortScriptTextBox = CreateScriptTextBox(GetDefaultSortScript());
            _dynamicScriptTextBox = CreateScriptTextBox(GetDefaultDynamicScript());
            _staticTab.Controls.Add(_staticScriptTextBox);
            _sortTab.Controls.Add(_sortScriptTextBox);
            _dynamicTab.Controls.Add(_dynamicScriptTextBox);
            scriptTabs.TabPages.Add(_staticTab);
            scriptTabs.TabPages.Add(_sortTab);
            scriptTabs.TabPages.Add(_dynamicTab);

            _guideTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Window,
                Font = new Font(FontFamily.GenericMonospace, 9.0f),
                Text = GetVariableGuide()
            };

            _editorSplit.Panel1.Controls.Add(scriptTabs);
            _editorSplit.Panel2.Controls.Add(_guideTextBox);
            _mainSplit.Panel1.Controls.Add(_listView);
            _mainSplit.Panel2.Controls.Add(_editorSplit);

            root.Controls.Add(toolbar, 0, 0);
            root.Controls.Add(_mainSplit, 0, 1);
            Controls.Add(root);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            SetPanelMinimums(_mainSplit, 200, 220);
            SetPanelMinimums(_editorSplit, 420, 230);
            SetSplitterDistance(_mainSplit, 360);
            SetSplitterDistance(_editorSplit, Math.Max(420, _editorSplit.Width - 260));
        }

        private static void SetPanelMinimums(SplitContainer splitContainer, int panel1MinSize, int panel2MinSize)
        {
            int available = splitContainer.Orientation == Orientation.Horizontal ? splitContainer.Height : splitContainer.Width;
            int maxCombined = Math.Max(50, available - splitContainer.SplitterWidth);

            if (panel1MinSize + panel2MinSize > maxCombined)
            {
                panel1MinSize = Math.Max(25, maxCombined / 2);
                panel2MinSize = Math.Max(25, maxCombined - panel1MinSize);
            }

            splitContainer.Panel1MinSize = panel1MinSize;
            splitContainer.Panel2MinSize = panel2MinSize;
        }

        private static void SetSplitterDistance(SplitContainer splitContainer, int requestedDistance)
        {
            int available = splitContainer.Orientation == Orientation.Horizontal ? splitContainer.Height : splitContainer.Width;
            int maxDistance = available - splitContainer.Panel2MinSize - splitContainer.SplitterWidth;

            if (maxDistance < splitContainer.Panel1MinSize)
            {
                return;
            }

            splitContainer.SplitterDistance = Math.Min(Math.Max(requestedDistance, splitContainer.Panel1MinSize), maxDistance);
        }

        private static GroupBox CreateToolbarGroup(string title, params Control[] controls)
        {
            var group = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(3, 0, 6, 0),
                Padding = new Padding(8, 15, 8, 6)
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            foreach (Control control in controls)
            {
                panel.Controls.Add(control);
            }

            group.Controls.Add(panel);
            return group;
        }

        private static TextBox CreateScriptTextBox(string text)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 10.0f),
                Text = text
            };
        }

        private static string TextOf(string key)
        {
            return LanguageManager.T(key);
        }

        private static string FormatText(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, TextOf(key), args);
        }

        private static string FormatItemCount(int count)
        {
            return count + " " + (count == 1 ? TextOf("ItemSingular") : TextOf("ItemPlural"));
        }

        private static string GetDefaultStaticScript()
        {
            return TextOf("DefaultStaticScript");
        }

        private static string GetDefaultSortScript()
        {
            return TextOf("DefaultSortScript");
        }

        private static string GetDefaultDynamicScript()
        {
            return TextOf("DefaultDynamicScript");
        }

        private static string GetVariableGuide()
        {
            return TextOf("VariableGuide");
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Title = TextOf("AddFilesTitle");
                openDialog.Multiselect = true;
                openDialog.CheckFileExists = true;
                if (openDialog.ShowDialog(this) == DialogResult.OK)
                {
                    AddPaths(openDialog.FileNames);
                }
            }

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = TextOf("AddFolderDescription");
                folderDialog.ShowNewFolderButton = false;
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                {
                    AddPaths(new[] { folderDialog.SelectedPath });
                }
            }
        }

        private void SimulateButton_Click(object sender, EventArgs e)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            SimulateRenames(showMessage: true);
        }

        private void LoadTemplateButton_Click(object sender, EventArgs e)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            string name = PromptTemplateName(TextOf("LoadTemplateDialogTitle"), TextOf("TemplateDialogPrompt"), LoadTemplates().Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            LoadTemplateIntoEditor(name);
        }

        private void SaveTemplateButton_Click(object sender, EventArgs e)
        {
            string name = PromptTemplateName(TextOf("SaveTemplateDialogTitle"), TextOf("TemplateDialogPrompt"), LoadTemplates().Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, TextOf("TemplateNameRequired"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<ScriptTemplate> templates = LoadTemplates();
                ScriptTemplate existing = templates.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    if (MessageBox.Show(this, FormatText("OverwriteTemplate", existing.Name), TextOf("SaveTemplate"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    existing.Name = name;
                    existing.StaticScript = _staticScriptTextBox.Text;
                    existing.DynamicScript = _dynamicScriptTextBox.Text;
                }
                else
                {
                    templates.Add(new ScriptTemplate
                    {
                        Name = name,
                        StaticScript = _staticScriptTextBox.Text,
                        DynamicScript = _dynamicScriptTextBox.Text
                    });
                }

                SaveTemplates(templates);
                MessageBox.Show(this, TextOf("TemplateSaved"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateSaveErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSortTemplateButton_Click(object sender, EventArgs e)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            string name = PromptTemplateName(TextOf("LoadSortTemplateDialogTitle"), TextOf("SortTemplateDialogPrompt"), LoadSortTemplates().Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            LoadSortTemplateIntoEditor(name);
        }

        private void SaveSortTemplateButton_Click(object sender, EventArgs e)
        {
            string name = PromptTemplateName(TextOf("SaveSortTemplateDialogTitle"), TextOf("SortTemplateDialogPrompt"), LoadSortTemplates().Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, TextOf("TemplateNameRequired"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<SortScriptTemplate> templates = LoadSortTemplates();
                SortScriptTemplate existing = templates.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    if (MessageBox.Show(this, FormatText("OverwriteTemplate", existing.Name), TextOf("SaveSortTemplate"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    existing.Name = name;
                    existing.SortScript = _sortScriptTextBox.Text;
                }
                else
                {
                    templates.Add(new SortScriptTemplate
                    {
                        Name = name,
                        SortScript = _sortScriptTextBox.Text
                    });
                }

                SaveSortTemplates(templates);
                MessageBox.Show(this, TextOf("TemplateSaved"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateSaveErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingLanguageList)
            {
                return;
            }

            var language = _languageComboBox.SelectedItem as LanguageManager.LanguageInfo;
            if (language == null || string.Equals(language.Code, LanguageManager.CurrentCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!LanguageManager.SetLanguage(language.Code))
            {
                LoadLanguageComboBox();
                return;
            }

            ApplyLanguageToUi();
        }

        private void LoadLanguageComboBox()
        {
            _loadingLanguageList = true;
            try
            {
                _languageComboBox.Items.Clear();
                foreach (LanguageManager.LanguageInfo language in LanguageManager.GetLanguages())
                {
                    int index = _languageComboBox.Items.Add(language);
                    if (string.Equals(language.Code, LanguageManager.CurrentCode, StringComparison.OrdinalIgnoreCase))
                    {
                        _languageComboBox.SelectedIndex = index;
                    }
                }

                if (_languageComboBox.SelectedIndex < 0 && _languageComboBox.Items.Count > 0)
                {
                    _languageComboBox.SelectedIndex = 0;
                }
            }
            finally
            {
                _loadingLanguageList = false;
            }
        }

        private void ApplyLanguageToUi()
        {
            _addButton.Text = TextOf("AddFilesFolders");
            _simulateButton.Text = TextOf("SimulatePreview");
            _applyButton.Text = TextOf("ApplyChanges");
            _undoButton.Text = TextOf("UndoLast");
            _previewSortButton.Text = TextOf("PreviewSort");
            _applySortButton.Text = TextOf("ApplySort");
            _cancelSortButton.Text = TextOf("CancelSort");
            _loadSortTemplateButton.Text = TextOf("LoadSortTemplate");
            _saveSortTemplateButton.Text = TextOf("SaveSortTemplate");
            _loadTemplateButton.Text = TextOf("LoadTemplate");
            _saveTemplateButton.Text = TextOf("SaveTemplate");
            _contextMenuCheckBox.Text = TextOf("AddToContextMenu");
            _languageLabel.Text = TextOf("LanguageShortLabel");
            _countLabel.Text = FormatItemCount(_entries.Count);

            _itemCountGroup.Text = TextOf("ItemCountGroupTitle");
            _fileOperationsGroup.Text = TextOf("FileOperationsGroupTitle");
            _sortOperationsGroup.Text = TextOf("SortOperationsGroupTitle");
            _templatesGroup.Text = TextOf("TemplatesGroupTitle");
            _settingsGroup.Text = TextOf("SettingsGroupTitle");

            if (_listView.Columns.Count >= 6)
            {
                _listView.Columns[0].Text = TextOf("CurrentNameColumn");
                _listView.Columns[1].Text = TextOf("NewNameColumn");
                _listView.Columns[2].Text = TextOf("PathColumn");
                _listView.Columns[3].Text = TextOf("SizeColumn");
                _listView.Columns[4].Text = TextOf("TypeColumn");
                _listView.Columns[5].Text = TextOf("StatusColumn");
            }

            if (_staticTab != null)
            {
                _staticTab.Text = TextOf("StaticTab");
            }

            if (_sortTab != null)
            {
                _sortTab.Text = TextOf("SortTab");
            }

            if (_dynamicTab != null)
            {
                _dynamicTab.Text = TextOf("DynamicTab");
            }

            if (_guideTextBox != null)
            {
                _guideTextBox.Text = GetVariableGuide();
            }

            LoadLanguageComboBox();

            try
            {
                if (_contextMenuCheckBox.Checked)
                {
                    RegistryHelper.InstallContextMenu(TextOf("ContextMenuText"));
                }
            }
            catch
            {
                // Language changes should not fail because the optional context menu text could not be refreshed.
            }
        }

        private void PreviewSortButton_Click(object sender, EventArgs e)
        {
            PreviewSort();
        }

        private void ApplySortButton_Click(object sender, EventArgs e)
        {
            if (!IsSortPreviewActive)
            {
                return;
            }

            _sortPreviewOriginalOrder = null;
            UpdateSortPreviewButtons();
        }

        private void CancelSortButton_Click(object sender, EventArgs e)
        {
            CancelSortPreview();
        }

        private void PreviewSort()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            if (IsSortPreviewActive)
            {
                RestoreSortPreviewOriginalOrder();
            }

            _sortPreviewOriginalOrder = new List<FileEntry>(_entries);

            try
            {
                var sortItems = new List<SortItem>();
                Engine engine = CreateScriptEngine();
                string sortScript = _sortScriptTextBox.Text;

                for (int i = 0; i < _entries.Count; i++)
                {
                    FileEntry entry = _entries[i];
                    object sortKey = ExecuteSortScript(engine, sortScript, entry, i);
                    sortItems.Add(new SortItem(entry, sortKey, i));
                }

                _entries.Clear();
                _entries.AddRange(sortItems
                    .OrderBy(x => x.Key, SortKeyComparer.Instance)
                    .ThenBy(x => x.OriginalIndex)
                    .Select(x => x.Entry));

                RefreshListView();
                UpdateSortPreviewButtons();
            }
            catch (JavaScriptException ex)
            {
                RestoreSortPreviewOriginalOrder();
                RefreshListView();
                UpdateSortPreviewButtons();
                MessageBox.Show(this, ex.Message, TextOf("SortErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                RestoreSortPreviewOriginalOrder();
                RefreshListView();
                UpdateSortPreviewButtons();
                MessageBox.Show(this, ex.Message, TextOf("SortErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureNoSortPreview()
        {
            if (!IsSortPreviewActive)
            {
                return true;
            }

            MessageBox.Show(this, TextOf("SortPreviewActiveMessage"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private bool IsSortPreviewActive
        {
            get { return _sortPreviewOriginalOrder != null; }
        }

        private void CancelSortPreview()
        {
            if (!IsSortPreviewActive)
            {
                return;
            }

            RestoreSortPreviewOriginalOrder();
            RefreshListView();
            UpdateSortPreviewButtons();
        }

        private void RestoreSortPreviewOriginalOrder()
        {
            if (_sortPreviewOriginalOrder == null)
            {
                return;
            }

            _entries.Clear();
            _entries.AddRange(_sortPreviewOriginalOrder);
            _sortPreviewOriginalOrder = null;
        }

        private void UpdateSortPreviewButtons()
        {
            bool isPreviewActive = IsSortPreviewActive;
            _applySortButton.Enabled = isPreviewActive;
            _cancelSortButton.Enabled = isPreviewActive;
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            SimulateRenames(showMessage: false);

            var ready = _entries.Where(x => x.Status == "Ready" && !string.IsNullOrWhiteSpace(x.NewName)).ToList();
            if (ready.Count == 0)
            {
                MessageBox.Show(this, TextOf("NoValidRenames"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this, FormatText("ApplyConfirm", ready.Count), TextOf("ApplyChanges"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _lastRenameOperations.Clear();
            _undoButton.Enabled = false;

            foreach (FileEntry entry in GetApplyOrder(ready))
            {
                try
                {
                    string sourcePath = entry.FullPath;
                    string targetPath = Path.Combine(entry.DirectoryPath, entry.NewName);
                    if (PathExists(targetPath))
                    {
                        entry.Status = "Skipped: target exists";
                        continue;
                    }

                    MovePath(sourcePath, targetPath, entry.IsDirectory);
                    if (entry.IsDirectory)
                    {
                        UpdateDescendantPaths(sourcePath, targetPath);
                    }

                    _lastRenameOperations.Add(new RenameOperation(sourcePath, targetPath, entry.IsDirectory));
                    UpdateEntryAfterMove(entry, targetPath);
                }
                catch (Exception ex)
                {
                    entry.Status = "Error: " + ex.Message;
                }
            }

            _undoButton.Enabled = _lastRenameOperations.Count > 0;
            RefreshListView();
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            if (_lastRenameOperations.Count == 0)
            {
                MessageBox.Show(this, TextOf("UndoNoOperation"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this, FormatText("UndoConfirm", _lastRenameOperations.Count), TextOf("UndoLast"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            int restored = 0;
            var remaining = new List<RenameOperation>();

            for (int i = _lastRenameOperations.Count - 1; i >= 0; i--)
            {
                RenameOperation operation = _lastRenameOperations[i];
                try
                {
                    if (!PathExists(operation.NewPath))
                    {
                        MarkEntryStatus(operation.NewPath, "Undo skipped: renamed file missing");
                        remaining.Add(operation);
                        continue;
                    }

                    if (PathExists(operation.OriginalPath))
                    {
                        MarkEntryStatus(operation.NewPath, "Undo skipped: original exists");
                        remaining.Add(operation);
                        continue;
                    }

                    MovePath(operation.NewPath, operation.OriginalPath, operation.IsDirectory);
                    if (operation.IsDirectory)
                    {
                        UpdateDescendantPaths(operation.NewPath, operation.OriginalPath);
                    }

                    UpdateEntryAfterUndo(operation.NewPath, operation.OriginalPath, operation.IsDirectory);
                    restored++;
                }
                catch (Exception ex)
                {
                    MarkEntryStatus(operation.NewPath, "Undo error: " + ex.Message);
                    remaining.Add(operation);
                }
            }

            _lastRenameOperations.Clear();
            remaining.Reverse();
            _lastRenameOperations.AddRange(remaining);
            _undoButton.Enabled = _lastRenameOperations.Count > 0;
            RefreshListView();
            MessageBox.Show(this, FormatText("UndoRestored", restored), TextOf("UndoLast"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ContextMenuCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_contextMenuCheckBox.Checked)
                {
                    RegistryHelper.InstallContextMenu(TextOf("ContextMenuText"));
                }
                else
                {
                    RegistryHelper.RemoveContextMenu();
                }
            }
            catch (Exception ex)
            {
                _contextMenuCheckBox.CheckedChanged -= ContextMenuCheckBox_CheckedChanged;
                _contextMenuCheckBox.Checked = !_contextMenuCheckBox.Checked;
                _contextMenuCheckBox.CheckedChanged += ContextMenuCheckBox_CheckedChanged;
                MessageBox.Show(this, ex.Message, TextOf("RegistryErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateEntryAfterMove(FileEntry entry, string targetPath)
        {
            PopulateEntryFromPath(entry, targetPath, entry.IsDirectory);
            entry.NewName = string.Empty;
            entry.Status = "Renamed";
        }

        private void UpdateEntryAfterUndo(string newPath, string originalPath, bool isDirectory)
        {
            FileEntry entry = _entries.FirstOrDefault(x => string.Equals(x.FullPath, newPath, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                return;
            }

            PopulateEntryFromPath(entry, originalPath, isDirectory);
            entry.NewName = string.Empty;
            entry.Status = "Undo restored";
        }

        private void MarkEntryStatus(string path, string status)
        {
            FileEntry entry = _entries.FirstOrDefault(x => string.Equals(x.FullPath, path, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                entry.Status = status;
            }
        }

        private static IEnumerable<FileEntry> GetApplyOrder(IEnumerable<FileEntry> entries)
        {
            return entries
                .OrderBy(x => x.IsDirectory ? 1 : 0)
                .ThenByDescending(x => GetPathDepth(x.FullPath));
        }

        private static int GetPathDepth(string path)
        {
            return string.IsNullOrEmpty(path) ? 0 : path.Count(x => x == Path.DirectorySeparatorChar || x == Path.AltDirectorySeparatorChar);
        }

        private static bool PathExists(string path)
        {
            return File.Exists(path) || System.IO.Directory.Exists(path);
        }

        private static void MovePath(string sourcePath, string targetPath, bool isDirectory)
        {
            if (isDirectory)
            {
                System.IO.Directory.Move(sourcePath, targetPath);
            }
            else
            {
                File.Move(sourcePath, targetPath);
            }
        }

        private void UpdateDescendantPaths(string oldDirectoryPath, string newDirectoryPath)
        {
            string oldPrefix = oldDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (FileEntry entry in _entries)
            {
                if (!entry.FullPath.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = entry.FullPath.Substring(oldPrefix.Length);
                string updatedPath = Path.Combine(newDirectoryPath, relativePath);
                PopulateEntryFromPath(entry, updatedPath, entry.IsDirectory);
            }
        }

        private void LoadTemplateIntoEditor(string name)
        {
            try
            {
                ScriptTemplate template = LoadTemplates().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (template == null)
                {
                    MessageBox.Show(this, TextOf("TemplateMissing"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _staticScriptTextBox.Text = template.StaticScript ?? string.Empty;
                _dynamicScriptTextBox.Text = template.DynamicScript ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateLoadErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSortTemplateIntoEditor(string name)
        {
            try
            {
                SortScriptTemplate template = LoadSortTemplates().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (template == null)
                {
                    MessageBox.Show(this, TextOf("TemplateMissing"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _sortScriptTextBox.Text = string.IsNullOrWhiteSpace(template.SortScript) ? GetDefaultSortScript() : template.SortScript;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateLoadErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string PromptTemplateName(string title, string prompt, IEnumerable<string> names)
        {
            using (var dialog = new TemplateNameDialog(title, prompt, names))
            {
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.TemplateName : null;
            }
        }

        private static List<ScriptTemplate> LoadTemplates()
        {
            string path = GetTemplateFilePath();
            if (!File.Exists(path))
            {
                return new List<ScriptTemplate>();
            }

            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Length == 0)
                {
                    return new List<ScriptTemplate>();
                }

                var serializer = new DataContractJsonSerializer(typeof(ScriptTemplateStore));
                var store = serializer.ReadObject(stream) as ScriptTemplateStore;
                return store == null || store.Templates == null ? new List<ScriptTemplate>() : store.Templates;
            }
        }

        private static void SaveTemplates(List<ScriptTemplate> templates)
        {
            var store = new ScriptTemplateStore
            {
                Templates = templates
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
            };

            using (FileStream stream = File.Create(GetTemplateFilePath()))
            {
                var serializer = new DataContractJsonSerializer(typeof(ScriptTemplateStore));
                serializer.WriteObject(stream, store);
            }
        }

        private static List<SortScriptTemplate> LoadSortTemplates()
        {
            string path = GetSortTemplateFilePath();
            if (!File.Exists(path))
            {
                return new List<SortScriptTemplate>();
            }

            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Length == 0)
                {
                    return new List<SortScriptTemplate>();
                }

                var serializer = new DataContractJsonSerializer(typeof(SortScriptTemplateStore));
                var store = serializer.ReadObject(stream) as SortScriptTemplateStore;
                return store == null || store.Templates == null ? new List<SortScriptTemplate>() : store.Templates;
            }
        }

        private static void SaveSortTemplates(List<SortScriptTemplate> templates)
        {
            var store = new SortScriptTemplateStore
            {
                Templates = templates
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
            };

            using (FileStream stream = File.Create(GetSortTemplateFilePath()))
            {
                var serializer = new DataContractJsonSerializer(typeof(SortScriptTemplateStore));
                serializer.WriteObject(stream, store);
            }
        }

        private static string GetTemplateFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplateFileName);
        }

        private static string GetSortTemplateFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SortTemplateFileName);
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddPaths(paths);
        }

        private void AddPaths(IEnumerable<string> paths)
        {
            if (!EnsureNoSortPreview())
            {
                return;
            }

            var items = new List<EntryCandidate>();
            foreach (string inputPath in paths.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                try
                {
                    if (File.Exists(inputPath))
                    {
                        items.Add(new EntryCandidate(inputPath, false));
                    }
                    else if (System.IO.Directory.Exists(inputPath))
                    {
                        items.AddRange(System.IO.Directory.EnumerateDirectories(inputPath, "*", SearchOption.TopDirectoryOnly).Select(x => new EntryCandidate(x, true)));
                        items.AddRange(System.IO.Directory.EnumerateFiles(inputPath, "*", SearchOption.TopDirectoryOnly).Select(x => new EntryCandidate(x, false)));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, inputPath + "\r\n" + ex.Message, TextOf("AddErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            var existing = new HashSet<string>(_entries.Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
            foreach (EntryCandidate item in items.GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase).Select(x => x.First()))
            {
                if (existing.Contains(item.FullPath))
                {
                    continue;
                }

                try
                {
                    _entries.Add(CreateEntry(item.FullPath, item.IsDirectory));
                    existing.Add(item.FullPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, item.FullPath + "\r\n" + ex.Message, TextOf("FileErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            RefreshListView();
        }

        private FileEntry CreateEntry(string path, bool isDirectory)
        {
            var entry = new FileEntry();
            PopulateEntryFromPath(entry, path, isDirectory);
            entry.Status = "Added";
            return entry;
        }

        private static void PopulateEntryFromPath(FileEntry entry, string path, bool isDirectory)
        {
            if (isDirectory)
            {
                var info = new DirectoryInfo(path);
                entry.FullPath = info.FullName;
                entry.CurrentName = info.Name;
                entry.DirectoryPath = info.Parent == null ? string.Empty : info.Parent.FullName;
                entry.Size = 0;
                entry.Type = "DIR";
                entry.IsDirectory = true;
                entry.IsFile = false;
                entry.IsImage = false;
                entry.IsMusic = false;
                entry.IsVideo = false;
                entry.IsApp = false;
                entry.Created = info.CreationTime;
                entry.Modified = info.LastWriteTime;
                entry.Accessed = info.LastAccessTime;
                entry.Attributes = info.Attributes;
                entry.Meta = LoadDirectoryMetadata(info);
                return;
            }

            var fileInfo = new FileInfo(path);
            bool isImage = IsImageFile(fileInfo.Extension);
            bool isMusic = IsAudioFile(fileInfo.Extension);
            bool isVideo = IsVideoFile(fileInfo.Extension);
            bool isApp = IsApplicationFile(fileInfo.Extension);

            entry.FullPath = fileInfo.FullName;
            entry.CurrentName = fileInfo.Name;
            entry.DirectoryPath = fileInfo.DirectoryName ?? string.Empty;
            entry.Size = fileInfo.Length;
            entry.Type = fileInfo.Extension.TrimStart('.').ToUpperInvariant();
            entry.IsDirectory = false;
            entry.IsFile = true;
            entry.IsImage = isImage;
            entry.IsMusic = isMusic;
            entry.IsVideo = isVideo;
            entry.IsApp = isApp;
            entry.Created = fileInfo.CreationTime;
            entry.Modified = fileInfo.LastWriteTime;
            entry.Accessed = fileInfo.LastAccessTime;
            entry.Attributes = fileInfo.Attributes;
            entry.Meta = LoadMetadata(fileInfo, isImage, isMusic, isVideo, isApp);
        }

        private void SimulateRenames(bool showMessage)
        {
            string staticScript = _staticScriptTextBox.Text;
            string dynamicScript = _dynamicScriptTextBox.Text;
            var proposedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Engine engine;

            try
            {
                engine = CreateScriptEngine();
                if (!string.IsNullOrWhiteSpace(staticScript))
                {
                    engine.Execute(staticScript);
                }
            }
            catch (JavaScriptException ex)
            {
                MarkAllEntriesAsScriptError("Static JS Error: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                MarkAllEntriesAsScriptError("Static Error: " + ex.Message);
                return;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                FileEntry entry = _entries[i];
                try
                {
                    string result = ExecuteDynamicScript(engine, dynamicScript, entry, i);
                    string sanitized = SanitizeFileName(result);

                    if (string.IsNullOrWhiteSpace(sanitized))
                    {
                        entry.NewName = string.Empty;
                        entry.Status = "Invalid: empty name";
                    }
                    else if (sanitized == entry.CurrentName)
                    {
                        entry.NewName = sanitized;
                        entry.Status = "Unchanged";
                    }
                    else
                    {
                        string targetPath = Path.Combine(entry.DirectoryPath, sanitized);
                        if (!proposedTargets.Add(targetPath))
                        {
                            entry.NewName = sanitized;
                            entry.Status = "Invalid: duplicate target";
                        }
                        else if (PathExists(targetPath))
                        {
                            entry.NewName = sanitized;
                            entry.Status = "Invalid: target exists";
                        }
                        else
                        {
                            entry.NewName = sanitized;
                            entry.Status = "Ready";
                        }
                    }
                }
                catch (JavaScriptException ex)
                {
                    entry.NewName = string.Empty;
                    entry.Status = "JS Error: " + ex.Message;
                }
                catch (Exception ex)
                {
                    entry.NewName = string.Empty;
                    entry.Status = "Error: " + ex.Message;
                }
            }

            RefreshListView();

            if (showMessage)
            {
                MessageBox.Show(this, TextOf("SimulationComplete"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void MarkAllEntriesAsScriptError(string status)
        {
            foreach (FileEntry entry in _entries)
            {
                entry.NewName = string.Empty;
                entry.Status = status;
            }

            RefreshListView();
            MessageBox.Show(this, status, AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static Engine CreateScriptEngine()
        {
            return new Engine(options =>
            {
                options.TimeoutInterval(TimeSpan.FromSeconds(3));
                options.LimitRecursion(64);
            });
        }

        private static string ExecuteDynamicScript(Engine engine, string script, FileEntry entry, int index)
        {
            SetEntryVariables(engine, entry, index);

            string wrappedScript = "(function(){\r\n" + script + "\r\n})()";
            object value = engine.Evaluate(wrappedScript).ToObject();
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static object ExecuteSortScript(Engine engine, string script, FileEntry entry, int index)
        {
            SetEntryVariables(engine, entry, index);

            string wrappedScript = "(function(){\r\n" + script + "\r\n})()";
            return engine.Evaluate(wrappedScript).ToObject();
        }

        private static void SetEntryVariables(Engine engine, FileEntry entry, int index)
        {
            engine.SetValue("name", GetScriptName(entry));
            engine.SetValue("ext", GetScriptExtension(entry));
            engine.SetValue("path", entry.DirectoryPath);
            engine.SetValue("index", index);
            engine.SetValue("isDirectory", entry.IsDirectory);
            engine.SetValue("isFile", entry.IsFile);
            engine.SetValue("isImage", entry.IsImage);
            engine.SetValue("isMusic", entry.IsMusic);
            engine.SetValue("isVideo", entry.IsVideo);
            engine.SetValue("isApp", entry.IsApp);
            engine.SetValue("size", entry.Size);
            engine.SetValue("fullName", entry.FullPath);
            engine.SetValue("attributes", entry.Attributes.ToString());
            engine.SetValue("meta", entry.Meta);
            engine.SetValue("__createdIso", entry.Created.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            engine.SetValue("__modifiedIso", entry.Modified.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            engine.SetValue("__accessedIso", entry.Accessed.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            engine.Execute("var created = new Date(__createdIso); var modified = new Date(__modifiedIso); var accessed = new Date(__accessedIso);");
        }

        private static string GetScriptName(FileEntry entry)
        {
            return entry.IsDirectory ? entry.CurrentName : Path.GetFileNameWithoutExtension(entry.CurrentName);
        }

        private static string GetScriptExtension(FileEntry entry)
        {
            return entry.IsDirectory ? string.Empty : Path.GetExtension(entry.CurrentName);
        }

        private static FileMeta LoadMetadata(FileInfo info, bool isImage, bool isMusic, bool isVideo, bool isApp)
        {
            var meta = new FileMeta();
            PopulateFileSystemMetadata(info, meta, info.Length, isDirectory: false);

            if (isImage)
            {
                try
                {
                    string filePath = info.FullName;
                    IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePath);
                    var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                    var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                    var gps = directories.OfType<GpsDirectory>().FirstOrDefault();

                    int width = GetFirstInt(directories, ExifDirectoryBase.TagExifImageWidth, ExifDirectoryBase.TagImageWidth);
                    int height = GetFirstInt(directories, ExifDirectoryBase.TagExifImageHeight, ExifDirectoryBase.TagImageHeight);

                    try
                    {
                        using (Image image = Image.FromFile(filePath))
                        {
                            if (width <= 0 || height <= 0)
                            {
                                width = image.Width;
                                height = image.Height;
                            }

                            meta.dpiX = image.HorizontalResolution;
                            meta.dpiY = image.VerticalResolution;
                        }
                    }
                    catch
                    {
                        // Some image formats expose metadata but cannot be opened by System.Drawing.
                    }

                    meta.width = width;
                    meta.height = height;
                    meta.cameraMake = ifd0 == null ? string.Empty : SafeDescription(ifd0, ExifDirectoryBase.TagMake);
                    meta.cameraModel = ifd0 == null ? string.Empty : SafeDescription(ifd0, ExifDirectoryBase.TagModel);
                    meta.dateTaken = subIfd == null ? string.Empty : SafeDescription(subIfd, ExifDirectoryBase.TagDateTimeOriginal);
                    meta.digitizedDate = subIfd == null ? string.Empty : SafeDescription(subIfd, ExifDirectoryBase.TagDateTimeDigitized);
                    meta.fStop = subIfd == null ? string.Empty : SafeDescription(subIfd, ExifDirectoryBase.TagFNumber);
                    meta.exposureTime = subIfd == null ? string.Empty : SafeDescription(subIfd, ExifDirectoryBase.TagExposureTime);
                    meta.iso = GetFirstInt(directories, ExifDirectoryBase.TagIsoEquivalent, ExifDirectoryBase.TagIsoSpeed);
                    meta.focalLength = subIfd == null ? string.Empty : SafeDescription(subIfd, ExifDirectoryBase.TagFocalLength);
                    meta.orientation = ifd0 == null ? string.Empty : SafeDescription(ifd0, ExifDirectoryBase.TagOrientation);

                    if (gps != null)
                    {
                        GeoLocation? location = gps.GetGeoLocation();
                        if (location.HasValue && !location.Value.IsZero)
                        {
                            meta.gpsLatitude = location.Value.Latitude;
                            meta.gpsLongitude = location.Value.Longitude;
                        }
                    }
                }
                catch
                {
                    // Metadata is optional; the file can still be renamed.
                }
            }

            if (isMusic || isVideo)
            {
                try
                {
                    using (TagLib.File tagFile = TagLib.File.Create(info.FullName))
                    {
                        meta.artist = FirstOrEmpty(tagFile.Tag.Performers);
                        meta.artists = JoinOrEmpty(tagFile.Tag.Performers);
                        meta.album = tagFile.Tag.Album ?? string.Empty;
                        meta.title = tagFile.Tag.Title ?? string.Empty;
                        meta.duration = tagFile.Properties.Duration.TotalSeconds;
                        meta.durationText = FormatDuration(tagFile.Properties.Duration);
                        meta.year = tagFile.Tag.Year;
                        meta.genre = FirstOrEmpty(tagFile.Tag.Genres);
                        meta.trackNumber = tagFile.Tag.Track;
                        meta.bpm = tagFile.Tag.BeatsPerMinute;
                        meta.audioChannels = tagFile.Properties.AudioChannels;
                        meta.audioSampleRate = tagFile.Properties.AudioSampleRate;
                        meta.audioBitrateKbps = tagFile.Properties.AudioBitrate;
                        meta.videoWidth = tagFile.Properties.VideoWidth;
                        meta.videoHeight = tagFile.Properties.VideoHeight;
                        meta.bitrateKbps = CalculateBitrateKbps(info.Length, tagFile.Properties.Duration);
                        meta.videoCodec = FirstCodecDescription(tagFile.Properties.Codecs, TagLib.MediaTypes.Video);
                        meta.audioCodec = FirstCodecDescription(tagFile.Properties.Codecs, TagLib.MediaTypes.Audio);
                    }
                }
                catch
                {
                    // Unsupported or damaged media files are allowed in the list.
                }
            }

            if (isApp)
            {
                PopulateApplicationMetadata(info.FullName, meta);
            }

            return meta;
        }

        private static FileMeta LoadDirectoryMetadata(DirectoryInfo info)
        {
            var meta = new FileMeta();
            PopulateFileSystemMetadata(info, meta, 0, isDirectory: true);
            return meta;
        }

        private static void PopulateFileSystemMetadata(FileSystemInfo info, FileMeta meta, long sizeBytes, bool isDirectory)
        {
            meta.name = isDirectory ? info.Name : Path.GetFileNameWithoutExtension(info.Name);
            meta.extension = isDirectory ? string.Empty : info.Extension;
            meta.fullName = info.FullName;
            meta.path = isDirectory
                ? (((DirectoryInfo)info).Parent == null ? string.Empty : ((DirectoryInfo)info).Parent.FullName)
                : (((FileInfo)info).DirectoryName ?? string.Empty);
            meta.sizeBytes = sizeBytes;
            meta.sizeText = FormatSize(sizeBytes);
            meta.isDirectory = isDirectory;
            meta.isFile = !isDirectory;
            meta.creationDate = info.CreationTime.ToString("o", CultureInfo.InvariantCulture);
            meta.modifiedDate = info.LastWriteTime.ToString("o", CultureInfo.InvariantCulture);
            meta.accessedDate = info.LastAccessTime.ToString("o", CultureInfo.InvariantCulture);
            meta.attributes = info.Attributes.ToString();
            meta.isReadOnly = (info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            meta.isHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            meta.isSystem = (info.Attributes & FileAttributes.System) == FileAttributes.System;
            meta.isArchive = (info.Attributes & FileAttributes.Archive) == FileAttributes.Archive;
        }

        private static void PopulateApplicationMetadata(string filePath, FileMeta meta)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                meta.productName = versionInfo.ProductName ?? string.Empty;
                meta.fileVersion = versionInfo.FileVersion ?? string.Empty;
                meta.copyright = versionInfo.LegalCopyright ?? string.Empty;
                meta.description = versionInfo.FileDescription ?? string.Empty;
            }
            catch
            {
                // Version metadata is optional.
            }

            try
            {
                var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
                meta.isSigned = true;
                meta.publisher = certificate.GetNameInfo(X509NameType.SimpleName, false);

                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    meta.signatureValid = chain.Build(certificate);
                }
            }
            catch
            {
                meta.isSigned = false;
                meta.signatureValid = false;
                meta.publisher = string.Empty;
            }
        }

        private static int GetFirstInt(IEnumerable<MetadataExtractor.Directory> directories, params int[] tags)
        {
            foreach (MetadataExtractor.Directory directory in directories)
            {
                foreach (int tag in tags)
                {
                    if (directory.TryGetInt32(tag, out int value))
                    {
                        return value;
                    }
                }
            }

            return 0;
        }

        private static string SafeDescription(MetadataExtractor.Directory directory, int tag)
        {
            return directory.ContainsTag(tag) ? directory.GetDescription(tag) ?? string.Empty : string.Empty;
        }

        private static string FirstOrEmpty(string[] values)
        {
            return values == null || values.Length == 0 ? string.Empty : values[0] ?? string.Empty;
        }

        private static string JoinOrEmpty(string[] values)
        {
            return values == null || values.Length == 0 ? string.Empty : string.Join("; ", values.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalHours >= 1 ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture) : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        private static int CalculateBitrateKbps(long bytes, TimeSpan duration)
        {
            if (duration.TotalSeconds <= 0)
            {
                return 0;
            }

            return (int)Math.Round((bytes * 8.0) / duration.TotalSeconds / 1000.0);
        }

        private static string FirstCodecDescription(IEnumerable<TagLib.ICodec> codecs, TagLib.MediaTypes mediaType)
        {
            if (codecs == null)
            {
                return string.Empty;
            }

            TagLib.ICodec codec = codecs.FirstOrDefault(x => (x.MediaTypes & mediaType) == mediaType);
            return codec == null ? string.Empty : codec.Description ?? string.Empty;
        }

        private static string SanitizeFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            var builder = new StringBuilder(fileName.Length);

            foreach (char c in fileName)
            {
                builder.Append(invalidChars.IndexOf(c) >= 0 ? '_' : c);
            }

            return builder.ToString().Trim().TrimEnd('.');
        }

        private static bool IsImageFile(string extension)
        {
            string ext = extension.ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".tif" || ext == ".tiff" || ext == ".webp";
        }

        private static bool IsAudioFile(string extension)
        {
            string ext = extension.ToLowerInvariant();
            return ext == ".mp3" || ext == ".flac" || ext == ".wav" || ext == ".m4a" || ext == ".aac" || ext == ".ogg" || ext == ".wma";
        }

        private static bool IsVideoFile(string extension)
        {
            string ext = extension.ToLowerInvariant();
            return ext == ".mp4" || ext == ".m4v" || ext == ".mov" || ext == ".avi" || ext == ".mkv" || ext == ".wmv" || ext == ".webm";
        }

        private static bool IsApplicationFile(string extension)
        {
            string ext = extension.ToLowerInvariant();
            return ext == ".exe" || ext == ".dll";
        }

        private void RefreshListView()
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (FileEntry entry in _entries)
            {
                var item = new ListViewItem(entry.CurrentName);
                item.SubItems.Add(entry.NewName);
                item.SubItems.Add(entry.DirectoryPath);
                item.SubItems.Add(FormatSize(entry.Size));
                item.SubItems.Add(entry.Type);
                item.SubItems.Add(entry.Status);
                item.Tag = entry;

                if (entry.Status.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                    entry.Status.StartsWith("Invalid", StringComparison.OrdinalIgnoreCase) ||
                    entry.Status.StartsWith("JS Error", StringComparison.OrdinalIgnoreCase))
                {
                    item.ForeColor = Color.Firebrick;
                }
                else if (entry.Status == "Ready")
                {
                    item.ForeColor = Color.DarkGreen;
                }

                _listView.Items.Add(item);
            }

            _listView.EndUpdate();
            _countLabel.Text = FormatItemCount(_entries.Count);
        }

        private static string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int suffix = 0;

            while (size >= 1024 && suffix < suffixes.Length - 1)
            {
                size /= 1024;
                suffix++;
            }

            return size.ToString(size >= 10 || suffix == 0 ? "0" : "0.0", CultureInfo.InvariantCulture) + " " + suffixes[suffix];
        }

        private sealed class FileEntry
        {
            public string FullPath { get; set; }
            public string CurrentName { get; set; }
            public string NewName { get; set; }
            public string DirectoryPath { get; set; }
            public long Size { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }
            public bool IsDirectory { get; set; }
            public bool IsFile { get; set; }
            public bool IsImage { get; set; }
            public bool IsMusic { get; set; }
            public bool IsVideo { get; set; }
            public bool IsApp { get; set; }
            public DateTime Created { get; set; }
            public DateTime Modified { get; set; }
            public DateTime Accessed { get; set; }
            public FileAttributes Attributes { get; set; }
            public FileMeta Meta { get; set; }
        }

        private sealed class RenameOperation
        {
            public RenameOperation(string originalPath, string newPath, bool isDirectory)
            {
                OriginalPath = originalPath;
                NewPath = newPath;
                IsDirectory = isDirectory;
            }

            public string OriginalPath { get; private set; }
            public string NewPath { get; private set; }
            public bool IsDirectory { get; private set; }
        }

        private sealed class EntryCandidate
        {
            public EntryCandidate(string fullPath, bool isDirectory)
            {
                FullPath = fullPath;
                IsDirectory = isDirectory;
            }

            public string FullPath { get; private set; }
            public bool IsDirectory { get; private set; }
        }

        private sealed class SortItem
        {
            public SortItem(FileEntry entry, object key, int originalIndex)
            {
                Entry = entry;
                Key = key;
                OriginalIndex = originalIndex;
            }

            public FileEntry Entry { get; private set; }
            public object Key { get; private set; }
            public int OriginalIndex { get; private set; }
        }

        private sealed class SortKeyComparer : IComparer<object>
        {
            public static readonly SortKeyComparer Instance = new SortKeyComparer();

            public int Compare(object x, object y)
            {
                bool xIsNumber = IsNumber(x);
                bool yIsNumber = IsNumber(y);

                if (xIsNumber && yIsNumber)
                {
                    return Convert.ToDouble(x, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(y, CultureInfo.InvariantCulture));
                }

                return string.Compare(
                    Convert.ToString(x, CultureInfo.CurrentCulture) ?? string.Empty,
                    Convert.ToString(y, CultureInfo.CurrentCulture) ?? string.Empty,
                    StringComparison.CurrentCultureIgnoreCase);
            }

            private static bool IsNumber(object value)
            {
                return value is byte || value is sbyte ||
                       value is short || value is ushort ||
                       value is int || value is uint ||
                       value is long || value is ulong ||
                       value is float || value is double ||
                       value is decimal;
            }
        }

        [DataContract]
        private sealed class ScriptTemplateStore
        {
            [DataMember(Name = "templates")]
            public List<ScriptTemplate> Templates { get; set; } = new List<ScriptTemplate>();
        }

        [DataContract]
        private sealed class ScriptTemplate
        {
            [DataMember(Name = "name")]
            public string Name { get; set; } = string.Empty;

            [DataMember(Name = "staticScript")]
            public string StaticScript { get; set; } = string.Empty;

            [DataMember(Name = "dynamicScript")]
            public string DynamicScript { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class SortScriptTemplateStore
        {
            [DataMember(Name = "templates")]
            public List<SortScriptTemplate> Templates { get; set; } = new List<SortScriptTemplate>();
        }

        [DataContract]
        private sealed class SortScriptTemplate
        {
            [DataMember(Name = "name")]
            public string Name { get; set; } = string.Empty;

            [DataMember(Name = "sortScript")]
            public string SortScript { get; set; } = string.Empty;
        }

        private sealed class TemplateNameDialog : Form
        {
            private readonly ComboBox _nameComboBox;

            public TemplateNameDialog(string title, string prompt, IEnumerable<string> names)
            {
                Text = title;
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MinimizeBox = false;
                MaximizeBox = false;
                ClientSize = new Size(360, 132);

                var label = new Label
                {
                    Text = prompt,
                    AutoSize = false,
                    Left = 16,
                    Top = 16,
                    Width = 328,
                    Height = 24
                };

                _nameComboBox = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDown,
                    Left = 16,
                    Top = 48,
                    Width = 328
                };

                foreach (string name in names.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
                {
                    _nameComboBox.Items.Add(name);
                }

                if (_nameComboBox.Items.Count > 0)
                {
                    _nameComboBox.SelectedIndex = 0;
                }

                var okButton = new Button
                {
                    Text = TextOf("DialogOk"),
                    DialogResult = DialogResult.OK,
                    Left = 138,
                    Top = 88,
                    Width = 100,
                    Height = 28
                };

                var cancelButton = new Button
                {
                    Text = TextOf("DialogCancel"),
                    DialogResult = DialogResult.Cancel,
                    Left = 244,
                    Top = 88,
                    Width = 100,
                    Height = 28
                };

                AcceptButton = okButton;
                CancelButton = cancelButton;
                Controls.Add(label);
                Controls.Add(_nameComboBox);
                Controls.Add(okButton);
                Controls.Add(cancelButton);
            }

            public string TemplateName
            {
                get { return Convert.ToString(_nameComboBox.Text, CultureInfo.CurrentCulture).Trim(); }
            }
        }
    }

    public sealed class FileMeta
    {
        public string name { get; set; } = string.Empty;
        public string extension { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
        public long sizeBytes { get; set; }
        public string sizeText { get; set; } = string.Empty;
        public string creationDate { get; set; } = string.Empty;
        public string modifiedDate { get; set; } = string.Empty;
        public string accessedDate { get; set; } = string.Empty;
        public string attributes { get; set; } = string.Empty;
        public bool isDirectory { get; set; }
        public bool isFile { get; set; }
        public bool isReadOnly { get; set; }
        public bool isHidden { get; set; }
        public bool isSystem { get; set; }
        public bool isArchive { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public float dpiX { get; set; }
        public float dpiY { get; set; }
        public string artist { get; set; } = string.Empty;
        public string artists { get; set; } = string.Empty;
        public string album { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public double duration { get; set; }
        public string durationText { get; set; } = string.Empty;
        public uint year { get; set; }
        public string genre { get; set; } = string.Empty;
        public uint trackNumber { get; set; }
        public uint bpm { get; set; }
        public string cameraMake { get; set; } = string.Empty;
        public string cameraModel { get; set; } = string.Empty;
        public string dateTaken { get; set; } = string.Empty;
        public string digitizedDate { get; set; } = string.Empty;
        public string fStop { get; set; } = string.Empty;
        public string exposureTime { get; set; } = string.Empty;
        public int iso { get; set; }
        public string focalLength { get; set; } = string.Empty;
        public double gpsLatitude { get; set; }
        public double gpsLongitude { get; set; }
        public string orientation { get; set; } = string.Empty;
        public int videoWidth { get; set; }
        public int videoHeight { get; set; }
        public int bitrateKbps { get; set; }
        public double frameRate { get; set; }
        public int audioChannels { get; set; }
        public int audioSampleRate { get; set; }
        public int audioBitrateKbps { get; set; }
        public string videoCodec { get; set; } = string.Empty;
        public string audioCodec { get; set; } = string.Empty;
        public string productName { get; set; } = string.Empty;
        public string fileVersion { get; set; } = string.Empty;
        public string copyright { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public bool isSigned { get; set; }
        public bool signatureValid { get; set; }
        public string publisher { get; set; } = string.Empty;
    }
}
