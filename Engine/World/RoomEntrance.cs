using Engine.Entities;

namespace Engine.World
{
    /// <summary>
    /// Представляет вход в помещение, который можно увидеть и с которым можно взаимодействовать
    /// </summary>
    public class RoomEntrance : IInteractable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TargetRoomID { get; set; } // ID целевого помещения
        public int ParentLocationID { get; set; } // ID локации, где находится вход
        public string EntranceType { get; set; } // Тип входа: "cave", "dungeon", "city", "building", etc.
        public bool IsLocked { get; set; } = false;
        public string LockDescription { get; set; } = ""; // Описание замка/препятствия
        public List<int> RequiredItemIDs { get; set; } = new List<int>(); // Предметы для открытия
        public bool RequiresKey { get; set; } = false;
        public int RequiredKeyID { get; set; } = 0;

        public RoomEntrance(int id, string name, string description, int targetRoomID, 
            int parentLocationID, string entranceType = "entrance")
        {
            ID = id;
            Name = name;
            Description = description;
            TargetRoomID = targetRoomID;
            ParentLocationID = parentLocationID;
            EntranceType = entranceType;
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string>();

            if (CanEnter(player))
            {
                actions.Add("Войти");
            }
            else
            {
                if (IsLocked)
                {
                    actions.Add("Попытаться открыть");
                }
            }

            actions.Add("Осмотреть");
            return actions;
        }

        public void ExecuteAction(Player player, string action)
        {
            switch (action)
            {
                case "Войти":
                    EnterRoom(player);
                    break;
                case "Попытаться открыть":
                    TryToUnlock(player);
                    break;
                case "Осмотреть":
                    InspectEntrance(player);
                    break;
            }
        }

        private bool CanEnter(Player player)
        {
            if (!IsLocked) return true;

            // Проверяем наличие ключа
            if (RequiresKey && RequiredKeyID > 0)
            {
                return player.Inventory.HasItem(RequiredKeyID);
            }

            // Проверяем наличие требуемых предметов
            if (RequiredItemIDs.Count > 0)
            {
                foreach (int itemID in RequiredItemIDs)
                {
                    if (!player.Inventory.HasItem(itemID))
                        return false;
                }
            }

            return true;
        }

        private void EnterRoom(Player player)
        {
            if (!CanEnter(player))
            {
                Engine.Core.MessageSystem.AddMessage("Вы не можете войти в это помещение.");
                return;
            }

            // Получаем целевое помещение
            var worldRepo = Engine.Core.GameServices.WorldRepository;
            var targetRoom = worldRepo.RoomByID(TargetRoomID);
            
            if (targetRoom == null)
            {
                Engine.Core.MessageSystem.AddMessage("Помещение недоступно.");
                return;
            }

            // Перемещаем игрока в помещение
            player.MoveToRoom(targetRoom);
            
            // Закрываем экран взаимодействия и возвращаемся к игровому миру
            ScreenManager.PopScreen();
            ScreenManager.RequestFullRedraw();
        }

        private void TryToUnlock(Player player)
        {
            if (!IsLocked)
            {
                Engine.Core.MessageSystem.AddMessage("Вход не заперт.");
                return;
            }

            if (CanEnter(player))
            {
                IsLocked = false;
                Engine.Core.MessageSystem.AddMessage("Вы успешно открыли вход!");
            }
            else
            {
                Engine.Core.MessageSystem.AddMessage(LockDescription);
            }
        }

        private void InspectEntrance(Player player)
        {
            var inspectionText = Description;
            
            if (IsLocked)
            {
                inspectionText += $"\n\n{LockDescription}";
                
                if (RequiresKey && RequiredKeyID > 0)
                {
                    inspectionText += "\nТребуется ключ для открытия.";
                }
                
                if (RequiredItemIDs.Count > 0)
                {
                    inspectionText += "\nТребуются специальные предметы для открытия.";
                }
            }

            Engine.Core.MessageSystem.AddMessage(inspectionText);
        }

        /// <summary>
        /// Получает отображаемое имя входа с учетом типа
        /// </summary>
        public string GetDisplayName()
        {
            string prefix = EntranceType switch
            {
                "cave" => "Пещера",
                "dungeon" => "Подземелье", 
                "city" => "Город",
                "building" => "Здание",
                "tower" => "Башня",
                "temple" => "Храм",
                "forest" => "Лес",
                _ => "Вход"
            };

            return $"{prefix}: {Name}";
        }
    }
}
