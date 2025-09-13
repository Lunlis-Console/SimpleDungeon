using Engine.Core;
using Engine.Entities;
using Engine.World;

namespace Engine.UI
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
            // Если нет сущностей — разрешаем выйти по Esc/Q/Enter (Enter — для удобства),
            // но возвращаемся, чтобы не выполнять навигацию по пустому списку.
            if (_interactableEntities == null || _interactableEntities.Count == 0)
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                    case ConsoleKey.Enter:
                        // Закрыть экран взаимодействия — возврат в мир
                        ScreenManager.PopScreen();
                        // Обновим экран
                        ScreenManager.RequestPartialRedraw();
                        try { if (DebugConsole.Enabled && DebugConsole.IsVisible) DebugConsole.GlobalDraw(); } catch { }
                        break;
                    default:
                        // игнорируем остальные клавиши
                        break;
                }
                return;
            }

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

        private BaseScreen _pendingScreenToPush = null;

        private void InteractWithMonster(Monster monster)
        {
            DebugConsole.Log("[interact] opening menu");
            ShowActionMenu($"Взаимодействие с {monster.Name}", new List<string> { "Атаковать", "Осмотреть", "Назад" }, (sel) =>
            {
                DebugConsole.Log("[interact] selected: " + sel);
                if (sel == "Осмотреть")
                {
                    _pendingScreenToPush = new MonsterInspectScreen(monster);
                    // ...
                    if (_pendingScreenToPush != null)
                    {
                        ScreenManager.PushScreen(_pendingScreenToPush);
                        _pendingScreenToPush = null;
                        // После того, как вы добавили экран в стек:

                        // Гарантируем, что движок знает о необходимости полной перерисовки:
                        ScreenManager.RequestFullRedraw();
                        GameServices.BufferedRenderer?.SetNeedsFullRedraw();

                        // --- Диагностический принудительный рендер кадра (убрать в релизе) ---
                        // Попробуем вызвать метод рендера у ScreenManager, если он есть.
                        // Этот вызов ускоряет появление экрана в момент отладки.
                        // Если у вас нет публичного метода RenderCurrentScreen, попытка будет безопасно проигнорирована.
                        try
                        {
                            // 1) если есть метод RenderCurrentScreen(), вызовем его напрямую
                            var smType = typeof(ScreenManager);
                            var renderMethod = smType.GetMethod("RenderCurrentScreen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                                               ?? smType.GetMethod("Render", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                            if (renderMethod != null)
                            {
                                renderMethod.Invoke(null, null);
                            }
                            else
                            {
                                // 2) В качестве крайней меры — попытаемся вызвать Render() у текущего скрина (если доступен)
                                var current = smType.GetProperty("CurrentScreen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                                if (current != null)
                                {
                                    var renderCur = current.GetType().GetMethod("Render", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    renderCur?.Invoke(current, null);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Логируем, но не ломаем выполнение
                            DebugConsole.Log($"[diag] forced render failed: {ex.GetType().Name}: {ex.Message}");
                        }

                    }
                }
                else if (sel == "Атаковать")
                {
                    _player.StartCombat(monster);
                }
            });

            DebugConsole.Log("[interact] menu closed, pending: " + (_pendingScreenToPush != null));
            if (_pendingScreenToPush != null)
            {
                ScreenManager.PushScreen(_pendingScreenToPush);
                _pendingScreenToPush = null;
                ScreenManager.RequestFullRedraw();
                DebugConsole.Log("[interact] pushed inspect screen");
            }
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
                // Рисуем меню
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

                // Если консоль видима, нарисуем её поверх (чтобы было видно)
                try
                {
                    if (DebugConsole.Enabled && DebugConsole.IsVisible)
                    {
                        DebugConsole.GlobalDraw();
                    }
                }
                catch { /* не критично */ }

                // Ждём клавишу
                var key = Console.ReadKey(true);

                // 1) Глобальная горячая клавиша для консоли — обрабатываем прямо в меню
                if (key.Key == ConsoleKey.F3)
                {
                    DebugConsole.Toggle();
                    // Перерисуем меню/экран целиком чтобы скрыть/показать следы консоли
                    ScreenManager.RequestFullRedraw();
                    continue; // не обрабатываем дальше эту клавишу в меню
                }

                // 2) Если консоль видима — передаём ввод консоли вместо меню
                if (DebugConsole.Enabled && DebugConsole.IsVisible)
                {
                    // Передаём клавишу в консоль (она сама обновит внутреннее состояние)
                    DebugConsole.ProcessInput(key);
                    // Обновим консольное отображение сразу
                    try { DebugConsole.GlobalDraw(); } catch { }
                    continue; // пропускаем обработку меню пока консоль активна
                }

                // 3) Обычная обработка клавиш меню
                switch (key.Key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(actions.Count - 1, selectedIndex + 1);
                        break;
                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                        // вызов колбэка выбора
                        try { onActionSelected(actions[selectedIndex]); } catch { }
                        inMenu = false;
                        break;
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        inMenu = false;
                        break;
                }
            }

            // После выхода из меню — обновляем экран и даём шанс нарисовать консоль/экран
            ScreenManager.RequestPartialRedraw();
            try { if (DebugConsole.Enabled && DebugConsole.IsVisible) DebugConsole.GlobalDraw(); } catch { }
        }
    }
}