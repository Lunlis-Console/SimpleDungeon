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

            ScreenManager.SetNeedsRedraw();
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            // Очищаем сообщения если их слишком много
            if (MessageSystem.messages.Count > 5)
            {
                MessageSystem.ClearMessages();
            }

            RenderMessages();
            RenderLocationInfo();
            RenderCreatures();
            RenderGroundItems();
            RenderNavigation();

            _renderer.EndFrame();
        }

        private void RenderMessages()
        {
            int y = 0;
            foreach (var message in MessageSystem.messages)
            {
                _renderer.Write(2, y, $"• {message}");
                y++;
            }
        }

        private void RenderLocationInfo()
        {
            int y = MessageSystem.messages.Count + 2;
            RenderHeader(_currentLocation.Name, y);

            y += 3;
            var descriptionLines = WrapText(_currentLocation.Description, Console.WindowWidth - 4);
            foreach (var line in descriptionLines)
            {
                _renderer.Write(2, y, line);
                y++;
            }
        }

        private void RenderCreatures()
        {
            int y = MessageSystem.messages.Count + 6;

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
            RenderFooter("WASD - движение │ I - инвентарь │ C - персонаж │ J - квесты │ E - взаимодействие │ ESC - меню", -2);

            string directions = "Доступно: ";
            if (_currentLocation.LocationToNorth != null) directions += "Север(W) ";
            if (_currentLocation.LocationToSouth != null) directions += "Юг(S) ";
            if (_currentLocation.LocationToEast != null) directions += "Восток(D) ";
            if (_currentLocation.LocationToWest != null) directions += "Запад(A) ";

            _renderer.Write(2, Console.WindowHeight - 4, directions);
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                    MoveNorth();
                    ScreenManager.SetNeedsRedraw(); // Явно запрашиваем перерисовку
                    break;
                case ConsoleKey.S:
                    MoveSouth();
                    ScreenManager.SetNeedsRedraw();
                    break;
                case ConsoleKey.A:
                    MoveWest();
                    ScreenManager.SetNeedsRedraw();
                    break;
                case ConsoleKey.D:
                    MoveEast();
                    ScreenManager.SetNeedsRedraw();
                    break;
                case ConsoleKey.I:
                    ScreenManager.PushScreen(new InventoryScreen(_player));
                    break;
                case ConsoleKey.C:
                    ScreenManager.PushScreen(new CharacterScreen(_player));
                    break;
                case ConsoleKey.J:
                    ScreenManager.PushScreen(new QuestLogScreen(_player));
                    break;
                case ConsoleKey.E:
                    ScreenManager.PushScreen(new InteractionScreen(_player, _currentLocation));
                    break;
                case ConsoleKey.Escape:
                    ScreenManager.PushScreen(new GameMenuScreen(_player));
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
            ScreenManager.SetNeedsRedraw();
        }
    }
}