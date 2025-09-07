namespace Engine
{
    public class InteractionScreen : BaseScreen
    {
        private readonly Player _player;
        private readonly Location _location;
        private int _selectedIndex;
        private List<WorldEntity> _interactableEntities;

        public InteractionScreen(Player player, Location location)
        {
            _player = player;
            _location = location;
            _selectedIndex = 0;
            _interactableEntities = GetInteractableEntities();
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("ВЗАИМОДЕЙСТВИЕ");
            RenderEntitiesList();
            RenderSelectedEntityInfo();
            RenderFooter("W/S - выбор │ E - взаимодействовать │ Q - назад");

            _renderer.EndFrame();
        }

        private List<WorldEntity> GetInteractableEntities()
        {
            var entities = new List<WorldEntity>();

            // Монстры
            var monsters = _location.FindMonsters();
            foreach (var monster in monsters)
            {
                entities.Add(new WorldEntity(monster, EntityType.Monster, $"{monster.Name} [Ур. {monster.Level}]"));
            }

            // NPC
            foreach (var npc in _location.NPCsHere)
            {
                entities.Add(new WorldEntity(npc, EntityType.NPC, npc.Name));
            }

            // Предметы на земле
            foreach (var item in _location.GroundItems)
            {
                entities.Add(new WorldEntity(item, EntityType.Item, $"{item.Details.Name} x{item.Quantity}"));
            }

            return entities;
        }

        private void RenderEntitiesList()
        {
            int y = 4;

            if (_interactableEntities.Count == 0)
            {
                _renderer.Write(2, y, "Не с чем взаимодействовать.", ConsoleColor.DarkGray);
                return;
            }

            for (int i = 0; i < _interactableEntities.Count; i++)
            {
                var entity = _interactableEntities[i];
                bool isSelected = i == _selectedIndex;

                string prefix = entity.Type switch
                {
                    EntityType.Monster => "",
                    EntityType.NPC => "",
                    EntityType.Item => "",
                    _ => ""
                };

                if (isSelected)
                {
                    _renderer.Write(2, y, "► ");
                    _renderer.Write(4, y, prefix + entity.DisplayName, ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, y, prefix + entity.DisplayName);
                }
                y++;
            }
        }

        private void RenderSelectedEntityInfo()
        {
            if (_interactableEntities.Count == 0 || _selectedIndex >= _interactableEntities.Count)
                return;

            var selectedEntity = _interactableEntities[_selectedIndex];
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 4;

            _renderer.Write(rightColumn, y, $"=== {selectedEntity.DisplayName} ===", ConsoleColor.Yellow);
            y += 2;

            switch (selectedEntity.Type)
            {
                case EntityType.Monster:
                    RenderMonsterInfo((Monster)selectedEntity.Entity, rightColumn, ref y);
                    break;
                case EntityType.NPC:
                    RenderNPCInfo((NPC)selectedEntity.Entity, rightColumn, ref y);
                    break;
                case EntityType.Item:
                    RenderItemInfo((InventoryItem)selectedEntity.Entity, rightColumn, ref y);
                    break;
            }
        }

        private void RenderMonsterInfo(Monster monster, int x, ref int y)
        {
            _renderer.Write(x, y, $"Уровень: {monster.Level}");
            y++;
            _renderer.Write(x, y, $"Здоровье: {monster.CurrentHP}/{monster.MaximumHP}");
            y++;
            _renderer.Write(x, y, $"Атака: ~{monster.Attack}");
            y++;
            _renderer.Write(x, y, $"Защита: ~{monster.Defence}");
            y++;
            _renderer.Write(x, y, $"Награда: {monster.RewardEXP} опыта, {monster.RewardGold} золота");
            y += 2;

            _renderer.Write(x, y, "Доступные действия:", ConsoleColor.Cyan);
            y++;
            _renderer.Write(x, y, "• Атаковать");
            y++;
            _renderer.Write(x, y, "• Осмотреть");
        }

        private void RenderNPCInfo(NPC npc, int x, ref int y)
        {
            _renderer.Write(x, y, npc.Greeting);
            y += 2;

            _renderer.Write(x, y, "Доступные действия:", ConsoleColor.Cyan);
            y++;
            _renderer.Write(x, y, "• Поговорить");
            y++;

            if (npc.Trader != null)
            {
                _renderer.Write(x, y, "• Торговать");
                y++;
            }

            if (npc.QuestsToGive?.Count > 0)
            {
                _renderer.Write(x, y, "• Квесты");
                y++;
            }

            _renderer.Write(x, y, "• Осмотреть");
        }

        private void RenderItemInfo(InventoryItem item, int x, ref int y)
        {
            _renderer.Write(x, y, $"Тип: {item.Details.Type}");
            y++;
            _renderer.Write(x, y, $"Цена: {item.Details.Price} золота");
            y += 2;

            _renderer.Write(x, y, "Доступные действия:", ConsoleColor.Cyan);
            y++;
            _renderer.Write(x, y, "• Подобрать");
            y++;
            _renderer.Write(x, y, "• Осмотреть");
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (_interactableEntities.Count == 0) return;

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_interactableEntities.Count - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    InteractWithSelectedEntity();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void InteractWithSelectedEntity()
        {
            var entity = _interactableEntities[_selectedIndex];

            switch (entity.Type)
            {
                case EntityType.Monster:
                    InteractWithMonster((Monster)entity.Entity);
                    break;
                case EntityType.NPC:
                    InteractWithNPC((NPC)entity.Entity);
                    break;
                case EntityType.Item:
                    InteractWithItem((InventoryItem)entity.Entity);
                    break;
            }
        }

        private void InteractWithMonster(Monster monster)
        {
            var actions = new List<string> { "Атаковать", "Осмотреть", "Назад" };
            ShowActionMenu($"Взаимодействие с {monster.Name}", actions, (selectedAction) =>
            {
                if (selectedAction == "Атаковать")
                {
                    ScreenManager.PopScreen(); // Закрываем меню взаимодействия
                    _player.StartCombat(monster);
                }
                else if (selectedAction == "Осмотреть")
                {
                    monster.Examine(_player);
                }
            });
        }

        private void InteractWithNPC(NPC npc)
        {
            var actions = npc.GetAvailableActions(_player);
            actions.Add("Назад");

            ShowActionMenu($"Взаимодействие с {npc.Name}", actions, (selectedAction) =>
            {
                if (selectedAction != "Назад")
                {
                    npc.ExecuteAction(_player, selectedAction);
                    ScreenManager.RequestPartialRedraw();
                }
            });
        }

        private void InteractWithItem(InventoryItem item)
        {
            var actions = new List<string> { "Подобрать", "Осмотреть", "Назад" };

            ShowActionMenu($"Взаимодействие с {item.Details.Name}", actions, (selectedAction) =>
            {
                if (selectedAction == "Подобрать")
                {
                    _player.AddItemToInventory(item.Details, item.Quantity);
                    _location.GroundItems.Remove(item);
                    _interactableEntities = GetInteractableEntities(); // Обновляем список
                    MessageSystem.AddMessage($"Подобран: {item.Details.Name} x{item.Quantity}");
                }
                else if (selectedAction == "Осмотреть")
                {
                    item.Details.Read();
                }
            });
        }

        private void ShowActionMenu(string title, List<string> actions, Action<string> onActionSelected)
        {
            int selectedIndex = 0;
            bool inMenu = true;

            while (inMenu)
            {
                _renderer.BeginFrame();
                ClearScreen();

                RenderHeader(title);

                for (int i = 0; i < actions.Count; i++)
                {
                    bool isSelected = i == selectedIndex;
                    if (isSelected)
                    {
                        _renderer.Write(2, 4 + i, "► ");
                        _renderer.Write(4, 4 + i, actions[i], ConsoleColor.Green);
                    }
                    else
                    {
                        _renderer.Write(4, 4 + i, actions[i]);
                    }
                }

                RenderFooter("W/S - выбор │ E - выбрать │ Q - назад");
                _renderer.EndFrame();

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.W:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;
                    case ConsoleKey.S:
                        selectedIndex = Math.Min(actions.Count - 1, selectedIndex + 1);
                        break;
                    case ConsoleKey.E:
                        onActionSelected(actions[selectedIndex]);
                        inMenu = false;
                        break;
                    case ConsoleKey.Q:
                        inMenu = false;
                        break;
                }
            }

            ScreenManager.RequestPartialRedraw();
        }
    }
}