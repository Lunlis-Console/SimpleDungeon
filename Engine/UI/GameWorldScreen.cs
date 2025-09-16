using Engine.Core;
using Engine.Entities;
using Engine.World;

namespace Engine.UI
{
    public class GameWorldScreen : BaseScreen
    {
        private readonly Player _player;
        private Location _currentLocation;
        private Room _currentRoom; // Текущее помещение (null если в основной локации)

        public GameWorldScreen(Player player)
        {
            _player = player;
            _currentLocation = player.CurrentLocation;
            _currentRoom = player.CurrentRoom;

            // Небольшая задержка для инициализации
            Thread.Sleep(50);
            ScreenManager.RequestPartialRedraw();
        }

        public override void Render()
        {
            ClearScreen();
            RenderAreaInfo();
            RenderCreatures();
            RenderGroundItems();
            RenderNavigation();
        }

        private void RenderAreaInfo()
        {
            int y = 0;

            if (_currentRoom != null)
            {
                // Рендерим информацию о помещении
                RenderHeader($"{_currentRoom.Name} (в {_currentLocation.Name})", y);
                y += 3;

                var descriptionLines = WrapText(_currentRoom.Description, Console.WindowWidth - 4);
                foreach (var line in descriptionLines)
                {
                    if (y < Height - 9) // Не заходим на область сообщений
                    {
                        _renderer.Write(2, y, line);
                        y++;
                    }
                }
            }
            else
            {
                // Рендерим информацию о локации
                RenderHeader(_currentLocation.Name, y);
                y += 3;

                var descriptionLines = WrapText(_currentLocation.Description, Console.WindowWidth - 4);
                foreach (var line in descriptionLines)
                {
                    if (y < Height - 9) // Не заходим на область сообщений
                    {
                        _renderer.Write(2, y, line);
                        y++;
                    }
                }
            }
        }

        private void RenderCreatures()
        {
            int y = 6;

            // Определяем текущую область (локация или помещение)
            var currentArea = _currentRoom ?? (object)_currentLocation;

            // Монстры
            List<Monster> monsters;
            if (_currentRoom != null)
            {
                monsters = _currentRoom.FindMonsters();
            }
            else
            {
                monsters = _currentLocation.FindMonsters();
            }

            if (monsters.Count > 0)
            {
                _renderer.Write(2, y, "Монстры:");
                y++;
                foreach (var monster in monsters)
                {
                    _renderer.Write(4, y, $"• {monster.Name} [Ур. {monster.Level}]");
                    y++;
                }
                y++;
            }

            // NPC
            List<NPC> npcs;
            if (_currentRoom != null)
            {
                npcs = _currentRoom.NPCsHere;
            }
            else
            {
                npcs = _currentLocation.NPCsHere;
            }

            if (npcs.Count > 0)
            {
                _renderer.Write(2, y, "Жители:");
                y++;
                foreach (var npc in npcs)
                {
                    _renderer.Write(4, y, $"• {npc.Name}");
                    y++;
                }
            }
        }

        private void RenderGroundItems()
        {
            int y = Console.WindowHeight - 10;
            
            List<InventoryItem> groundItems;
            if (_currentRoom != null)
            {
                groundItems = _currentRoom.GroundItems;
            }
            else
            {
                groundItems = _currentLocation.GroundItems;
            }

            if (groundItems.Count > 0)
            {
                _renderer.Write(2, y, "Предметы на земле:");
                y++;
                foreach (var item in groundItems)
                {
                    _renderer.Write(4, y, $"• {item.Details.Name} x{item.Quantity}");
                    y++;
                }
            }
        }

        private void RenderNavigation()
        {
            RenderFooter("WASD - движение │ I - инвентарь │ C - персонаж │ J - журнал │ E - взаимодействие │ ESC - меню", 0);

            RenderCompass();
        }

