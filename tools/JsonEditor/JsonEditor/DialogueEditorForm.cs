// DialogueEditorForm.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Engine.Data;

namespace SimpleDungeon.Tools.DialogueEditor
{
    /// <summary>
    /// DialogueEditorForm переписан для работы с моделями Engine.Data:
    /// DialogueData, DialogueNodeData, DialogueChoiceData.
    /// UI — тот же, что и у тебя (без Designer).
    /// </summary>
    public class DialogueEditorForm : Form
    {
        // UI
        private readonly ListBox _nodesList;
        private readonly TextBox _nodeId;
        private readonly TextBox _nodeText;
        private readonly ListView _responsesList;
        private readonly Button _addNodeBtn;
        private readonly Button _delNodeBtn;
        private readonly Button _addRespBtn;
        private readonly Button _editRespBtn;
        private readonly Button _delRespBtn;
        private readonly Button _saveAsBtn;
        private readonly Button _loadBtn;
        private readonly Button _setStartBtn;
        private readonly Label _startLabel;

        // File/id controls
        private readonly TextBox _docIdBox;
        private readonly TextBox _docNameBox; // <- новое поле для имени диалога
        private readonly TextBox _filePathBox;
        private readonly Button _saveCurrentBtn;
        private readonly Label _statusLabel;

        // State (теперь DialogueData)
        private DialogueData _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };
        private string _currentFilePath;
        private JObject _loadedJObject; // если открыт контейнер (game_data.json)

