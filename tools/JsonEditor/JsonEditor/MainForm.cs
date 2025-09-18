using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Engine.Data;
using Engine.Dialogue;
using Engine.Quests;

namespace JsonEditor
{
    public class MainForm : Form
    {
        private GameData _gameData;
        private string _currentFilePath;

        private MenuStrip _menu;
        private StatusStrip _status;
        private ToolStripStatusLabel _statusLabel;
        private TabControl _tabs;
        private PropertyGrid _propertyGrid;

        // mapping: tabName -> (listObject, elementType, grid)
        private readonly Dictionary<string, (IList list, Type elementType, DataGridView grid)> _lists =
            new Dictionary<string, (IList, Type, DataGridView)>();

        public MainForm()
        {
            Text = "JsonEditor";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            InitUi();
            // создаём пустой GameData чтобы UI был доступен до загрузки файла
            _gameData = new GameData();
            BuildTabsFromGameData();
        }

        private void InitUi()
        {
            // Menu
            _menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            var open = new ToolStripMenuItem("Открыть...", null, (s, e) => OpenFile());
            var save = new ToolStripMenuItem("Сохранить", null, (s, e) => SaveFile());
            var saveAs = new ToolStripMenuItem("Сохранить как...", null, (s, e) => SaveFileAs());
            var exit = new ToolStripMenuItem("Выход", null, (s, e) => Close());
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { open, save, saveAs, new ToolStripSeparator(), exit });

            var dataMenu = new ToolStripMenuItem("Данные");
            // пункты добавим динамически после загрузки GameData
            dataMenu.DropDownOpening += (s, e) => RebuildDataMenu(dataMenu);

            var toolsMenu = new ToolStripMenuItem("Инструменты");
            var validateItem = new ToolStripMenuItem("Проверить синхронизацию с runtime", null, (s, e) => ValidateRuntimeSync());
            toolsMenu.DropDownItems.Add(validateItem);

            _menu.Items.AddRange(new[] { fileMenu, dataMenu, toolsMenu });

            // Status
            _status = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Готово");
            _status.Items.Add(_statusLabel);

            // Tabs and PropertyGrid
            _tabs = new TabControl { Dock = DockStyle.Fill };
            _propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Right,
                Width = 360,
                ToolbarVisible = true,
                PropertySort = PropertySort.CategorizedAlphabetical,
                HelpVisible = true
            };

