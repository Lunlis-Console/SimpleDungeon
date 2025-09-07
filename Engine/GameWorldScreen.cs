namespace Engine
{
    public class GameWorldScreen : BaseScreen
    {
        private readonly Player _player;
        private Location _currentLocation;

        public GameWorldScreen(Player player)
        {
            _player = player;
            _currentLocation = player.CurrentLocation;

            // Небольшая задержка для инициализации
            Thread.Sleep(50);
            ScreenManager.RequestPartialRedraw();
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();
        
            RenderLocationInfo();
            RenderCreatures();
            RenderGroundItems();

            RenderMessages();

            RenderNavigation();

            _renderer.EndFrame();
        }

        private void RenderMessages()
        {
            int maxMessages = 3;
            int messageAreaHeight = 3;
            int startY = Height - 3 - messageAreaHeight; // Начинаем над футером

            // Очищаем область сообщений
            _renderer.FillArea(0, startY, Width, messageAreaHeight, ' ', ConsoleColor.White, ConsoleColor.Black);

            // Берем самые новые сообщения (последние добавленные)
            var messagesToShow = MessageSystem.Messages.Take(maxMessages).ToArray();

            // Рендерим снизу вверх - новые сообщения появляются внизу, старые поднимаются
            for (int i = 0; i < messagesToShow.Length; i++)
            {
                var messageData = messagesToShow[i];

                // Новые сообщения внизу, старые поднимаются вверх
                int y = startY + (maxMessages - 1 - i);

                if (y >= startY && y < Height - 3)
                {
                    ConsoleColor color = CalculateMessageColor(messageData.Alpha);

                    string displayText = messageData.Text;
                    if (displayText.Length > Width - 4)
                    {
                        displayText = displayText.Substring(0, Width - 7) + "...";
                    }

                    _renderer.Write(2, y, $"• {displayText}", color);
                }
            }
        }
        private ConsoleColor CalculateMessageColor(float alpha)
        {
            if (alpha > 0.7f) return ConsoleColor.Gray;
            if (alpha > 0.4f) return ConsoleColor.DarkGray;
            return ConsoleColor.DarkGray; // Почти невидимый
        }

        private void RenderLocationInfo()
        {
            int y = 0;

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

        private void RenderCreatures()
        {
            int y = 6;

            // Монстры
            var monsters = _currentLocation.FindMonsters();
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
            if (_currentLocation.NPCsHere.Count > 0)
            {
                _renderer.Write(2, y, "Жители:");
                y++;
                foreach (var npc in _currentLocation.NPCsHere)
                {
                    _renderer.Write(4, y, $"• {npc.Name}");
                    y++;
                }
            }
        }

        private void RenderGroundItems()
        {
            int y = Console.WindowHeight - 10;
            if (_currentLocation.GroundItems.Count > 0)
            {
                _renderer.Write(2, y, "Предметы на земле:");
                y++;
                foreach (var item in _currentLocation.GroundItems)
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

            bool north = _currentLocation.LocationToNorth != null;
            bool south = _currentLocation.LocationToSouth != null;
            bool east = _currentLocation.LocationToEast != null;
            bool west = _currentLocation.LocationToWest != null;

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
        }
        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                    MoveNorth();
                    RequestPartialRedraw(); // Только сообщение изменилось
                    break;

                case ConsoleKey.S:
                    MoveSouth();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.A:
                    MoveWest();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.D:
                    MoveEast();
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.I: // Инвентарь
                    ScreenManager.PushScreen(new InventoryScreen(_player));
                    RequestFullRedraw(); // Полная перерисовка при открытии
                    break;

                case ConsoleKey.C: // Персонаж
                    ScreenManager.PushScreen(new CharacterScreen(_player));
                    RequestFullRedraw();
                    break;
                //case ConsoleKey.E:
                //    ScreenManager.PushScreen(new );

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

        private void MoveTo(Location newLocation, string direction)
        {
            if (newLocation == null)
            {
                MessageSystem.AddMessage($"Нельзя двигаться на {direction}.");
                return;
            }

            _player.MoveTo(newLocation);
            _currentLocation = newLocation;
            MessageSystem.AddMessage($"Вы переместились в {newLocation.Name}");
            ScreenManager.RequestPartialRedraw();
        }
    }
}