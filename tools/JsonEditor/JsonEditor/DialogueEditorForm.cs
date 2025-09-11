// DialogueEditorForm.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SimpleDungeon.Engine.Dialogue;
using System.IO;

namespace SimpleDungeon.Tools.DialogueEditor
{
    /// <summary>
    /// DialogueEditorForm с поддержкой имени диалога (Name).
    /// Замените существующий файл этим содержимым.
    /// Требует: Newtonsoft.Json, типы DialogueDocument/DialogueNode/Response/DialogueAction.
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

        // State
        private DialogueDocument _doc = new DialogueDocument { Nodes = new List<DialogueNode>() };
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

            // Responses list
            var lbResp = new Label { Left = 250, Top = 250, Text = "Responses", Width = 80 };
            Controls.Add(lbResp);

            _responsesList = new ListView { Left = 250, Top = 270, Width = 700, Height = 250, View = View.Details, FullRowSelect = true };
            _responsesList.Columns.Add("Text", 420);
            _responsesList.Columns.Add("Target", 160);
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
            if (_doc == null) _doc = new DialogueDocument { Nodes = new List<DialogueNode>() };
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

                // изменить id и обновить все target'ы, которые ссылались на старый id
                var oldId = sel.Id;
                sel.Id = newId;
                foreach (var n in _doc.Nodes)
                {
                    if (n.Responses == null) continue;
                    foreach (var r in n.Responses)
                        if (r.Target == oldId) r.Target = newId;
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
            if (_doc == null) _doc = new DialogueDocument { Nodes = new List<DialogueNode>() };

            var nid = id;
            if (string.IsNullOrEmpty(nid))
            {
                int idx = 1;
                while (_doc.Nodes.Any(n => n.Id == $"n{idx}")) idx++;
                nid = $"n{idx}";
            }

            var node = new DialogueNode { Id = nid, Text = text ?? "" };
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
            // удалить ссылки на этот node в target у ответов
            foreach (var n in _doc.Nodes)
            {
                if (n.Responses == null) continue;
                foreach (var r in n.Responses)
                    if (r.Target == sel.Id) r.Target = null;
            }
            RefreshNodesList();
        }

        private DialogueNode GetSelectedNode()
        {
            return _nodesList.SelectedItem as DialogueNode;
        }

        private void SelectNodeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < _nodesList.Items.Count; i++)
            {
                if ((_nodesList.Items[i] as DialogueNode)?.Id == id)
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

        private void RefreshResponses(DialogueNode node)
        {
            _responsesList.Items.Clear();
            if (node?.Responses == null) return;
            foreach (var r in node.Responses)
            {
                var item = new ListViewItem(r.Text ?? "(empty)");
                item.SubItems.Add(r.Target ?? "(null)");
                item.Tag = r;
                _responsesList.Items.Add(item);
            }
        }

        // -------------------------
        // Response editing
        // -------------------------
        private void AddResponse()
        {
            var node = GetSelectedNode();
            if (node == null) return;
            var form = new EditResponseForm(_doc, null);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                node.Responses ??= new List<Response>();
                node.Responses.Add(form.Result);
                RefreshResponses(node);
            }
        }

        private void EditSelectedResponse()
        {
            if (_responsesList.SelectedItems.Count == 0) return;
            var item = _responsesList.SelectedItems[0];
            var resp = item.Tag as Response;
            var node = GetSelectedNode();
            if (resp == null || node == null) return;

            var form = new EditResponseForm(_doc, resp);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // изменения уже внесены в объект resp
                RefreshResponses(node);
            }
        }

        private void DeleteSelectedResponse()
        {
            if (_responsesList.SelectedItems.Count == 0) return;
            var item = _responsesList.SelectedItems[0];
            var resp = item.Tag as Response;
            var node = GetSelectedNode();
            if (resp == null || node == null) return;
            node.Responses.Remove(resp);
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

        public void EditDialogFromGameData(string gameDataPath, SimpleDungeon.Engine.Dialogue.DialogueDocument dialog)
        {
            if (string.IsNullOrEmpty(gameDataPath)) throw new ArgumentNullException(nameof(gameDataPath));
            if (dialog == null) dialog = new SimpleDungeon.Engine.Dialogue.DialogueDocument { Nodes = new System.Collections.Generic.List<SimpleDungeon.Engine.Dialogue.DialogueNode>() };

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
            if (_doc.Nodes == null) _doc.Nodes = new System.Collections.Generic.List<SimpleDungeon.Engine.Dialogue.DialogueNode>();
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

                // heuristics
                bool looksLikePlainDialogue = jobj["nodes"] != null && (jobj["start"] != null || jobj["nodes"].HasValues);
                bool hasDialogsArray = jobj.Properties().Any(p => string.Equals(p.Name, "dialogs", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);
                bool hasDialoguesArray = jobj.Properties().Any(p => string.Equals(p.Name, "dialogues", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);

                if (looksLikePlainDialogue && !hasDialogsArray && !hasDialoguesArray && jobj.Properties().Count() <= 6)
                {
                    _doc = jobj.ToObject<DialogueDocument>() ?? new DialogueDocument { Nodes = new List<DialogueNode>() };
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
                            _doc = new DialogueDocument { Nodes = new List<DialogueNode>() };
                        }
                        else
                        {
                            DialogueDocument found = null;
                            if (!string.IsNullOrWhiteSpace(_doc?.Id))
                            {
                                foreach (var it in array)
                                {
                                    try
                                    {
                                        var cand = it.ToObject<DialogueDocument>();
                                        if (cand != null && cand.Id == _doc.Id) { found = cand; break; }
                                    }
                                    catch { }
                                }
                            }
                            if (found == null)
                            {
                                found = array[0].ToObject<DialogueDocument>();
                            }
                            _doc = found ?? new DialogueDocument { Nodes = new List<DialogueNode>() };
                        }
                        _loadedJObject = jobj;
                    }
                    else
                    {
                        _doc = new DialogueDocument { Nodes = new List<DialogueNode>() };
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
                        _doc = anyDialogs[0].ToObject<DialogueDocument>() ?? new DialogueDocument { Nodes = new List<DialogueNode>() };
                        _loadedJObject = jobj;
                    }
                    else
                    {
                        _doc = new DialogueDocument { Nodes = new List<DialogueNode>() };
                        _loadedJObject = jobj;
                        MessageBox.Show("Файл содержит другие данные. Редактор загрузил пустой документ. При сохранении используйте «Save As...» чтобы избежать перезаписи исходного файла.");
                    }
                }
            }
            else
            {
                _doc = JsonConvert.DeserializeObject<DialogueDocument>(json) ?? new DialogueDocument { Nodes = new List<DialogueNode>() };
                _loadedJObject = null;
            }

            if (_doc.Nodes == null) _doc.Nodes = new List<DialogueNode>();
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
                    File.WriteAllText(tmpPath, original.ToString(Formatting.Indented));
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

                        File.WriteAllText(tmpPath, original.ToString(Formatting.Indented));
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

                var json = JsonConvert.SerializeObject(root, settings);
                File.WriteAllText(path, json);
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
    }
}
