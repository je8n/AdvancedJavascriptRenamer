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
        private readonly List<FileEntry> _entries = new List<FileEntry>();
        private readonly List<RenameOperation> _lastRenameOperations = new List<RenameOperation>();
        private bool _loadingTemplateList;
        private ListView _listView;
        private TextBox _staticScriptTextBox;
        private TextBox _dynamicScriptTextBox;
        private Button _addButton;
        private Button _simulateButton;
        private Button _applyButton;
        private Button _undoButton;
        private TextBox _templateNameTextBox;
        private ComboBox _templateComboBox;
        private Button _loadTemplateButton;
        private Button _saveTemplateButton;
        private CheckBox _contextMenuCheckBox;
        private Label _languageLabel;
        private ComboBox _languageComboBox;
        private Label _countLabel;
        private SplitContainer _mainSplit;
        private SplitContainer _editorSplit;
        private TabPage _staticTab;
        private TabPage _dynamicTab;
        private TextBox _guideTextBox;
        private bool _loadingLanguageList;

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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8, 7, 8, 4),
                WrapContents = false
            };

            _addButton = new Button { Text = TextOf("AddFilesFolders"), AutoSize = true, Height = 28 };
            _simulateButton = new Button { Text = TextOf("SimulatePreview"), AutoSize = true, Height = 28 };
            _applyButton = new Button { Text = TextOf("ApplyChanges"), AutoSize = true, Height = 28 };
            _undoButton = new Button { Text = TextOf("UndoLast"), AutoSize = true, Height = 28, Enabled = false };
            _templateNameTextBox = new TextBox { Width = 130, Height = 23, Margin = new Padding(18, 5, 3, 3) };
            _templateComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Height = 23, Margin = new Padding(3, 5, 3, 3) };
            _loadTemplateButton = new Button { Text = TextOf("LoadTemplate"), AutoSize = true, Height = 28 };
            _saveTemplateButton = new Button { Text = TextOf("SaveTemplate"), AutoSize = true, Height = 28 };
            _contextMenuCheckBox = new CheckBox { Text = TextOf("AddToContextMenu"), AutoSize = true, Height = 28, Margin = new Padding(18, 6, 3, 3) };
            _languageLabel = new Label { Text = TextOf("LanguageShortLabel"), AutoSize = true, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(12, 8, 3, 3) };
            _languageComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 23, Margin = new Padding(3, 5, 3, 3) };
            _countLabel = new Label { Text = FormatItemCount(0), AutoSize = true, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(18, 8, 3, 3) };

            _addButton.Click += AddButton_Click;
            _simulateButton.Click += SimulateButton_Click;
            _applyButton.Click += ApplyButton_Click;
            _undoButton.Click += UndoButton_Click;
            _loadTemplateButton.Click += LoadTemplateButton_Click;
            _saveTemplateButton.Click += SaveTemplateButton_Click;
            _templateComboBox.SelectedIndexChanged += TemplateComboBox_SelectedIndexChanged;
            _contextMenuCheckBox.CheckedChanged += ContextMenuCheckBox_CheckedChanged;
            _contextMenuCheckBox.Checked = RegistryHelper.IsContextMenuInstalled();
            LoadLanguageComboBox();
            _languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;

            toolbar.Controls.Add(_addButton);
            toolbar.Controls.Add(_simulateButton);
            toolbar.Controls.Add(_applyButton);
            toolbar.Controls.Add(_undoButton);
            toolbar.Controls.Add(_templateNameTextBox);
            toolbar.Controls.Add(_templateComboBox);
            toolbar.Controls.Add(_loadTemplateButton);
            toolbar.Controls.Add(_saveTemplateButton);
            toolbar.Controls.Add(_contextMenuCheckBox);
            toolbar.Controls.Add(_languageLabel);
            toolbar.Controls.Add(_languageComboBox);
            toolbar.Controls.Add(_countLabel);

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
            _dynamicTab = new TabPage(TextOf("DynamicTab"));

            _staticScriptTextBox = CreateScriptTextBox(GetDefaultStaticScript());
            _dynamicScriptTextBox = CreateScriptTextBox(GetDefaultDynamicScript());
            _staticTab.Controls.Add(_staticScriptTextBox);
            _dynamicTab.Controls.Add(_dynamicScriptTextBox);
            scriptTabs.TabPages.Add(_staticTab);
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
            RefreshTemplateList(selectName: null);
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
            SimulateRenames(showMessage: true);
        }

        private void LoadTemplateButton_Click(object sender, EventArgs e)
        {
            string name = Convert.ToString(_templateComboBox.SelectedItem, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, TextOf("TemplateNotSelected"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            LoadTemplateIntoEditor(name);
        }

        private void SaveTemplateButton_Click(object sender, EventArgs e)
        {
            string name = _templateNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Convert.ToString(_templateComboBox.SelectedItem, CultureInfo.InvariantCulture);
            }

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
                RefreshTemplateList(name);
                MessageBox.Show(this, TextOf("TemplateSaved"), AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateSaveErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TemplateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingTemplateList)
            {
                return;
            }

            string name = Convert.ToString(_templateComboBox.SelectedItem, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(name))
            {
                LoadTemplateIntoEditor(name);
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
            _loadTemplateButton.Text = TextOf("LoadTemplate");
            _saveTemplateButton.Text = TextOf("SaveTemplate");
            _contextMenuCheckBox.Text = TextOf("AddToContextMenu");
            _languageLabel.Text = TextOf("LanguageShortLabel");
            _countLabel.Text = FormatItemCount(_entries.Count);

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

        private void ApplyButton_Click(object sender, EventArgs e)
        {
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

            foreach (FileEntry entry in ready)
            {
                try
                {
                    string sourcePath = entry.FullPath;
                    string targetPath = Path.Combine(entry.DirectoryPath, entry.NewName);
                    if (File.Exists(targetPath))
                    {
                        entry.Status = "Skipped: target exists";
                        continue;
                    }

                    File.Move(sourcePath, targetPath);
                    _lastRenameOperations.Add(new RenameOperation(sourcePath, targetPath));
                    entry.FullPath = targetPath;
                    entry.CurrentName = Path.GetFileName(targetPath);
                    entry.NewName = string.Empty;
                    entry.Status = "Renamed";
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
                    if (!File.Exists(operation.NewPath))
                    {
                        MarkEntryStatus(operation.NewPath, "Undo skipped: renamed file missing");
                        remaining.Add(operation);
                        continue;
                    }

                    if (File.Exists(operation.OriginalPath))
                    {
                        MarkEntryStatus(operation.NewPath, "Undo skipped: original exists");
                        remaining.Add(operation);
                        continue;
                    }

                    File.Move(operation.NewPath, operation.OriginalPath);
                    UpdateEntryAfterUndo(operation.NewPath, operation.OriginalPath);
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

        private void UpdateEntryAfterUndo(string newPath, string originalPath)
        {
            FileEntry entry = _entries.FirstOrDefault(x => string.Equals(x.FullPath, newPath, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                return;
            }

            var info = new FileInfo(originalPath);
            entry.FullPath = info.FullName;
            entry.CurrentName = info.Name;
            entry.NewName = string.Empty;
            entry.DirectoryPath = info.DirectoryName ?? string.Empty;
            entry.Size = info.Length;
            entry.Type = info.Extension.TrimStart('.').ToUpperInvariant();
            entry.Created = info.CreationTime;
            entry.Modified = info.LastWriteTime;
            entry.Accessed = info.LastAccessTime;
            entry.Attributes = info.Attributes;
            entry.Status = "Undo restored";
            entry.Meta = LoadMetadata(info, entry.IsImage, entry.IsMusic, entry.IsVideo, entry.IsApp);
        }

        private void MarkEntryStatus(string path, string status)
        {
            FileEntry entry = _entries.FirstOrDefault(x => string.Equals(x.FullPath, path, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                entry.Status = status;
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
                    RefreshTemplateList(selectName: null);
                    return;
                }

                _templateNameTextBox.Text = template.Name;
                _staticScriptTextBox.Text = template.StaticScript ?? string.Empty;
                _dynamicScriptTextBox.Text = template.DynamicScript ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateLoadErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTemplateList(string selectName)
        {
            try
            {
                _loadingTemplateList = true;
                _templateComboBox.Items.Clear();

                foreach (ScriptTemplate template in LoadTemplates().OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(template.Name))
                    {
                        _templateComboBox.Items.Add(template.Name);
                    }
                }

                if (!string.IsNullOrWhiteSpace(selectName))
                {
                    for (int i = 0; i < _templateComboBox.Items.Count; i++)
                    {
                        if (string.Equals(Convert.ToString(_templateComboBox.Items[i], CultureInfo.InvariantCulture), selectName, StringComparison.OrdinalIgnoreCase))
                        {
                            _templateComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TextOf("TemplateListErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loadingTemplateList = false;
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

        private static string GetTemplateFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplateFileName);
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
            var files = new List<string>();
            foreach (string inputPath in paths.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                try
                {
                    if (File.Exists(inputPath))
                    {
                        files.Add(inputPath);
                    }
                    else if (System.IO.Directory.Exists(inputPath))
                    {
                        files.AddRange(System.IO.Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, inputPath + "\r\n" + ex.Message, TextOf("AddErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            var existing = new HashSet<string>(_entries.Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
            foreach (string file in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (existing.Contains(file))
                {
                    continue;
                }

                try
                {
                    _entries.Add(CreateEntry(file));
                    existing.Add(file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, file + "\r\n" + ex.Message, TextOf("FileErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            RefreshListView();
        }

        private FileEntry CreateEntry(string filePath)
        {
            var info = new FileInfo(filePath);
            bool isImage = IsImageFile(info.Extension);
            bool isMusic = IsAudioFile(info.Extension);
            bool isVideo = IsVideoFile(info.Extension);
            bool isApp = IsApplicationFile(info.Extension);

            return new FileEntry
            {
                FullPath = info.FullName,
                CurrentName = info.Name,
                DirectoryPath = info.DirectoryName ?? string.Empty,
                Size = info.Length,
                Type = info.Extension.TrimStart('.').ToUpperInvariant(),
                IsImage = isImage,
                IsMusic = isMusic,
                IsVideo = isVideo,
                IsApp = isApp,
                Created = info.CreationTime,
                Modified = info.LastWriteTime,
                Accessed = info.LastAccessTime,
                Attributes = info.Attributes,
                Meta = LoadMetadata(info, isImage, isMusic, isVideo, isApp),
                Status = "Added"
            };
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
                        else if (File.Exists(targetPath))
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

        private static void SetEntryVariables(Engine engine, FileEntry entry, int index)
        {
            engine.SetValue("name", Path.GetFileNameWithoutExtension(entry.CurrentName));
            engine.SetValue("ext", Path.GetExtension(entry.CurrentName));
            engine.SetValue("path", entry.DirectoryPath);
            engine.SetValue("index", index);
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

        private static FileMeta LoadMetadata(FileInfo info, bool isImage, bool isMusic, bool isVideo, bool isApp)
        {
            var meta = new FileMeta();
            PopulateFileMetadata(info, meta);

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

        private static void PopulateFileMetadata(FileInfo info, FileMeta meta)
        {
            meta.name = Path.GetFileNameWithoutExtension(info.Name);
            meta.extension = info.Extension;
            meta.fullName = info.FullName;
            meta.path = info.DirectoryName ?? string.Empty;
            meta.sizeBytes = info.Length;
            meta.sizeText = FormatSize(info.Length);
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
            public RenameOperation(string originalPath, string newPath)
            {
                OriginalPath = originalPath;
                NewPath = newPath;
            }

            public string OriginalPath { get; private set; }
            public string NewPath { get; private set; }
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
