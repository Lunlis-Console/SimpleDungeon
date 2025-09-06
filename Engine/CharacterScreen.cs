namespace Engine
{
    public class CharacterScreen : BaseScreen
    {
        private readonly Player _player;

        public CharacterScreen(Player player)
        {
            _player = player;
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("ХАРАКТЕРИСТИКИ ПЕРСОНАЖА");
            RenderCharacterInfo();
            RenderAttributes();
            RenderStats();
            RenderEquipment();
            RenderFooter("Q - назад │ T - титулы │ I - инвентарь");

            _renderer.EndFrame();
        }

        private void RenderCharacterInfo()
        {
            int y = 4;
            string titleInfo = _player.ActiveTitle != null ? $"[{_player.ActiveTitle.Name}]" : "";

            _renderer.Write(2, y, $"Игрок {titleInfo}");
            _renderer.Write(40, y, $"Уровень: {_player.Level}");
            y++;

            _renderer.Write(2, y, $"Опыт: {_player.CurrentEXP:N0}/{_player.MaximumEXP:N0}");
            _renderer.Write(40, y, $"Золото: {_player.Gold:N0}");
            y += 2;
        }

        private void RenderAttributes()
        {
            int y = 8;
            _renderer.Write(2, y, "=== ОСНОВНЫЕ АТРИБУТЫ ===", ConsoleColor.Cyan);
            y += 2;

            RenderAttribute(2, ref y, "Сила:", _player.Attributes.Strength);
            RenderAttribute(2, ref y, "Телосложение:", _player.Attributes.Constitution);
            RenderAttribute(2, ref y, "Ловкость:", _player.Attributes.Dexterity);
            RenderAttribute(2, ref y, "Интеллект:", _player.Attributes.Intelligence);
            RenderAttribute(2, ref y, "Мудрость:", _player.Attributes.Wisdom);
            RenderAttribute(2, ref y, "Харизма:", _player.Attributes.Charisma);
            y++;
        }

        private void RenderAttribute(int x, ref int y, string name, int value)
        {
            int bonus = value - 10;
            string bonusText = bonus >= 0 ? $"+{bonus}" : bonus.ToString();

            _renderer.Write(x, y, $"{name,-15} {value,2} ({bonusText})");
            y++;
        }

        private void RenderStats()
        {
            int x = 30;
            int y = 8;

            _renderer.Write(x, y, "=== БОЕВЫЕ ПАРАМЕТРЫ ===", ConsoleColor.Cyan);
            y += 2;

            _renderer.Write(x, y, $"Атака:    {_player.Attack,3}");
            y++;
            _renderer.Write(x, y, $"Защита:   {_player.Defence,3}");
            y++;
            _renderer.Write(x, y, $"Ловкость: {_player.Agility,3}");
            y++;
            _renderer.Write(x, y, $"Уклонение: {_player.EvasionChance}%");
            y += 2;

            // Health bar
            RenderHealthBar(x, ref y, "Здоровье:", _player.CurrentHP, _player.TotalMaximumHP);
        }

        private void RenderHealthBar(int x, ref int y, string label, int current, int max)
        {
            float percentage = (float)current / max;
            int bars = (int)(20 * percentage);
            bars = Math.Clamp(bars, 0, 20);

            ConsoleColor color = percentage > 0.5f ? ConsoleColor.Green :
                                percentage > 0.25f ? ConsoleColor.Yellow :
                                ConsoleColor.Red;

            _renderer.Write(x, y, $"{label,-10} [{new string('█', bars)}{new string('░', 20 - bars)}]");
            _renderer.Write(x + 35, y, $"{current}/{max}", color);
            y++;
        }

        private void RenderEquipment()
        {
            int y = 18;
            _renderer.Write(2, y, "=== ЭКИПИРОВКА ===", ConsoleColor.Cyan);
            y += 2;

            RenderEquipmentSlot(2, ref y, "Оружие:", _player.Inventory.MainHand);
            RenderEquipmentSlot(2, ref y, "Щит:", _player.Inventory.OffHand);
            RenderEquipmentSlot(2, ref y, "Шлем:", _player.Inventory.Helmet);
            RenderEquipmentSlot(2, ref y, "Броня:", _player.Inventory.Armor);
            RenderEquipmentSlot(2, ref y, "Перчатки:", _player.Inventory.Gloves);
            RenderEquipmentSlot(2, ref y, "Ботинки:", _player.Inventory.Boots);
            RenderEquipmentSlot(2, ref y, "Амулет:", _player.Inventory.Amulet);
            RenderEquipmentSlot(2, ref y, "Кольцо 1:", _player.Inventory.Ring1);
            RenderEquipmentSlot(2, ref y, "Кольцо 2:", _player.Inventory.Ring2);
        }

        private void RenderEquipmentSlot(int x, ref int y, string slotName, Equipment equipment)
        {
            string equipmentName = equipment?.Name ?? "Пусто";
            int bonus = 0;

            if (equipment != null)
            {
                bonus = equipment.AttackBonus + equipment.DefenceBonus + equipment.AgilityBonus;
            }

            string bonusText = bonus > 0 ? $"(+{bonus})" : "";

            _renderer.Write(x, y, $"{slotName,-10} {equipmentName,-20} {bonusText}");
            y++;
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                case ConsoleKey.C:
                    ScreenManager.PopScreen();
                    break;

                case ConsoleKey.T:
                    ScreenManager.PushScreen(new TitlesScreen(_player));
                    break;

                case ConsoleKey.I:
                    ScreenManager.PushScreen(new InventoryScreen(_player));
                    break;
            }
        }
    }
}