        private void RenderCompass()
        {
            int compassX = Console.WindowWidth - 10;
            int compassY = Console.WindowHeight - 9;

            bool north, south, east, west;

            if (_currentRoom != null)
            {
                // В помещении - показываем навигацию между помещениями
                north = _currentRoom.RoomToNorth != null;
                south = _currentRoom.RoomToSouth != null;
                east = _currentRoom.RoomToEast != null;
                west = _currentRoom.RoomToWest != null;
            }
            else
            {
                // В основной локации - показываем навигацию между локациями
                north = _currentLocation.LocationToNorth != null;
                south = _currentLocation.LocationToSouth != null;
                east = _currentLocation.LocationToEast != null;
                west = _currentLocation.LocationToWest != null;
            }

            ConsoleColor activeColor = ConsoleColor.Yellow;
            ConsoleColor inactiveColor = ConsoleColor.DarkGray;

            // Компас в виде креста
            _renderer.Write(compassX + 3, compassY - 1, "N", north ? activeColor : inactiveColor);
            _renderer.Write(compassX, compassY + 1, "W", west ? activeColor : inactiveColor);
            _renderer.Write(compassX + 6, compassY + 1, "E", east ? activeColor : inactiveColor);
            _renderer.Write(compassX + 3, compassY + 3, "S", south ? activeColor : inactiveColor);

            // Соединительные линии
            _renderer.Write(compassX + 3, compassY + 1, "+", ConsoleColor.DarkGray);
            _renderer.Write(compassX + 1, compassY + 1, "─", ConsoleColor.DarkGray);
            _renderer.Write(compassX + 2, compassY + 1, "─", ConsoleColor.DarkGray);
            _renderer.Write(compassX + 4, compassY + 1, "─", ConsoleColor.DarkGray);
            _renderer.Write(compassX + 5, compassY + 1, "─", ConsoleColor.DarkGray);
            _renderer.Write(compassX + 3, compassY, "│", ConsoleColor.DarkGray);
            _renderer.Write(compassX + 3, compassY + 2, "│", ConsoleColor.DarkGray);

            // Если в помещении, показываем кнопку выхода
            if (_currentRoom != null)
            {
                _renderer.Write(compassX - 15, compassY + 1, "Q - Выйти", ConsoleColor.Cyan);
            }
        }
        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                    if (_currentRoom != null)
                        MoveRoomNorth();
                    else
                        MoveNorth();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.S:
                    if (_currentRoom != null)
                        MoveRoomSouth();
                    else
                        MoveSouth();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.A:
                    if (_currentRoom != null)
                        MoveRoomWest();
                    else
                        MoveWest();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.D:
                    if (_currentRoom != null)
                        MoveRoomEast();
                    else
                        MoveEast();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.Q:
                    if (_currentRoom != null)
                    {
                        ExitRoom();
                        RequestPartialRedraw();
                    }
                    break;

                case ConsoleKey.I: // Инвентарь
                    ScreenManager.PushScreen(new InventoryScreen(_player));
                    RequestFullRedraw();
                    break;

                case ConsoleKey.C: // Персонаж
                    ScreenManager.PushScreen(new CharacterScreen(_player));
                    RequestFullRedraw();
                    break;
                case ConsoleKey.E: // Взаимодействие
                    var currentArea = _currentRoom ?? (object)_currentLocation;
                    if (_currentRoom != null)
                    {
                        // В помещении - создаем InteractionScreen для помещения
                        ScreenManager.PushScreen(new RoomInteractionScreen(_player, _currentRoom));
                    }
                    else
                    {
                        // В основной локации - обычный InteractionScreen
                        ScreenManager.PushScreen(new InteractionScreen(_player, _currentLocation));
                    }
                    RequestFullRedraw();
                    break;
                case ConsoleKey.J: // Журнал заданий
                    _player.QuestLog.DisplayQuestLog();
                    RequestFullRedraw();
                    break;
                case ConsoleKey.F2: // Перезагрузка данных
                    ReloadGameData();
                    break;
                case ConsoleKey.Escape: // Меню
                    ScreenManager.PushScreen(new GameMenuScreen(_player));
                    RequestFullRedraw();
                    break;
            }
        }
        private void MoveNorth() => MoveTo(_currentLocation.LocationToNorth, "север");
        private void MoveSouth() => MoveTo(_currentLocation.LocationToSouth, "юг");
        private void MoveEast() => MoveTo(_currentLocation.LocationToEast, "восток");
        private void MoveWest() => MoveTo(_currentLocation.LocationToWest, "запад");

        // Методы для движения в помещениях
        private void MoveRoomNorth() => MoveToRoom(_currentRoom.RoomToNorth, "север");
        private void MoveRoomSouth() => MoveToRoom(_currentRoom.RoomToSouth, "юг");
        private void MoveRoomEast() => MoveToRoom(_currentRoom.RoomToEast, "восток");
        private void MoveRoomWest() => MoveToRoom(_currentRoom.RoomToWest, "запад");

        private void ExitRoom()
        {
            _player.ExitRoom();
            _currentRoom = null;
            _currentLocation = _player.CurrentLocation;
        }

        private void MoveTo(Location newLocation, string direction)
        {
            if (newLocation == null)
            {
                MessageSystem.AddMessage($"Нельзя двигаться на {direction}.");
                return;
            }

            _player.MoveTo(newLocation);
            _currentLocation = newLocation;
            _currentRoom = null; // Выходим из помещения при перемещении между локациями
            MessageSystem.AddMessage($"Вы переместились в {newLocation.Name}");
            ScreenManager.RequestPartialRedraw();
        }

        private void MoveToRoom(Room newRoom, string direction)
        {
            if (newRoom == null)
            {
                MessageSystem.AddMessage($"Нельзя двигаться на {direction}.");
                return;
            }

            _player.MoveToRoom(newRoom);
            _currentRoom = newRoom;
            MessageSystem.AddMessage($"Вы переместились в {newRoom.Name}");
            ScreenManager.RequestPartialRedraw();
        }

        private void ReloadGameData()
        {
            try
            {
                GameServices.WorldRepository = new JsonWorldRepository("game_data.json");
                DebugConsole.Log("Игровые данные перезагружены");
                RequestFullRedraw();
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Ошибка перезагрузки: {ex.Message}");
            }
        }
    }
}