        public DialogueEditorForm()
        {
            Text = "Dialogue Editor";
            Width = 1000;
            Height = 740;
            StartPosition = FormStartPosition.CenterScreen;

            // Left: nodes list and control buttons
            _nodesList = new ListBox { Left = 10, Top = 10, Width = 220, Height = 540 };
            _nodesList.SelectedIndexChanged += NodesList_SelectedIndexChanged;
            Controls.Add(_nodesList);

            _addNodeBtn = new Button { Left = 10, Top = 560, Width = 70, Text = "New" };
            _addNodeBtn.Click += (s, e) => { CreateNewNode(); };
            Controls.Add(_addNodeBtn);

            _delNodeBtn = new Button { Left = 90, Top = 560, Width = 70, Text = "Delete" };
            _delNodeBtn.Click += (s, e) => { DeleteSelectedNode(); };
            Controls.Add(_delNodeBtn);

            _setStartBtn = new Button { Left = 170, Top = 560, Width = 60, Text = "SetStart" };
            _setStartBtn.Click += (s, e) => { SetStartNode(); };
            Controls.Add(_setStartBtn);

            _startLabel = new Label { Left = 10, Top = 590, Width = 220, Height = 40, Text = "Start: (none)" };
            Controls.Add(_startLabel);

            // Right: node properties
            var lbId = new Label { Left = 250, Top = 10, Text = "Node ID", Width = 80 };
            Controls.Add(lbId);
            _nodeId = new TextBox { Left = 250, Top = 30, Width = 320 };
            _nodeId.Leave += NodeId_Leave;
            Controls.Add(_nodeId);

            var lbText = new Label { Left = 250, Top = 60, Text = "Text", Width = 80 };
            Controls.Add(lbText);
            _nodeText = new TextBox { Left = 250, Top = 80, Width = 700, Height = 160, Multiline = true, ScrollBars = ScrollBars.Vertical };
            _nodeText.Leave += NodeText_Leave;
            Controls.Add(_nodeText);

            // Responses list (Choices)
            var lbResp = new Label { Left = 250, Top = 250, Text = "Responses / Choices", Width = 140 };
            Controls.Add(lbResp);

            _responsesList = new ListView { Left = 250, Top = 270, Width = 700, Height = 250, View = View.Details, FullRowSelect = true };
            _responsesList.Columns.Add("Text", 420);
            _responsesList.Columns.Add("Target", 160);
            // Новая колонка с действием
            _responsesList.Columns.Add("Action", 120);
            _responsesList.DoubleClick += (s, e) => EditSelectedResponse();
            Controls.Add(_responsesList);

            _addRespBtn = new Button { Left = 250, Top = 530, Width = 80, Text = "Add" };
            _addRespBtn.Click += (s, e) => AddResponse();
            Controls.Add(_addRespBtn);

            _editRespBtn = new Button { Left = 340, Top = 530, Width = 80, Text = "Edit" };
            _editRespBtn.Click += (s, e) => EditSelectedResponse();
            Controls.Add(_editRespBtn);

            _delRespBtn = new Button { Left = 430, Top = 530, Width = 80, Text = "Del" };
            _delRespBtn.Click += (s, e) => DeleteSelectedResponse();
            Controls.Add(_delRespBtn);

            // Bottom: load/save
            _saveAsBtn = new Button { Left = 250, Top = 570, Width = 120, Text = "Save As..." };
            _saveAsBtn.Click += (s, e) => SaveToFile();
            Controls.Add(_saveAsBtn);

            _loadBtn = new Button { Left = 380, Top = 570, Width = 120, Text = "Load JSON..." };
            _loadBtn.Click += (s, e) => LoadFromFile();
            Controls.Add(_loadBtn);

            // File/id controls (нижняя панель)
            var lbDocId = new Label { Left = 10, Top = 630, Width = 60, Text = "Doc ID" };
            Controls.Add(lbDocId);
            _docIdBox = new TextBox { Left = 70, Top = 628, Width = 140 };
            _docIdBox.Leave += (s, e) => { if (_doc != null) _doc.Id = _docIdBox.Text?.Trim(); };
            Controls.Add(_docIdBox);

            var lbDocName = new Label { Left = 220, Top = 630, Width = 90, Text = "Dialogue Name" };
            Controls.Add(lbDocName);
            _docNameBox = new TextBox { Left = 310, Top = 628, Width = 300 };
            _docNameBox.Leave += DocNameBox_Leave;
            Controls.Add(_docNameBox);

            _filePathBox = new TextBox { Left = 620, Top = 628, Width = 260, ReadOnly = true };
            Controls.Add(_filePathBox);

            _saveCurrentBtn = new Button { Left = 890, Top = 624, Width = 80, Text = "Save" };
            _saveCurrentBtn.Click += (s, e) => SaveToCurrentFile();
            Controls.Add(_saveCurrentBtn);

            _statusLabel = new Label { Left = 10, Top = 660, Width = 960, Height = 40, Text = "" };
            Controls.Add(_statusLabel);

            // Initialize doc
            if (_doc == null) _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };
            CreateNewNode("n1", "Новый узел");
            RefreshNodesList();
        }

        private void DocNameBox_Leave(object sender, EventArgs e)
        {
            if (_doc == null) return;
            _doc.Name = _docNameBox.Text?.Trim();
        }

        // -------------------------
        // UI / data sync methods
        // -------------------------
        private void NodeId_Leave(object sender, EventArgs e)
        {
            var sel = GetSelectedNode();
            if (sel == null) return;
            var newId = _nodeId.Text?.Trim();
            if (string.IsNullOrEmpty(newId))
            {
                MessageBox.Show("Id не может быть пустым");
                _nodeId.Text = sel.Id;
                return;
            }
            if (newId != sel.Id)
            {
                // проверка на уникальность
                if (_doc.Nodes.Any(x => x.Id.Equals(newId, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Id уже используется");
                    _nodeId.Text = sel.Id;
                    return;
                }

                // изменить id и обновить все target'ы (NextNodeId), которые ссылались на старый id
                var oldId = sel.Id;
                sel.Id = newId;
                foreach (var n in _doc.Nodes)
                {
                    if (n.Choices == null) continue;
                    foreach (var c in n.Choices)
                        if (c.NextNodeId == oldId) c.NextNodeId = newId;
                }

                RefreshNodesList();
                SelectNodeById(newId);
            }
        }

        private void NodeText_Leave(object sender, EventArgs e)
        {
            var sel = GetSelectedNode();
            if (sel == null) return;
            sel.Text = _nodeText.Text;
            RefreshNodesList();
        }

        private void CreateNewNode(string id = null, string text = null)
        {
            if (_doc == null) _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };

            var nid = id;
            if (string.IsNullOrEmpty(nid))
            {
                int idx = 1;
                while (_doc.Nodes.Any(n => n.Id == $"n{idx}")) idx++;
                nid = $"n{idx}";
            }

            var node = new DialogueNodeData { Id = nid, Text = text ?? "", Choices = new List<DialogueChoiceData>() };
            _doc.Nodes.Add(node);
            RefreshNodesList();
            SelectNodeById(nid);
        }

        private void DeleteSelectedNode()
        {
            var sel = GetSelectedNode();
            if (sel == null) return;
            var confirm = MessageBox.Show($"Delete node {sel.Id} ?", "Confirm", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;
            _doc.Nodes.Remove(sel);
            // удалить ссылки на этот node в NextNodeId у choices
            foreach (var n in _doc.Nodes)
            {
                if (n.Choices == null) continue;
                foreach (var c in n.Choices)
                    if (c.NextNodeId == sel.Id) c.NextNodeId = null;
            }
            RefreshNodesList();
        }

        private DialogueNodeData GetSelectedNode()
        {
            return _nodesList.SelectedItem as DialogueNodeData;
        }

        private void SelectNodeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < _nodesList.Items.Count; i++)
            {
                if ((_nodesList.Items[i] as DialogueNodeData)?.Id == id)
                {
                    _nodesList.SelectedIndex = i;
                    return;
                }
            }
        }

        private void NodesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var node = GetSelectedNode();
            if (node == null)
            {
                _nodeId.Text = "";
                _nodeText.Text = "";
                _responsesList.Items.Clear();
                return;
            }
            _nodeId.Text = node.Id;
            _nodeText.Text = node.Text ?? "";
            RefreshResponses(node);
            _startLabel.Text = "Start: " + (_doc.Start ?? "(none)");
        }

        private void RefreshNodesList()
        {
            _nodesList.DataSource = null;
            _nodesList.DataSource = _doc.Nodes;
            _nodesList.DisplayMember = "Id";

            _startLabel.Text = "Start: " + (_doc.Start ?? "(none)");
            _docIdBox.Text = _doc?.Id ?? "";
            _docNameBox.Text = _doc?.Name ?? ""; // отображаем имя
            _filePathBox.Text = _currentFilePath ?? "";
            _statusLabel.Text = _loadedJObject != null ? $"container: {_currentFilePath}" : $"file: {_currentFilePath}";
            if (_nodesList.Items.Count > 0)
            {
                _nodesList.SelectedIndex = Math.Min(_nodesList.SelectedIndex == -1 ? 0 : _nodesList.SelectedIndex, _nodesList.Items.Count - 1);
            }
        }

        private void RefreshResponses(DialogueNodeData node)
        {
            _responsesList.Items.Clear();
            if (node?.Choices == null) return;
            foreach (var c in node.Choices)
            {
                var item = new ListViewItem(c.Text ?? "(empty)");
                item.SubItems.Add(c.NextNodeId ?? "(null)");
                item.SubItems.Add(c.ActionSummary);
                item.Tag = c;
                _responsesList.Items.Add(item);
            }
        }

        // -------------------------
        // Choice editing (замена старого Response редактирования)
        // -------------------------
        private void AddResponse()
        {
            var node = GetSelectedNode();
            if (node == null) return;
            var form = new EditChoiceForm(_doc, null);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                node.Choices ??= new List<DialogueChoiceData>();
                node.Choices.Add(form.Result);
                RefreshResponses(node);
            }
        }

        private void EditSelectedResponse()
        {
            if (_responsesList.SelectedItems.Count == 0) return;
            var item = _responsesList.SelectedItems[0];
            var choice = item.Tag as DialogueChoiceData;
            var node = GetSelectedNode();
            if (choice == null || node == null) return;

            var form = new EditChoiceForm(_doc, choice);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // изменения уже внесены в объект choice (формой)
                RefreshResponses(node);
            }
        }

        private void DeleteSelectedResponse()
        {
            if (_responsesList.SelectedItems.Count == 0) return;
            var item = _responsesList.SelectedItems[0];
            var choice = item.Tag as DialogueChoiceData;
            var node = GetSelectedNode();
            if (choice == null || node == null) return;
            node.Choices.Remove(choice);
            RefreshResponses(node);
        }

        private void SetStartNode()
        {
            var node = GetSelectedNode();
            if (node == null) return;
            _doc.Start = node.Id;
            _startLabel.Text = "Start: " + _doc.Start;
            MessageBox.Show($"Start set to {node.Id}");
        }

        // -------------------------
        // Load / Save logic (UI)
        // -------------------------
        private void SaveToFile()
        {
            var dlg = new SaveFileDialog { Filter = "JSON files|*.json", DefaultExt = "json" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                SaveToPath(dlg.FileName);
                MessageBox.Show("Saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        private void LoadFromFile()
        {
            var dlg = new OpenFileDialog { Filter = "JSON files|*.json", DefaultExt = "json" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                LoadFromPath(dlg.FileName);
                MessageBox.Show("Loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load failed: " + ex.Message);
            }
        }

        // -------------------------
        // Public programmatic API
        // -------------------------
        public void LoadFileProgrammatically(string path)
        {
            LoadFromPath(path);
        }

        // Раньше принимал старый DialogueDocument, теперь — DialogueData
        public void EditDialogFromGameData(string gameDataPath, DialogueData dialog)
        {
            if (string.IsNullOrEmpty(gameDataPath)) throw new ArgumentNullException(nameof(gameDataPath));
            if (dialog == null) dialog = new DialogueData { Nodes = new System.Collections.Generic.List<DialogueNodeData>() };

            try
            {
                var txt = System.IO.File.ReadAllText(gameDataPath);
                var tok = JToken.Parse(txt);
                if (tok.Type == JTokenType.Object)
                {
                    _loadedJObject = (JObject)tok;
                    _currentFilePath = gameDataPath;
                }
                else
                {
                    _loadedJObject = null;
                    _currentFilePath = gameDataPath;
                }
            }
            catch
            {
                _loadedJObject = null;
                _currentFilePath = gameDataPath;
            }

            _doc = dialog;
            if (_doc.Nodes == null) _doc.Nodes = new System.Collections.Generic.List<DialogueNodeData>();
            RefreshNodesList();
        }

        public void LoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            var json = System.IO.File.ReadAllText(path);
            _currentFilePath = path;
            _loadedJObject = null;

            var token = JToken.Parse(json);

            if (token.Type == JTokenType.Object)
            {
                var jobj = (JObject)token;

                // heuristics (поддержка как плейн-диалога, так и контейнера GameData)
                bool looksLikePlainDialogue = jobj["nodes"] != null && (jobj["start"] != null || jobj["nodes"].HasValues);
                bool hasDialogsArray = jobj.Properties().Any(p => string.Equals(p.Name, "dialogs", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);
                bool hasDialoguesArray = jobj.Properties().Any(p => string.Equals(p.Name, "dialogues", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);

                if (looksLikePlainDialogue && !hasDialogsArray && !hasDialoguesArray && jobj.Properties().Count() <= 6)
                {
                    // single dialogue object — попытаемся распарсить в new model (или конвертировать старую структуру)
                    _doc = ParseDialogueTokenToDialogueData(jobj) ?? new DialogueData { Nodes = new List<DialogueNodeData>() };
                    _loadedJObject = null;
                }
                else if (hasDialogsArray || hasDialoguesArray)
                {
                    // find the first matching array property ignoring case
                    JArray array = null;
                    foreach (var p in jobj.Properties())
                    {
                        if (p.Value.Type == JTokenType.Array && (p.Name.Equals("dialogs", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("dialogues", StringComparison.OrdinalIgnoreCase)))
                        {
                            array = (JArray)p.Value;
                            break;
                        }
                    }

                    if (array == null)
                    {
                        // fallback: find any array whose first element looks like a dialogue (has nodes)
                        foreach (var p in jobj.Properties())
                        {
                            if (p.Value.Type == JTokenType.Array)
                            {
                                var a = (JArray)p.Value;
                                if (a.Count > 0 && a[0].Type == JTokenType.Object && ((JObject)a[0])["nodes"] != null)
                                {
                                    array = a;
                                    break;
                                }
                            }
                        }
                    }

                    if (array != null)
                    {
                        if (array.Count == 0)
                        {
                            _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };
                        }
                        else
                        {
                            DialogueData found = null;
                            if (!string.IsNullOrWhiteSpace(_doc?.Id))
                            {
                                foreach (var it in array)
                                {
                                    try
                                    {
                                        var cand = ParseDialogueTokenToDialogueData(it);
                                        if (cand != null && cand.Id == _doc.Id) { found = cand; break; }
                                    }
                                    catch { }
                                }
                            }
                            if (found == null)
                            {
                                found = ParseDialogueTokenToDialogueData(array[0]) ?? new DialogueData { Nodes = new List<DialogueNodeData>() };
                            }
                            _doc = found ?? new DialogueData { Nodes = new List<DialogueNodeData>() };
                        }
                        _loadedJObject = jobj;
                    }
                    else
                    {
                        _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };
                        _loadedJObject = jobj;
                        MessageBox.Show("Файл содержит другие данные. Редактор загрузил пустой документ. При сохранении используйте «Save As...» чтобы избежать перезаписи исходного файла.");
                    }
                }
                else
                {
                    // try to find any array that looks like dialogs
                    JArray anyDialogs = null;
                    foreach (var p in jobj.Properties())
                    {
                        if (p.Value.Type == JTokenType.Array)
                        {
                            var a = (JArray)p.Value;
                            if (a.Count > 0 && a[0].Type == JTokenType.Object && ((JObject)a[0])["nodes"] != null)
                            {
                                anyDialogs = a;
                                break;
                            }
                        }
                    }
                    if (anyDialogs != null)
                    {
                        _doc = ParseDialogueTokenToDialogueData(anyDialogs[0]) ?? new DialogueData { Nodes = new List<DialogueNodeData>() };
                        _loadedJObject = jobj;
                    }
                    else
                    {
                        _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };
                        _loadedJObject = jobj;
                        MessageBox.Show("Файл содержит другие данные. Редактор загрузил пустой документ. При сохранении используйте «Save As...» чтобы избежать перезаписи исходного файла.");
                    }
                }
            }
            else
            {
                // token is not object — try to deserialize as DialogueData directly
                try
                {
                    _doc = JsonConvert.DeserializeObject<DialogueData>(json) ?? new DialogueData { Nodes = new List<DialogueNodeData>() };
                    _loadedJObject = null;
                }
                catch
                {
                    _doc = new DialogueData { Nodes = new List<DialogueNodeData>() };
                    _loadedJObject = null;
                }
            }

            if (_doc.Nodes == null) _doc.Nodes = new List<DialogueNodeData>();
            RefreshNodesList();
        }

        /// <summary>
        /// Сохранить в конкретный путь.
        /// Ищет массив диалогов регистронезависимо (Dialogues/dialogues/Dialogs...).
        /// Если найден — выполняет upsert (replace/add) по Id.
        /// Если не найден — создаёт поле "Dialogues" и добавляет туда диалог (без удаления других данных).
        /// Всегда делает резервную копию (*.bak) и сохраняет атомарно (tmp -> replace).
        /// </summary>
        public void SaveToPath(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            // Подготовка сериализации
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };

            JObject original = null;
            bool parsedOriginal = false;

            if (File.Exists(path))
            {
                try
                {
                    var txt = File.ReadAllText(path);
                    var tok = JToken.Parse(txt);
                    if (tok.Type == JTokenType.Object) { original = (JObject)tok; parsedOriginal = true; }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось распарсить целевой файл: {ex.Message}. Сохранение отменено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Преобразуем текущий документ в JObject (docJson)
            if (string.IsNullOrWhiteSpace(_doc?.Id))
            {
                MessageBox.Show("Документ должен иметь id перед сохранением в этот файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ensure Name is taken from the UI (in case user edited it and didn't lose focus)
            _doc.Name = _docNameBox.Text?.Trim();

            var docJson = JObject.Parse(JsonConvert.SerializeObject(_doc, settings));

            if (parsedOriginal && original != null)
            {
                // Попытка найти массив диалогов (регистронезависимо) среди явных имён
                JArray arr = null;
                string arrKey = null;

                var candidateNames = new[] { "dialogs", "dialogues", "dialogsList", "dialogs_list", "Dialogues", "Dialogs" };
                foreach (var p in original.Properties())
                {
                    if (p.Value.Type != JTokenType.Array) continue;
                    if (candidateNames.Any(c => string.Equals(p.Name, c, StringComparison.OrdinalIgnoreCase)))
                    {
                        arr = (JArray)p.Value;
                        arrKey = p.Name;
                        break;
                    }
                }

                // Fallback: любая JArray, чей первый элемент похож на диалог (имеет поле nodes)
                if (arr == null)
                {
                    foreach (var p in original.Properties())
                    {
                        if (p.Value.Type == JTokenType.Array)
                        {
                            var a = (JArray)p.Value;
                            if (a.Count == 0) continue;
                            if (a[0].Type == JTokenType.Object && ((JObject)a[0]).Properties().Any(pp => string.Equals(pp.Name, "nodes", StringComparison.OrdinalIgnoreCase)))
                            {
                                arr = a;
                                arrKey = p.Name;
                                break;
                            }
                        }
                    }
                }

                // Если массив найден — сделать upsert
                if (arr != null)
                {
                    int foundIndex = -1;
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (arr[i] is JObject it)
                        {
                            var idToken = it["id"] ?? it["Id"] ?? it["ID"];
                            if (idToken != null && idToken.Type == JTokenType.String && idToken.ToString() == _doc.Id)
                            {
                                foundIndex = i;
                                break;
                            }
                        }
                    }

                    if (foundIndex >= 0) arr[foundIndex] = docJson;
                    else arr.Add(docJson);

                    // Safe write: tmp + replace + bak
                    var tmpPath = path + ".tmp";
                    var bakPath = path + ".bak";
                    File.WriteAllText(tmpPath, original.ToString(Newtonsoft.Json.Formatting.Indented));
                    if (File.Exists(bakPath)) File.Delete(bakPath);
                    try
                    {
                        File.Replace(tmpPath, path, bakPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // fallback: копирование, если File.Replace недоступен
                        File.Copy(path, bakPath, true);
                        File.Copy(tmpPath, path, true);
                        File.Delete(tmpPath);
                    }

                    _currentFilePath = path;
                    _loadedJObject = original;
                    RefreshNodesList();
                    MessageBox.Show($"Диалог сохранён в массив '{arrKey ?? "Dialogues"}' (upsert). Резервная копия: {Path.GetFileName(bakPath)}", "Сохранено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    // Если массив не найден — создаём новый "Dialogues" и вставляем диалог (сохраняем остальные поля)
                    original["Dialogues"] = new JArray { docJson };

                    // Safe write: tmp + replace + bak
                    try
                    {
                        var tmpPath = path + ".tmp";
                        var bakPath = path + ".bak";

                        File.WriteAllText(tmpPath, original.ToString(Newtonsoft.Json.Formatting.Indented));
                        if (File.Exists(bakPath)) File.Delete(bakPath);
                        try
                        {
                            File.Replace(tmpPath, path, bakPath);
                        }
                        catch (PlatformNotSupportedException)
                        {
                            File.Copy(path, bakPath, true);
                            File.Copy(tmpPath, path, true);
                            File.Delete(tmpPath);
                        }

                        _currentFilePath = path;
                        _loadedJObject = original;
                        RefreshNodesList();
                        MessageBox.Show("Поле \"Dialogues\" не было найдено — создано и диалог добавлен. Резервная копия сохранена.", "Сохранено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            // Если файла не было — создаём новый JSON с Dialogues
            try
            {
                var root = new JObject();
                root["Dialogues"] = new JArray { docJson };

                var jsonOut = JsonConvert.SerializeObject(root, settings);
                File.WriteAllText(path, jsonOut);
                _currentFilePath = path;
                _loadedJObject = root;
                RefreshNodesList();
                MessageBox.Show("Файл не найден — создан новый game_data.json с Dialogues и добавленным диалогом.", "Сохранено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveToCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveToFile();
                return;
            }

            try
            {
                SaveToPath(_currentFilePath);
                // Сообщения показываются внутри SaveToPath
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        // ---------- Вспомогательные методы ----------

        // Универсальный парсер: поддерживает старую структуру (responses/target) и новую (Choices/NextNodeId)
        private DialogueData ParseDialogueTokenToDialogueData(JToken token)
        {
            if (token == null || token.Type != JTokenType.Object) return null;
            var jobj = (JObject)token;

            var dlg = new DialogueData
            {
                Id = jobj["id"]?.ToString() ?? jobj["Id"]?.ToString(),
                Name = jobj["name"]?.ToString() ?? jobj["Name"]?.ToString(),
                Nodes = new List<DialogueNodeData>()
            };

            var nodesToken = jobj["nodes"] ?? jobj["Nodes"];
            if (nodesToken == null || nodesToken.Type != JTokenType.Array) return dlg;

            foreach (var n in nodesToken)
            {
                var nodeObj = n as JObject;
                if (nodeObj == null) continue;

                var nd = new DialogueNodeData
                {
                    Id = nodeObj["id"]?.ToString() ?? nodeObj["Id"]?.ToString() ?? Guid.NewGuid().ToString(),
                    Text = nodeObj["text"]?.ToString() ?? nodeObj["Text"]?.ToString() ?? string.Empty,
                    ParentId = nodeObj["parentId"]?.ToString() ?? nodeObj["ParentId"]?.ToString()
                };
                nd.Choices = new List<DialogueChoiceData>();

                // First: try new-style choices (Choices / choices)
                var choicesToken = nodeObj["choices"] ?? nodeObj["Choices"];
                if (choicesToken != null && choicesToken.Type == JTokenType.Array)
                {
                    foreach (var c in choicesToken)
                    {
                        if (c is JObject co)
                        {
                            var ch = new DialogueChoiceData
                            {
                                Text = co["text"]?.ToString() ?? co["Text"]?.ToString() ?? string.Empty,
                                NextNodeId = co["nextNodeId"]?.ToString() ?? co["NextNodeId"]?.ToString() ?? co["target"]?.ToString() ?? co["Target"]?.ToString()
                            };
                            nd.Choices.Add(ch);
                        }
                    }
                    dlg.Nodes.Add(nd);
                    continue;
                }

                // Second: try old-style responses (responses / Responses)
                var responsesToken = nodeObj["responses"] ?? nodeObj["Responses"];
                if (responsesToken != null && responsesToken.Type == JTokenType.Array)
                {
                    foreach (var r in responsesToken)
                    {
                        if (r is JObject ro)
                        {
                            var ch = new DialogueChoiceData
                            {
                                Text = ro["text"]?.ToString() ?? ro["Text"]?.ToString() ?? string.Empty,
                                NextNodeId = ro["target"]?.ToString() ?? ro["Target"]?.ToString() ?? ro["next"]?.ToString()
                            };
                            nd.Choices.Add(ch);
                        }
                    }
                    dlg.Nodes.Add(nd);
                    continue;
                }

                // If neither, try to parse any array property named something else
                dlg.Nodes.Add(nd);
            }



            return dlg;
        }

        // --------- Вспомогательная модалка для редактирования DialogueChoiceData -----------
        private class EditChoiceForm : Form
        {
            private readonly TextBox _txtText;
            private readonly TextBox _txtTarget;
            private readonly Button _ok;
            private readonly Button _cancel;
            public DialogueChoiceData Result { get; private set; }
            private readonly DialogueChoiceData _editing;
            private readonly DialogueData _doc;
            private ComboBox _comboAction;
            private TextBox _txtActionParam;
            private ListBox _lstActions;
            private Button _btnAddAction, _btnRemoveAction;

            public EditChoiceForm(DialogueData doc, DialogueChoiceData existing)
            {
                _doc = doc;
                _editing = existing;

                Text = existing == null ? "Add Choice" : "Edit Choice";
                Width = 540;
                Height = 300; // Увеличим высоту формы
                StartPosition = FormStartPosition.CenterParent;

                var lblAction = new Label { Left = 10, Top = 80, Width = 100, Text = "Действие" };
                _comboAction = new ComboBox { Left = 120, Top = 80, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
                _comboAction.Items.AddRange(Enum.GetValues(typeof(DialogueAction)).Cast<object>().ToArray());

                // TextBox для параметра
                var lblParam = new Label { Left = 10, Top = 110, Width = 100, Text = "Параметр" };
                _txtActionParam = new TextBox { Left = 120, Top = 110, Width = 200 };

                // ListBox для списка действий
                var lblActions = new Label { Left = 10, Top = 140, Width = 100, Text = "Список действий" };

                _lstActions = new ListBox { Left = 120, Top = 140, Width = 250, Height = 60 };

                // Кнопки управления действиями
                _btnAddAction = new Button { Left = 280, Top = 80, Width = 90, Text = "Добавить" };
                _btnRemoveAction = new Button { Left = 280, Top = 110, Width = 90, Text = "Удалить" };

                // Добавляем элементы на форму
                Controls.AddRange(new Control[] { lblAction, _comboAction, lblParam, _txtActionParam, lblActions, _lstActions, _btnAddAction, _btnRemoveAction });

                // Обработчики событий
                _btnAddAction.Click += (s, e) => AddAction();
                _btnRemoveAction.Click += (s, e) => RemoveAction();

                // Загрузка данных
                if (existing != null)
                {
                    _comboAction.SelectedItem = existing.Action;
                    _txtActionParam.Text = existing.ActionParameter;
                    if (existing.Actions != null)
                        foreach (var action in existing.Actions)
                            _lstActions.Items.Add(action);
                }

                var lb1 = new Label { Left = 10, Top = 10, Width = 80, Text = "Text" };
                Controls.Add(lb1);
                _txtText = new TextBox { Left = 100, Top = 10, Width = 400 };
                Controls.Add(_txtText);

                var lb2 = new Label { Left = 10, Top = 45, Width = 80, Text = "NextNodeId" };
                Controls.Add(lb2);
                _txtTarget = new TextBox { Left = 100, Top = 45, Width = 300 };
                Controls.Add(_txtTarget);

                var pickBtn = new Button { Left = 410, Top = 45, Width = 90, Text = "Pick node..." };
                pickBtn.Click += (s, e) => { ShowPickNodeMenu(); };
                Controls.Add(pickBtn);

                _ok = new Button { Left = 320, Top = 120, Width = 80, Text = "OK" };
                _ok.Click += (s, e) => { OnOk(); };
                Controls.Add(_ok);

                _cancel = new Button { Left = 410, Top = 120, Width = 80, Text = "Cancel" };
                _cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                Controls.Add(_cancel);

                if (existing != null)
                {
                    _txtText.Text = existing.Text;
                    _txtTarget.Text = existing.NextNodeId;
                }
            }

            private void AddAction()
            {
                if (_comboAction.SelectedItem != null && _comboAction.SelectedItem is DialogueAction actionType)
                {
                    var action = new DialogueActionData
                    {
                        Type = actionType,
                        Parameter = _txtActionParam.Text
                    };
                    _lstActions.Items.Add(action);
                }
            }

            private void RemoveAction()
            {
                if (_lstActions.SelectedIndex >= 0)
                    _lstActions.Items.RemoveAt(_lstActions.SelectedIndex);
            }



            private void ShowPickNodeMenu()
            {
                if (_doc?.Nodes == null || _doc.Nodes.Count == 0)
                {
                    MessageBox.Show("Нет узлов для выбора");
                    return;
                }
                var pick = new Form { Width = 300, Height = 400, StartPosition = FormStartPosition.CenterParent, Text = "Pick Node" };
                var lb = new ListBox { Dock = DockStyle.Fill };
                lb.DataSource = _doc.Nodes;
                lb.DisplayMember = "Id";
                pick.Controls.Add(lb);
                var ok = new Button { Text = "Select", Dock = DockStyle.Bottom, Height = 30 };
                ok.Click += (s, e) =>
                {
                    if (lb.SelectedItem is DialogueNodeData nd)
                    {
                        _txtTarget.Text = nd.Id;
                        pick.Close();
                    }
                };
                pick.Controls.Add(ok);
                pick.ShowDialog(this);
            }

            private void OnOk()
            {
                var text = _txtText.Text?.Trim() ?? string.Empty;
                var next = string.IsNullOrWhiteSpace(_txtTarget.Text) ? null : _txtTarget.Text.Trim();

                if (string.IsNullOrEmpty(text))
                {
                    if (MessageBox.Show("Text is empty. Continue?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        return;
                }

                if (_editing == null)
                {
                    Result = new DialogueChoiceData
                    {
                        Text = text,
                        NextNodeId = next,
                        Actions = _lstActions.Items.Cast<DialogueActionData>().ToList()
                    };
                }
                else
                {
                    _editing.Text = text;
                    _editing.NextNodeId = next;
                    _editing.Actions = _lstActions.Items.Cast<DialogueActionData>().ToList();
                    Result = _editing;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
