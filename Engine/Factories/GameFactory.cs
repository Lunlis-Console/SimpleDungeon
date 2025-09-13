using Engine.Core;
using Engine.Entities;
using Engine.Saving;
using Engine.World;
using Engine.Dialogue;

namespace Engine.Factories
{
    public class GameFactory : IGameFactory
    {
        private readonly IWorldRepository _worldRepository;

        public GameFactory(IWorldRepository worldRepository)
        {
            _worldRepository = worldRepository;

            try
            {
                DialogueSystem.DialogueOption.RegisterDefaultActionHandlers();
                DebugConsole.Log("Dialogue action handlers registered by GameFactory.");
            }
            catch (Exception ex)
            {
                DebugConsole.Log("Failed to register dialogue action handlers in GameFactory constructor: " + ex.Message);
            }

        }

        public Player CreateNewPlayer()
        {
            return new Player("Странник", 0, 100, 100, 0, 100, 1, 0, 0, 10, _worldRepository);

        }

        public Player CreatePlayerFromSave(GameSave save)
        {
            var player = new Player(
                save.SaveName ,save.Gold, save.CurrentHP, save.MaximumHP,
                save.CurrentEXP, save.MaximumEXP, save.Level,
                save.BaseAttack, save.BaseDefence, save.BaseAgility,
                _worldRepository
            );

            // ... остальная логика загрузки
            return player;
        }

        public Monster CreateMonster(int monsterId, int level)
        {
            var baseMonster = _worldRepository.MonsterByID(monsterId);
            return new Monster(baseMonster, level);
        }
        public InventoryItem CreateInventoryItem(string param)
        {
            if (string.IsNullOrWhiteSpace(param)) return null;

            try
            {
                // Попробуем распарсить простую форму "id,qty" или "name,qty"
                var working = param.Trim();

                // Обработка форматов вида "itemId:...;qty:..."
                var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (working.Contains(":") && (working.Contains(";") || working.Contains(" ")))
                {
                    // разделяем по ';' и по пробелам на пары key:value
                    var segs = working.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in segs)
                    {
                        var parts = s.Split(new[] { ':' }, 2);
                        if (parts.Length == 2) kv[parts[0].Trim().ToLowerInvariant()] = parts[1].Trim();
                    }
                }

                // если обнаружен itemid/qty в kv — используем их
                if (kv.ContainsKey("itemid") || kv.ContainsKey("id"))
                {
                    var idStr = kv.ContainsKey("itemid") ? kv["itemid"] : kv["id"];
                    int qty = 1;
                    if (kv.ContainsKey("qty") && int.TryParse(kv["qty"], out var qv)) qty = qv;
                    else if (kv.ContainsKey("quantity") && int.TryParse(kv["quantity"], out var qv2)) qty = qv2;

                    if (int.TryParse(idStr, out var numericId))
                    {
                        return CreateInventoryItemById(numericId, qty);
                    }
                    // если id указан строкой — попробуем поиск по имени/description
                    return CreateInventoryItemByName(idStr, qty);
                }

                // пробуем split "some_id,5"
                var partsSimple = working.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string idPart = partsSimple.Length > 0 ? partsSimple[0].Trim() : null;
                int qtyPart = 1;
                if (partsSimple.Length > 1 && int.TryParse(partsSimple[1].Trim(), out var tmpq)) qtyPart = tmpq;

                if (!string.IsNullOrEmpty(idPart))
                {
                    if (int.TryParse(idPart, out var numericId2))
                    {
                        return CreateInventoryItemById(numericId2, qtyPart);
                    }

                    // Поиск по имени или по Description
                    return CreateInventoryItemByName(idPart, qtyPart);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"GameFactory.CreateInventoryItem error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Прямое создание по числовому ID.
        /// </summary>
        public InventoryItem CreateInventoryItemById(int id, int qty = 1)
        {
            try
            {
                var item = _worldRepository.ItemByID(id);
                if (item == null) return null;
                return new InventoryItem(item, qty);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"GameFactory.CreateInventoryItemById error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Вспомогательный метод: поиск предмета по имени/описанию (нормализованный поиск).
        /// </summary>
        private InventoryItem CreateInventoryItemByName(string needle, int qty = 1)
        {
            if (string.IsNullOrWhiteSpace(needle)) return null;
            try
            {
                var all = _worldRepository.GetAllItems();
                if (all == null || all.Count == 0) return null;

                // первый проход: поиск подстроки в Description или Name (регистронезависимо)
                var found = all.FirstOrDefault(it =>
                    (!string.IsNullOrEmpty(it.Description) && it.Description.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(it.Name) && it.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                );

                // второй проход: нормализованный поиск (удаляем не-алфанум)
                if (found == null)
                {
                    string Normalize(string s) => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                    var nneedle = Normalize(needle);
                    if (!string.IsNullOrEmpty(nneedle))
                    {
                        found = all.FirstOrDefault(it =>
                            !string.IsNullOrEmpty(it.Name) && Normalize(it.Name).IndexOf(nneedle, StringComparison.OrdinalIgnoreCase) >= 0
                        );
                    }
                }

                if (found != null) return new InventoryItem(found, qty);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"GameFactory.CreateInventoryItemByName error: {ex.Message}");
            }

            return null;
        }
    }
}