            // Split: tabs left, propgrid right
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_tabs);
            panel.Controls.Add(_propertyGrid);

            Controls.Add(panel);
            Controls.Add(_menu);
            Controls.Add(_status);

            _menu.Dock = DockStyle.Top;
            _status.Dock = DockStyle.Bottom;

            // Resize handler to keep layout correct
            Resize += (s, e) => {
                _tabs.Size = new System.Drawing.Size(ClientSize.Width - _propertyGrid.Width, ClientSize.Height - _menu.Height - _status.Height);
                _propertyGrid.Height = ClientSize.Height - _menu.Height - _status.Height;
                _propertyGrid.Location = new System.Drawing.Point(ClientSize.Width - _propertyGrid.Width, _menu.Height);
            };
        }

        // Rebuild dynamic "Данные" menu so user can Add/Edit/Delete per collection
        private void RebuildDataMenu(ToolStripMenuItem dataMenu)
        {
            dataMenu.DropDownItems.Clear();
            foreach (var kv in _lists)
            {
                var name = kv.Key;
                var sub = new ToolStripMenuItem(name);
                sub.DropDownItems.Add(new ToolStripMenuItem($"Добавить {name}", null, (s, e) => AddToList(name)));
                sub.DropDownItems.Add(new ToolStripMenuItem($"Удалить выбранный в {name}", null, (s, e) => DeleteSelectedInList(name)));
                sub.DropDownItems.Add(new ToolStripMenuItem($"Редактировать выбранный в {name}", null, (s, e) => EditSelectedInList(name)));
                dataMenu.DropDownItems.Add(sub);
            }
        }

        private void OpenFile()
        {
            using var ofd = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _currentFilePath = ofd.FileName;
                _gameData = SerializerHelper.LoadGameData(_currentFilePath);
                if (_gameData == null) _gameData = new GameData();
                BuildTabsFromGameData();
                _statusLabel.Text = $"Загружено: {Path.GetFileName(_currentFilePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveFileAs();
                return;
            }

            try
            {
                SerializerHelper.SaveGameData(_gameData, _currentFilePath);
                _statusLabel.Text = $"Сохранено: {Path.GetFileName(_currentFilePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void SaveFileAs()
        {
            using var sfd = new SaveFileDialog { Filter = "JSON (*.json)|*.json" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            _currentFilePath = sfd.FileName;
            SaveFile();
        }

        // Построение вкладок по отражению GameData
        private void BuildTabsFromGameData()
        {
            _tabs.TabPages.Clear();
            _lists.Clear();

            if (_gameData == null)
            {
                _gameData = new GameData();
            }

            var gdType = _gameData.GetType();
            var props = gdType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                var pType = p.PropertyType;
                if (!pType.IsGenericType) continue;
                var gen = pType.GetGenericTypeDefinition();
                if (gen != typeof(List<>)) continue;

                var elemType = pType.GetGenericArguments()[0];
                // ensure list not null
                var listObj = p.GetValue(_gameData);
                if (listObj == null)
                {
                    listObj = Activator.CreateInstance(pType);
                    p.SetValue(_gameData, listObj);
                }

                var ilist = listObj as IList;
                if (ilist == null) continue;

                // create tab
                var tab = new TabPage(p.Name);
                var grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoGenerateColumns = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false
                };

                // Специальная обработка для помещений - показываем только корневые помещения
                IList displayList = ilist;
                if (p.Name == "Rooms")
                {
                    displayList = GetRootRooms();
                }

                grid.DataSource = displayList;
                grid.SelectionChanged += (s, e) => OnGridSelectionChanged(p.Name);
                grid.CellDoubleClick += (s, e) => OnGridDoubleClick(p.Name);

                tab.Controls.Add(grid);
                _tabs.TabPages.Add(tab);

                _lists[p.Name] = (ilist, elemType, grid);
            }

            // initial layout call
            _tabs.SelectedIndex = _tabs.TabPages.Count > 0 ? 0 : -1;
            // refresh sizes
            _tabs.Invalidate();

            SetupGridDoubleClick();
        }

        // событие выбора в гриде — покажем объект в PropertyGrid
        private void OnGridSelectionChanged(string listName)
        {
            if (!_lists.TryGetValue(listName, out var info)) return;
            var grid = info.grid;
            if (grid.SelectedRows.Count == 0)
            {
                _propertyGrid.SelectedObject = null;
                return;
            }

            var idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= info.list.Count) { _propertyGrid.SelectedObject = null; return; }

            var obj = info.list[idx];
            _propertyGrid.SelectedObject = obj;
            // подписываемся на изменение свойства, чтобы обновлять грид
            _propertyGrid.PropertyValueChanged -= PropertyGrid_PropertyValueChanged;
            _propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;
        }

        private void OnGridDoubleClick(string listName)
        {
            // показывает propertyGrid уже реализовано через OnGridSelectionChanged,
            // поэтому двойной клик — можно оставить для UX (ничего доп. не делаем)
            OnGridSelectionChanged(listName);
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // Обновляем текущий DataGridView, чтобы изменения отобразились
            if (_tabs.SelectedTab == null) return;
            var name = _tabs.SelectedTab.Text;
            if (!_lists.TryGetValue(name, out var info)) return;

            // перезадаём datasource, чтобы DataGridView обновил отображение
            var grid = info.grid;
            var current = grid.DataSource;
            grid.DataSource = null;
            grid.DataSource = info.list;
            grid.Refresh();
            _statusLabel.Text = "Изменения применены";
        }

        // Добавление элемента в коллекцию
        private void AddToList(string listName)
        {
            if (!_lists.TryGetValue(listName, out var info)) return;
            
            // Специальная обработка для квестов
            if (listName.ToLower() == "quests")
            {
                AddEnhancedQuest();
                return;
            }
            
            // Специальная обработка для диалогов
            if (listName.ToLower() == "dialogues")
            {
                AddNewDialogue();
                return;
            }
            
            // Специальная обработка для помещений
            if (listName.ToLower() == "rooms")
            {
                AddNewRoom();
                return;
            }
            
            // Специальная обработка для входов в помещения
            if (listName.ToLower() == "roomentrances")
            {
                AddNewRoomEntrance();
                return;
            }
            
            // Специальная обработка для сундуков
            if (listName.ToLower() == "chests")
            {
                AddNewChest();
                return;
            }
            
            var elemType = info.elementType;
            object newElem = null;
            try
            {
                newElem = Activator.CreateInstance(elemType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Невозможно создать экземпляр типа {elemType.Name}: {ex.Message}");
                return;
            }

            // Инициализация типичных свойств (ID/Id, Name и вложенных списков)
            TryInitDefaultPropertiesForNewElement(info.list, newElem);

            // Add to underlying list
            info.list.Add(newElem);

            // Refresh grid and select new row
            var grid = info.grid;
            grid.DataSource = null;
            grid.DataSource = info.list;
            grid.ClearSelection();
            var newIndex = info.list.Count - 1;
            if (newIndex >= 0)
            {
                grid.Rows[newIndex].Selected = true;
                grid.CurrentCell = grid.Rows[newIndex].Cells.Cast<DataGridViewCell>().FirstOrDefault();
            }

            _statusLabel.Text = $"Добавлен новый элемент в {listName}";
        }

        // Удаление выбранного элемента с подтверждением
        private void DeleteSelectedInList(string listName)
        {
            if (!_lists.TryGetValue(listName, out var info)) return;
            var grid = info.grid;
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Ничего не выбрано для удаления.");
                return;
            }

            var idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= info.list.Count) return;

            var obj = info.list[idx];
            var idProp = GetIdProperty(obj);
            var idVal = idProp?.GetValue(obj)?.ToString() ?? idx.ToString();
            var nameProp = obj.GetType().GetProperty("Name");
            var nameVal = nameProp?.GetValue(obj)?.ToString() ?? obj.GetType().Name;

            if (MessageBox.Show($"Удалить {nameVal} (ID={idVal})?", "Подтвердите удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            info.list.RemoveAt(idx);
            grid.DataSource = null;
            grid.DataSource = info.list;
            _propertyGrid.SelectedObject = null;
            _statusLabel.Text = $"Удалён элемент {nameVal}";
        }

        // Просто переключим фокус на PropertyGrid для редактирования
        private void EditSelectedInList(string listName)
        {
            // Специальная обработка для помещений
            if (listName.ToLower() == "rooms")
            {
                EditSelectedRoom();
                return;
            }
            
            // Специальная обработка для входов в помещения
            if (listName.ToLower() == "roomentrances")
            {
                EditSelectedRoomEntrance();
                return;
            }
            
            EditSelectedItem(listName);
        }

        // Умная инициализация стандартных полей для вновь созданного элемента
        private void TryInitDefaultPropertiesForNewElement(IList existingList, object newElem)
        {
            if (newElem == null) return;
            var t = newElem.GetType();

            // 1) Инициализируем вложенные списки (List<...>) пустыми экземплярами
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanWrite) continue;
                var pType = p.PropertyType;
                if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var inst = Activator.CreateInstance(pType);
                    p.SetValue(newElem, inst);
                }
            }

            // 2) Проставим Name (если есть) и ID/Id
            SetPropertyIfExists(newElem, "Name", $"Новый {t.Name}");

            // Проставляем ID/Id: если числовой — берем max+1, если строковый — GUID
            var idProp = GetIdPropertyByType(t);
            if (idProp != null && idProp.CanWrite)
            {
                var idType = idProp.PropertyType;
                if (IsIntegerType(idType))
                {
                    // вычислим max существующих
                    long max = 0;
                    foreach (var item in existingList)
                    {
                        try
                        {
                            var v = idProp.GetValue(item);
                            if (v == null) continue;
                            var num = Convert.ToInt64(v);
                            if (num > max) max = num;
                        }
                        catch { }
                    }
                    object next = Convert.ChangeType(max + 1, Nullable.GetUnderlyingType(idType) ?? idType);
                    idProp.SetValue(newElem, next);
                }
                else if (IsStringType(idType))
                {
                    idProp.SetValue(newElem, Guid.NewGuid().ToString());
                }
                else
                {
                    // попытка поставить GUID как fallback
                    try { idProp.SetValue(newElem, Convert.ChangeType(Guid.NewGuid().ToString(), idProp.PropertyType)); } catch { }
                }
            }
        }

        private PropertyInfo GetIdProperty(object obj)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            return GetIdPropertyByType(t);
        }

        private PropertyInfo GetIdPropertyByType(Type t)
        {
            // ищем ID / Id / identifier common names
            var candidates = new[] { "ID", "Id", "id", "Identifier", "identifier" };
            foreach (var name in candidates)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p != null) return p;
            }
            return null;
        }

        private bool IsIntegerType(Type t)
        {
            var u = Nullable.GetUnderlyingType(t) ?? t;
            return u == typeof(int) || u == typeof(long) || u == typeof(short) || u == typeof(byte);
        }
        private bool IsStringType(Type t) => (Nullable.GetUnderlyingType(t) ?? t) == typeof(string);

        private void SetPropertyIfExists(object target, string propName, object value)
        {
            var pi = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (pi == null || !pi.CanWrite) return;
            try
            {
                var converted = ConvertToTypeWithFallback(value, pi.PropertyType);
                pi.SetValue(target, converted);
            }
            catch { /* silently ignore */ }
        }

        private object ConvertToTypeWithFallback(object value, Type targetType)
        {
            if (value == null) return null;
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(string)) return value.ToString();
            if (underlying == typeof(int))
            {
                if (value is int i) return i;
                if (int.TryParse(value.ToString(), out var parsed)) return parsed;
                return 0;
            }
            if (underlying == typeof(long))
            {
                if (value is long l) return l;
                if (long.TryParse(value.ToString(), out var parsedL)) return parsedL;
                return 0L;
            }
            if (underlying == typeof(bool))
            {
                if (value is bool b) return b;
                if (bool.TryParse(value.ToString(), out var pb)) return pb;
                return false;
            }
            try { return Convert.ChangeType(value, underlying); } catch { return Activator.CreateInstance(underlying); }
        }

        // Добавьте этот метод в класс MainForm
        private void SetupGridDoubleClick()
        {
            foreach (var kv in _lists)
            {
                var grid = kv.Value.grid;
                grid.CellDoubleClick += (s, e) => EditSelectedItem(kv.Key);
            }
        }

        // Добавьте этот метод для обработки редактирования выбранного элемента
        private void EditSelectedItem(string listName)
        {
            if (!_lists.TryGetValue(listName, out var info)) return;
            var grid = info.grid;

            if (grid.SelectedRows.Count == 0) return;

            var idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= info.list.Count) return;

            var selectedItem = info.list[idx];

            // Определяем тип элемента и открываем соответствующую форму редактирования
            switch (listName.ToLower())
            {
                case "items":
                    EditItem(selectedItem as ItemData);
                    break;

                case "npcs":
                    EditNPC(selectedItem as NPCData);
                    break;

                case "monsters":
                    EditMonster(selectedItem as MonsterData);
                    break;

                case "locations":
                    EditLocation(selectedItem as LocationData);
                    break;

                case "quests":
                    EditEnhancedQuest(selectedItem as Engine.Quests.EnhancedQuest);
                    break;

                case "dialogues":
                    EditDialogue(selectedItem as DialogueData);
                    break;

                case "rooms":
                    EditRoom(selectedItem as RoomData);
                    break;

                case "chests":
                    EditChest(selectedItem as ChestData);
                    break;

                default:
                    // Для неизвестных типов используем PropertyGrid
                    _propertyGrid.SelectedObject = selectedItem;
                    _propertyGrid.Focus();
                    break;
            }
        }

        // Методы для открытия соответствующих форм редактирования
        private void EditItem(ItemData item)
        {
            if (item == null) return;

            using (var form = new EditItemForm(item))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // Обновляем данные
                    var editedItem = form.EditedItemData;
                    // Копируем изменения в исходный объект
                    CopyProperties(editedItem, item);
                    RefreshCurrentGrid();
                }
            }
        }

        private void EditNPC(NPCData npc)
        {
            if (npc == null) return;

            using (var form = new EditNPCForm(_gameData, npc))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedNPC = form.GetNPCData();
                    CopyProperties(editedNPC, npc);
                    RefreshCurrentGrid();
                }
            }
        }

        private void EditMonster(MonsterData monster)
        {
            if (monster == null) return;

            using (var form = new EditMonsterForm(monster, _gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // Изменения применяются напрямую к объекту monster через форму
                    RefreshCurrentGrid();
                }
            }
        }

        private void EditLocation(LocationData location)
        {
            if (location == null) return;

            using (var form = new EditLocationForm(_gameData, location))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedLocation = form.GetLocation();
                    CopyProperties(editedLocation, location);
                    RefreshCurrentGrid();
                }
            }
        }

        private void EditRoom(RoomData room)
        {
            if (room == null) return;

            using (var form = new EditRoomForm(room, _gameData, false))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshCurrentGrid();
                    _statusLabel.Text = $"Обновлено помещение: {room.Name}";
                }
            }
        }

        private void EditEnhancedQuest(Engine.Quests.EnhancedQuest quest)
        {
            if (quest == null) return;

            using (var form = new EditEnhancedQuestForm(_gameData, quest))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedQuest = form.GetQuest();

                    // Копируем свойства из возвращенного квеста в исходный
                    CopyProperties(editedQuest, quest);

                    // Обновляем только отображение
                    if (_lists.TryGetValue("Quests", out var questsInfo))
                    {
                        questsInfo.grid.Refresh();
                    }

                    _statusLabel.Text = "Квест обновлен";
                }
            }
        }
        private void AddEnhancedQuest()
        {
            var newQuest = new Engine.Quests.EnhancedQuest();

            using (var form = new EditEnhancedQuestForm(_gameData, newQuest))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var quest = form.GetQuest();
                    _gameData.Quests.Add(quest);

                    // Полностью перезагружаем DataSource
                    if (_lists.TryGetValue("Quests", out var questsInfo))
                    {
                        questsInfo.grid.DataSource = null;
                        questsInfo.grid.DataSource = _gameData.Quests;
                        questsInfo.grid.Refresh();
                    }

                    _statusLabel.Text = "Добавлен новый квест";
                }
            }
        }

        private void AddNewDialogue()
        {
            // Генерируем новый ID для диалога
            var nextId = GetNextDialogueId();
            
            var newDialogue = new DialogueData
            {
                Id = nextId.ToString(),
                Name = $"Новый диалог {nextId}",
                Start = "greeting",
                Nodes = new List<DialogueNodeData>()
            };

            // Конвертируем в DialogueDocument для редактирования
            var document = ConvertDialogueDataToDocument(newDialogue);
            
            using (var form = new NewDialogueEditorForm(_gameData, document))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedDocument = form.GetDocument();
                    // Конвертируем обратно в DialogueData
                    ConvertDocumentToDialogueData(editedDocument, newDialogue);
                    
                    _gameData.Dialogues.Add(newDialogue);

                    // Полностью перезагружаем DataSource
                    if (_lists.TryGetValue("Dialogues", out var dialoguesInfo))
                    {
                        dialoguesInfo.grid.DataSource = null;
                        dialoguesInfo.grid.DataSource = _gameData.Dialogues;
                        dialoguesInfo.grid.Refresh();
                    }

                    _statusLabel.Text = "Добавлен новый диалог";
                }
            }
        }

        private int GetNextDialogueId()
        {
            int maxId = 0;
            
            foreach (var dialogue in _gameData.Dialogues)
            {
                if (int.TryParse(dialogue.Id, out int id) && id > maxId)
                {
                    maxId = id;
                }
            }
            
            return maxId + 1;
        }
        private void EditDialogue(DialogueData dialogue)
        {
            if (dialogue == null) return;

            // Конвертируем DialogueData в DialogueDocument для редактирования
            var document = ConvertDialogueDataToDocument(dialogue);
            
            using (var form = new NewDialogueEditorForm(_gameData, document))
            {
                // Подписываемся на событие применения изменений
                form.ChangesApplied += (sender, e) =>
                {
                    var editedDocument = form.GetDocument();
                    // Конвертируем обратно в DialogueData
                    ConvertDocumentToDialogueData(editedDocument, dialogue);
                    RefreshCurrentGrid();
                };

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedDocument = form.GetDocument();
                    // Конвертируем обратно в DialogueData
                    ConvertDocumentToDialogueData(editedDocument, dialogue);
                    RefreshCurrentGrid();
                }
            }
        }

        // Методы конвертации между старой и новой системами диалогов
        private Engine.Dialogue.DialogueDocument ConvertDialogueDataToDocument(DialogueData dialogueData)
        {
            var document = new Engine.Dialogue.DialogueDocument
            {
                Id = dialogueData.Id,
                Name = dialogueData.Name ?? $"Диалог {dialogueData.Id}",
                Start = dialogueData.Start ?? "greeting",
                Nodes = new List<Engine.Dialogue.DialogueNode>()
            };

            if (dialogueData.Nodes != null)
            {
                foreach (var nodeData in dialogueData.Nodes)
                {
                    var node = new Engine.Dialogue.DialogueNode
                    {
                        Id = nodeData.Id,
                        Text = nodeData.Text,
                        Type = nodeData.Type ?? "default",
                        Responses = new List<Response>()
                    };

                    if (nodeData.Choices != null)
                    {
                        foreach (var choice in nodeData.Choices)
                        {
                            var response = new Response
                            {
                                Text = choice.Text,
                                Target = choice.NextNodeId ?? "",
                                Condition = choice.Condition ?? "",
                                Actions = new List<Engine.Dialogue.DialogueAction>()
                            };

                            // Конвертируем действия
                            if (choice.Actions != null && choice.Actions.Count > 0)
                            {
                                foreach (var actionData in choice.Actions)
                                {
                                    response.Actions.Add(new Engine.Dialogue.DialogueAction
                                    {
                                        Type = GetActionTypeName(actionData.Type),
                                        Param = actionData.Parameter ?? ""
                                    });
                                }
                            }
                            else if (choice.Action != Engine.Data.DialogueAction.None)
                            {
                                response.Actions.Add(new Engine.Dialogue.DialogueAction
                                {
                                    Type = GetActionTypeName(choice.Action),
                                    Param = choice.ActionParameter ?? ""
                                });
                            }

                            node.Responses.Add(response);
                        }
                    }

                    document.Nodes.Add(node);
                }
            }

            return document;
        }

        private void ConvertDocumentToDialogueData(Engine.Dialogue.DialogueDocument document, DialogueData dialogueData)
        {
            dialogueData.Id = document.Id;
            dialogueData.Name = document.Name;
            dialogueData.Start = document.Start;
            dialogueData.Nodes = new List<DialogueNodeData>();

            foreach (var node in document.Nodes)
            {
                var nodeData = new DialogueNodeData
                {
                    Id = node.Id,
                    Text = node.Text,
                    Type = node.Type,
                    Choices = new List<DialogueChoiceData>()
                };

                if (node.Responses != null)
                {
                    foreach (var response in node.Responses)
                    {
                        var choice = new DialogueChoiceData
                        {
                            Text = response.Text,
                            NextNodeId = response.Target,
                            Condition = response.Condition,
                            Actions = new List<DialogueActionData>()
                        };

                        if (response.Actions != null)
                        {
                            foreach (var action in response.Actions)
                            {
                                choice.Actions.Add(new DialogueActionData
                                {
                                    Type = GetDialogueActionType(action.Type),
                                    Parameter = action.Param
                                });
                            }
                        }

                        nodeData.Choices.Add(choice);
                    }
                }

                dialogueData.Nodes.Add(nodeData);
            }
        }

        private string GetActionTypeName(Engine.Data.DialogueAction actionType)
        {
            return actionType switch
            {
                Engine.Data.DialogueAction.StartQuest => "StartQuest",
                Engine.Data.DialogueAction.CompleteQuest => "CompleteQuest",
                Engine.Data.DialogueAction.StartTrade => "StartTrade",
                Engine.Data.DialogueAction.EndDialogue => "EndDialogue",
                Engine.Data.DialogueAction.GiveGold => "GiveGold",
                Engine.Data.DialogueAction.GiveItem => "GiveItem",
                Engine.Data.DialogueAction.SetFlag => "SetFlag",
                _ => "None"
            };
        }

        private Engine.Data.DialogueAction GetDialogueActionType(string actionType)
        {
            return actionType switch
            {
                "StartQuest" => Engine.Data.DialogueAction.StartQuest,
                "CompleteQuest" => Engine.Data.DialogueAction.CompleteQuest,
                "StartTrade" => Engine.Data.DialogueAction.StartTrade,
                "EndDialogue" => Engine.Data.DialogueAction.EndDialogue,
                "GiveGold" => Engine.Data.DialogueAction.GiveGold,
                "GiveItem" => Engine.Data.DialogueAction.GiveItem,
                "SetFlag" => Engine.Data.DialogueAction.SetFlag,
                _ => Engine.Data.DialogueAction.None
            };
        }
        private void ValidateRuntimeSync()
        {
            if (_gameData == null)
            {
                MessageBox.Show("Сначала загрузите файл данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Проверяем синхронизацию диалогов и квестов
                var dialogueQuestResult = RuntimeSyncValidator.ValidateDialogueQuestSync(_gameData);
                
                // Проверяем торговлю NPC
                var tradingResult = RuntimeSyncValidator.ValidateNPCTrading(_gameData);

                // Объединяем результаты
                var combinedResult = new ValidationResult();
                combinedResult.Errors.AddRange(dialogueQuestResult.Errors);
                combinedResult.Errors.AddRange(tradingResult.Errors);
                combinedResult.Warnings.AddRange(dialogueQuestResult.Warnings);
                combinedResult.Warnings.AddRange(tradingResult.Warnings);
                combinedResult.Infos.AddRange(dialogueQuestResult.Infos);
                combinedResult.Infos.AddRange(tradingResult.Infos);

                RuntimeSyncValidator.ShowValidationResults(combinedResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при валидации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CopyProperties(object source, object target)
        {
            if (source == null || target == null) return;

            var properties = source.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var value = property.GetValue(source);
                    property.SetValue(target, value);
                }
            }
        }

        private void RefreshCurrentGrid()
        {
            if (_tabs.SelectedTab == null) return;

            var tabName = _tabs.SelectedTab.Text;
            if (!_lists.TryGetValue(tabName, out var info)) return;

            var grid = info.grid;
            
            // Специальная обработка для помещений - обновляем отображение корневых помещений
            if (tabName == "Rooms")
            {
                var rootRooms = GetRootRooms();
                grid.DataSource = null;
                grid.DataSource = rootRooms;
            }
            
            grid.Refresh();
            _statusLabel.Text = "Изменения применены";
        }

        // Методы для работы с помещениями
        private void AddNewRoom()
        {
            var nextId = GetNextRoomId();
            var newRoom = new RoomData
            {
                ID = nextId,
                Name = $"Новое помещение {nextId}",
                Description = "Описание нового помещения",
                ParentLocationID = 0,
                NPCsHere = new List<int>(),
                GroundItems = new List<InventoryItemData>(),
                MonsterSpawns = new List<MonsterSpawnData>(),
                MonsterTemplates = new List<int>(),
                Entrances = new List<int>(),
                ScaleMonstersToPlayerLevel = true
            };

            using (var form = new EditRoomForm(newRoom, _gameData, true))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Rooms.Add(newRoom);
                    RefreshCurrentGrid();
                    _statusLabel.Text = $"Добавлено новое помещение: {newRoom.Name}";
                }
            }
        }

        private void EditSelectedRoom()
        {
            if (!_lists.TryGetValue("Rooms", out var info)) return;
            var grid = info.grid;
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите помещение для редактирования.");
                return;
            }

            var idx = grid.SelectedRows[0].Index;
            var rootRooms = GetRootRooms();
            if (idx < 0 || idx >= rootRooms.Count) return;

            var room = rootRooms[idx];
            if (room == null) return;

            using (var form = new EditRoomForm(room, _gameData, false))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshCurrentGrid();
                    _statusLabel.Text = $"Обновлено помещение: {room.Name}";
                }
            }
        }

        // Методы для работы с входами в помещения
        private void AddNewRoomEntrance()
        {
            var nextId = GetNextRoomEntranceId();
            var newEntrance = new RoomEntranceData
            {
                ID = nextId,
                Name = $"Новый вход {nextId}",
                Description = "Описание нового входа",
                TargetRoomID = 0,
                ParentLocationID = 0,
                EntranceType = "entrance",
                IsLocked = false,
                LockDescription = "",
                RequiresKey = false,
                RequiredKeyID = 0,
                RequiredItemIDs = new List<int>()
            };

            using (var form = new EditRoomEntranceForm(newEntrance, _gameData, true))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.RoomEntrances.Add(newEntrance);
                    RefreshCurrentGrid();
                    _statusLabel.Text = $"Добавлен новый вход: {newEntrance.Name}";
                }
            }
        }

        private void EditSelectedRoomEntrance()
        {
            if (!_lists.TryGetValue("RoomEntrances", out var info)) return;
            var grid = info.grid;
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите вход для редактирования.");
                return;
            }

            var idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= info.list.Count) return;

            var entrance = info.list[idx] as RoomEntranceData;
            if (entrance == null) return;

            using (var form = new EditRoomEntranceForm(entrance, _gameData, false))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshCurrentGrid();
                    _statusLabel.Text = $"Обновлен вход: {entrance.Name}";
                }
            }
        }

        // Вспомогательные методы для генерации ID
        private int GetNextRoomId()
        {
            if (_gameData.Rooms.Count == 0) return 5001;
            return _gameData.Rooms.Max(r => r.ID) + 1;
        }

        private int GetNextRoomEntranceId()
        {
            if (_gameData.RoomEntrances.Count == 0) return 6001;
            return _gameData.RoomEntrances.Max(r => r.ID) + 1;
        }

        private void EditChest(ChestData chest)
        {
            if (chest == null) return;

            using (var form = new EditChestForm(_gameData, chest))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedChest = form.GetChestData();
                    CopyProperties(editedChest, chest);
                    RefreshCurrentGrid();
                    _statusLabel.Text = $"Обновлен сундук: {chest.Name}";
                }
            }
        }

        private int GetNextChestId()
        {
            if (_gameData.Chests.Count == 0) return 7001;
            return _gameData.Chests.Max(c => c.ID) + 1;
        }

        private void AddNewChest()
        {
            var newChest = new ChestData
            {
                ID = GetNextChestId(),
                Name = "Новый сундук",
                NamePlural = "Новые сундуки",
                Description = "Описание сундука",
                Price = 0,
                IsLocked = false,
                IsTrapped = false,
                RequiresKey = false,
                RequiredKeyID = 0,
                RequiredItemIDs = new List<int>(),
                LockDescription = "Сундук заперт.",
                MaxCapacity = 20,
                InitialContents = new List<InventoryItemData>()
            };

            using (var form = new EditChestForm(_gameData, newChest))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var editedChest = form.GetChestData();
                    _gameData.Chests.Add(editedChest);

                    // Обновляем грид
                    if (_lists.TryGetValue("Chests", out var chestsInfo))
                    {
                        chestsInfo.grid.DataSource = null;
                        chestsInfo.grid.DataSource = _gameData.Chests;
                        chestsInfo.grid.Refresh();
                    }

                    _statusLabel.Text = $"Добавлен новый сундук: {editedChest.Name}";
                }
            }
        }

        /// <summary>
        /// Получает список корневых помещений (тех, на которые есть входы)
        /// </summary>
        private List<RoomData> GetRootRooms()
        {
            if (_gameData?.RoomEntrances == null || _gameData?.Rooms == null)
                return new List<RoomData>();

            // Получаем ID всех помещений, на которые есть входы
            var rootRoomIds = _gameData.RoomEntrances.Select(e => e.TargetRoomID).ToHashSet();
            
            // Возвращаем только те помещения, на которые есть входы
            return _gameData.Rooms.Where(r => rootRoomIds.Contains(r.ID)).ToList();
        }
    }
}
