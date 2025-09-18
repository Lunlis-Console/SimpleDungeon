using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    /// <summary>
    /// Графический редактор для создания связей между под-помещениями
    /// </summary>
    public partial class RoomConnectionEditor : Form
    {
        private List<RoomData> _rooms;
        private List<RoomData> _currentLocationRooms;
        private RoomData _currentRoom;
        private DoubleBufferedPanel _canvas;
        private Dictionary<int, RoomNode> _roomNodes;
        private RoomNode _selectedNode;
        private RoomNode _draggedNode;
        private Point _dragOffset;
        private bool _isDragging;
        private bool _isConnecting;
        private RoomNode _connectionStart;
        private Dictionary<string, ConnectionLine> _connections;

        private const int NODE_WIDTH = 120;
        private const int NODE_HEIGHT = 80;
        private const int GRID_SIZE = 20;

        public RoomConnectionEditor(List<RoomData> rooms, RoomData currentRoom)
        {
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (currentRoom == null) throw new ArgumentNullException(nameof(currentRoom));
            
            _rooms = rooms;
            _currentRoom = currentRoom;
            _currentLocationRooms = rooms.Where(r => r.ParentLocationID == currentRoom.ParentLocationID).ToList();
            _roomNodes = new Dictionary<int, RoomNode>();
            _connections = new Dictionary<string, ConnectionLine>();
            
            InitializeComponent();
            LoadRoomData();
        }

        private void InitializeComponent()
        {
            this.Text = $"Редактор связей помещений - {_currentRoom.Name}";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // Создаем панель инструментов
            var toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            
            var btnSave = new ToolStripButton("Сохранить");
            btnSave.Click += BtnSave_Click;
            toolStrip.Items.Add(btnSave);
            
            var btnCancel = new ToolStripButton("Отмена");
            btnCancel.Click += BtnCancel_Click;
            toolStrip.Items.Add(btnCancel);
            
            var separator = new ToolStripSeparator();
            toolStrip.Items.Add(separator);
            
            var btnAddRoom = new ToolStripButton("Добавить помещение");
            btnAddRoom.Click += BtnAddRoom_Click;
            toolStrip.Items.Add(btnAddRoom);
            
            var btnDeleteRoom = new ToolStripButton("Удалить помещение");
            btnDeleteRoom.Click += BtnDeleteRoom_Click;
            toolStrip.Items.Add(btnDeleteRoom);
            
            this.Controls.Add(toolStrip);

            // Создаем холст для рисования
            _canvas = new DoubleBufferedPanel();
            _canvas.Dock = DockStyle.Fill;
            _canvas.BackColor = Color.White;
            _canvas.Paint += Canvas_Paint;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            
            this.Controls.Add(_canvas);
        }


        private void LoadRoomData()
        {
            _roomNodes.Clear();
            _connections.Clear();

            // Создаем узлы для всех помещений текущей локации
            int x = 50;
            int y = 50;
            int nodesPerRow = 4;
            int currentRow = 0;

            foreach (var room in _currentLocationRooms)
            {
                var node = new RoomNode
                {
                    RoomData = room,
                    Position = new Point(x, y),
                    Size = new Size(NODE_WIDTH, NODE_HEIGHT),
                    IsCurrentRoom = room.ID == _currentRoom.ID
                };

                _roomNodes[room.ID] = node;

                // Переходим к следующей позиции
                currentRow++;
                if (currentRow >= nodesPerRow)
                {
                    currentRow = 0;
                    x = 50;
                    y += NODE_HEIGHT + 50;
                }
                else
                {
                    x += NODE_WIDTH + 50;
                }
            }

            // Создаем связи
            CreateConnections();
        }

        private void CreateConnections()
        {
            foreach (var room in _currentLocationRooms)
            {
                var fromNode = _roomNodes[room.ID];

                // Север
                if (room.RoomToNorth.HasValue && _roomNodes.ContainsKey(room.RoomToNorth.Value))
                {
                    var toNode = _roomNodes[room.RoomToNorth.Value];
                    var connectionKey = $"{room.ID}_north_{room.RoomToNorth.Value}";
                    _connections[connectionKey] = new ConnectionLine
                    {
                        From = fromNode,
                        To = toNode,
                        Direction = "north",
                        FromRoomID = room.ID,
                        ToRoomID = room.RoomToNorth.Value
                    };
                }

                // Восток
                if (room.RoomToEast.HasValue && _roomNodes.ContainsKey(room.RoomToEast.Value))
                {
                    var toNode = _roomNodes[room.RoomToEast.Value];
                    var connectionKey = $"{room.ID}_east_{room.RoomToEast.Value}";
                    _connections[connectionKey] = new ConnectionLine
                    {
                        From = fromNode,
                        To = toNode,
                        Direction = "east",
                        FromRoomID = room.ID,
                        ToRoomID = room.RoomToEast.Value
                    };
                }

                // Юг
                if (room.RoomToSouth.HasValue && _roomNodes.ContainsKey(room.RoomToSouth.Value))
                {
                    var toNode = _roomNodes[room.RoomToSouth.Value];
                    var connectionKey = $"{room.ID}_south_{room.RoomToSouth.Value}";
                    _connections[connectionKey] = new ConnectionLine
                    {
                        From = fromNode,
                        To = toNode,
                        Direction = "south",
                        FromRoomID = room.ID,
                        ToRoomID = room.RoomToSouth.Value
                    };
                }

                // Запад
                if (room.RoomToWest.HasValue && _roomNodes.ContainsKey(room.RoomToWest.Value))
                {
                    var toNode = _roomNodes[room.RoomToWest.Value];
                    var connectionKey = $"{room.ID}_west_{room.RoomToWest.Value}";
                    _connections[connectionKey] = new ConnectionLine
                    {
                        From = fromNode,
                        To = toNode,
                        Direction = "west",
                        FromRoomID = room.ID,
                        ToRoomID = room.RoomToWest.Value
                    };
                }
            }
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Рисуем сетку
            DrawGrid(g);

            // Рисуем связи
            foreach (var connection in _connections.Values)
            {
                DrawConnection(g, connection);
            }

            // Рисуем временную линию при создании связи
            if (_isConnecting && _connectionStart != null)
            {
                DrawTemporaryConnection(g, _connectionStart.Position);
            }

            // Рисуем узлы
            foreach (var node in _roomNodes.Values)
            {
                DrawRoomNode(g, node);
            }
        }

        private void DrawGrid(Graphics g)
        {
            var pen = new Pen(Color.LightGray, 1);
            
            for (int x = 0; x < _canvas.Width; x += GRID_SIZE)
            {
                g.DrawLine(pen, x, 0, x, _canvas.Height);
            }
            
            for (int y = 0; y < _canvas.Height; y += GRID_SIZE)
            {
                g.DrawLine(pen, 0, y, _canvas.Width, y);
            }
            
            pen.Dispose();
        }

        private void DrawTemporaryConnection(Graphics g, Point fromPosition)
        {
            var mousePos = _canvas.PointToClient(Cursor.Position);
            var fromCenter = new Point(
                fromPosition.X + NODE_WIDTH / 2,
                fromPosition.Y + NODE_HEIGHT / 2
            );

            using (var pen = new Pen(Color.Gray, 2))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawLine(pen, fromCenter, mousePos);
            }
        }

        private void DrawRoomNode(Graphics g, RoomNode node)
        {
            var rect = new Rectangle(node.Position, node.Size);
            
            // Цвет узла
            Color nodeColor = node.IsCurrentRoom ? Color.LightBlue : Color.LightGreen;
            if (node == _selectedNode)
                nodeColor = Color.Yellow;
            
            // Рисуем фон узла
            using (var brush = new SolidBrush(nodeColor))
            {
                g.FillRectangle(brush, rect);
            }
            
            // Рисуем рамку
            using (var pen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(pen, rect);
            }
            
            // Рисуем текст
            using (var font = new Font("Arial", 8))
            using (var brush = new SolidBrush(Color.Black))
            {
                var textRect = new Rectangle(node.Position.X + 5, node.Position.Y + 5, 
                                           node.Size.Width - 10, node.Size.Height - 10);
                g.DrawString($"{node.RoomData.Name}\nID: {node.RoomData.ID}", 
                           font, brush, textRect);
            }
        }

        private void DrawConnection(Graphics g, ConnectionLine connection)
        {
            var fromCenter = new Point(
                connection.From.Position.X + connection.From.Size.Width / 2,
                connection.From.Position.Y + connection.From.Size.Height / 2
            );
            
            var toCenter = new Point(
                connection.To.Position.X + connection.To.Size.Width / 2,
                connection.To.Position.Y + connection.To.Size.Height / 2
            );

            // Цвет связи в зависимости от направления
            Color connectionColor = connection.Direction switch
            {
                "north" => Color.Red,
                "east" => Color.Green,
                "south" => Color.Blue,
                "west" => Color.Orange,
                _ => Color.Black
            };

            using (var pen = new Pen(connectionColor, 3))
            {
                g.DrawLine(pen, fromCenter, toCenter);
            }

            // Рисуем стрелку
            DrawArrow(g, fromCenter, toCenter, connectionColor);
        }

        private void DrawArrow(Graphics g, Point from, Point to, Color color)
        {
            // Простая стрелка
            var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            var arrowLength = 10;
            var arrowAngle = Math.PI / 6;

            var arrow1 = new Point(
                (int)(to.X - arrowLength * Math.Cos(angle - arrowAngle)),
                (int)(to.Y - arrowLength * Math.Sin(angle - arrowAngle))
            );

            var arrow2 = new Point(
                (int)(to.X - arrowLength * Math.Cos(angle + arrowAngle)),
                (int)(to.Y - arrowLength * Math.Sin(angle + arrowAngle))
            );

            using (var pen = new Pen(color, 2))
            {
                g.DrawLine(pen, to, arrow1);
                g.DrawLine(pen, to, arrow2);
            }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            var clickedNode = GetNodeAtPosition(e.Location);
            
            if (e.Button == MouseButtons.Left)
            {
                if (clickedNode != null)
                {
                    _selectedNode = clickedNode;
                    _draggedNode = clickedNode;
                    _dragOffset = new Point(e.X - clickedNode.Position.X, e.Y - clickedNode.Position.Y);
                    _isDragging = true;
                }
                else
                {
                    _selectedNode = null;
                }
            }
            else if (e.Button == MouseButtons.Right && clickedNode != null)
            {
                ShowContextMenu(clickedNode, e.Location);
            }

            _canvas.Invalidate();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedNode != null)
            {
                // Привязываем к сетке
                var newX = ((e.X - _dragOffset.X) / GRID_SIZE) * GRID_SIZE;
                var newY = ((e.Y - _dragOffset.Y) / GRID_SIZE) * GRID_SIZE;
                
                _draggedNode.Position = new Point(newX, newY);
                _canvas.Invalidate();
            }
            else if (_isConnecting)
            {
                // Обновляем холст для отображения временной линии
                _canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedNode = null;
            }
            else if (_isConnecting && e.Button == MouseButtons.Left)
            {
                var clickedNode = GetNodeAtPosition(e.Location);
                if (clickedNode != null && clickedNode != _connectionStart)
                {
                    // Создаем связь
                    CreateConnection(_connectionStart, clickedNode);
                }
                
                _isConnecting = false;
                _connectionStart = null;
                this.Cursor = Cursors.Default;
            }
        }

        private void Canvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var clickedNode = GetNodeAtPosition(e.Location);
            if (clickedNode != null && clickedNode.RoomData != null)
            {
                try
                {
                    // Создаем временный GameData для EditSubRoomForm
                    var tempGameData = new GameData
                    {
                        Rooms = _rooms,
                        Items = new List<ItemData>(),
                        Monsters = new List<MonsterData>(),
                        Locations = new List<LocationData>(),
                        NPCs = new List<NPCData>(),
                        Quests = new List<Engine.Quests.EnhancedQuest>(),
                        Dialogues = new List<DialogueData>(),
                        RoomEntrances = new List<RoomEntranceData>(),
                        Titles = new List<TitleData>()
                    };

                    // Открываем редактор помещения
                    var editForm = new EditSubRoomForm(_currentRoom, clickedNode.RoomData, tempGameData, false);
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        // Обновляем данные
                        var index = _rooms.FindIndex(r => r.ID == clickedNode.RoomData.ID);
                        if (index >= 0)
                        {
                            _rooms[index] = editForm.GetSubRoom();
                            clickedNode.RoomData = editForm.GetSubRoom();
                            _canvas.Invalidate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии редактора помещения: {ex.Message}", 
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private RoomNode GetNodeAtPosition(Point position)
        {
            foreach (var node in _roomNodes.Values)
            {
                var rect = new Rectangle(node.Position, node.Size);
                if (rect.Contains(position))
                    return node;
            }
            return null;
        }

        private void CreateConnection(RoomNode fromNode, RoomNode toNode)
        {
            // Определяем направление связи
            var dx = toNode.Position.X - fromNode.Position.X;
            var dy = toNode.Position.Y - fromNode.Position.Y;
            
            string direction;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                direction = dx > 0 ? "east" : "west";
            }
            else
            {
                direction = dy > 0 ? "south" : "north";
            }
            
            // Проверяем, не существует ли уже такая связь
            var connectionKey = $"{fromNode.RoomData.ID}_{direction}_{toNode.RoomData.ID}";
            if (_connections.ContainsKey(connectionKey))
            {
                MessageBox.Show("Связь уже существует!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Создаем новую связь
            _connections[connectionKey] = new ConnectionLine
            {
                From = fromNode,
                To = toNode,
                Direction = direction,
                FromRoomID = fromNode.RoomData.ID,
                ToRoomID = toNode.RoomData.ID
            };
            
            _canvas.Invalidate();
        }

        private void ShowContextMenu(RoomNode node, Point position)
        {
            var contextMenu = new ContextMenuStrip();
            
            var connectItem = new ToolStripMenuItem("Создать связь");
            connectItem.Click += (s, e) => StartConnection(node);
            contextMenu.Items.Add(connectItem);
            
            var disconnectItem = new ToolStripMenuItem("Удалить связи");
            disconnectItem.Click += (s, e) => RemoveConnections(node);
            contextMenu.Items.Add(disconnectItem);
            
            contextMenu.Show(_canvas, position);
        }

        private void StartConnection(RoomNode fromNode)
        {
            _isConnecting = true;
            _connectionStart = fromNode;
            this.Cursor = Cursors.Cross;
        }

        private void RemoveConnections(RoomNode node)
        {
            var connectionsToRemove = _connections.Where(c => 
                c.Value.FromRoomID == node.RoomData.ID || c.Value.ToRoomID == node.RoomData.ID).ToList();
            
            foreach (var connection in connectionsToRemove)
            {
                _connections.Remove(connection.Key);
            }
            
            _canvas.Invalidate();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Сохраняем связи в данные помещений
            SaveConnections();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BtnAddRoom_Click(object sender, EventArgs e)
        {
            var newRoom = new RoomData
            {
                ID = GetNextRoomId(),
                Name = "Новое помещение",
                Description = "Описание нового помещения",
                ParentLocationID = _currentRoom.ParentLocationID,
                MonsterTemplates = new List<int>(),
                GroundItems = new List<InventoryItemData>(),
                NPCsHere = new List<int>(),
                MonsterSpawns = new List<MonsterSpawnData>(),
                Entrances = new List<int>(),
                ScaleMonstersToPlayerLevel = true
            };

            _rooms.Add(newRoom);
            _currentLocationRooms.Add(newRoom);

            // Создаем новый узел
            var node = new RoomNode
            {
                RoomData = newRoom,
                Position = new Point(50, 50),
                Size = new Size(NODE_WIDTH, NODE_HEIGHT),
                IsCurrentRoom = false
            };

            _roomNodes[newRoom.ID] = node;
            _canvas.Invalidate();
        }

        private void BtnDeleteRoom_Click(object sender, EventArgs e)
        {
            if (_selectedNode != null && !_selectedNode.IsCurrentRoom)
            {
                var result = MessageBox.Show($"Удалить помещение '{_selectedNode.RoomData.Name}'?", 
                                           "Подтверждение", MessageBoxButtons.YesNo);
                
                if (result == DialogResult.Yes)
                {
                    // Удаляем связи
                    RemoveConnections(_selectedNode);
                    
                    // Удаляем помещение
                    _rooms.RemoveAll(r => r.ID == _selectedNode.RoomData.ID);
                    _currentLocationRooms.RemoveAll(r => r.ID == _selectedNode.RoomData.ID);
                    _roomNodes.Remove(_selectedNode.RoomData.ID);
                    
                    _selectedNode = null;
                    _canvas.Invalidate();
                }
            }
        }

        private void SaveConnections()
        {
            // Очищаем все связи
            foreach (var room in _currentLocationRooms)
            {
                room.RoomToNorth = null;
                room.RoomToEast = null;
                room.RoomToSouth = null;
                room.RoomToWest = null;
            }

            // Устанавливаем новые связи
            foreach (var connection in _connections.Values)
            {
                var fromRoom = _currentLocationRooms.FirstOrDefault(r => r.ID == connection.FromRoomID);
                if (fromRoom != null)
                {
                    switch (connection.Direction)
                    {
                        case "north":
                            fromRoom.RoomToNorth = connection.ToRoomID;
                            break;
                        case "east":
                            fromRoom.RoomToEast = connection.ToRoomID;
                            break;
                        case "south":
                            fromRoom.RoomToSouth = connection.ToRoomID;
                            break;
                        case "west":
                            fromRoom.RoomToWest = connection.ToRoomID;
                            break;
                    }
                }
            }
        }

        private int GetNextRoomId()
        {
            return _rooms.Count > 0 ? _rooms.Max(r => r.ID) + 1 : 5001;
        }
    }

    public class RoomNode
    {
        public RoomData RoomData { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
        public bool IsCurrentRoom { get; set; }
    }

    public class ConnectionLine
    {
        public RoomNode From { get; set; }
        public RoomNode To { get; set; }
        public string Direction { get; set; }
        public int FromRoomID { get; set; }
        public int ToRoomID { get; set; }
    }

    /// <summary>
    /// Кастомный Panel с поддержкой двойной буферизации для улучшения производительности рисования
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            // Включаем двойную буферизацию для устранения мерцания
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }
    }
}
