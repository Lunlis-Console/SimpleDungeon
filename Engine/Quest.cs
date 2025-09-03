using System;
using System.Collections.Generic;

namespace Engine
{
    public class Quest
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int RewardEXP { get; set; }
        public int RewardGold { get; set; }
        public List<QuestItem> QuestItems { get; set; }
        public List<InventoryItem> RewardItems { get; set; }
        public bool IsCompleted { get; set; }
        public NPC QuestGiver { get; set; } // Новое свойство
        public Action<Player> OnQuestComplete { get; set; }

        public Quest(int id, string name, string description, int rewardEXP, int rewardGold, NPC questGiver = null)
        {
            ID = id;
            Name = name;
            Description = description;
            RewardEXP = rewardEXP;
            RewardGold = rewardGold;
            QuestItems = new List<QuestItem>();
            RewardItems = new List<InventoryItem>();
            IsCompleted = false;
            QuestGiver = questGiver;
        }

        public bool CheckCompletion(Player player)
        {
            // Проверка на null
            if (player?.Inventory == null || QuestItems == null)
                return false;

            foreach (var questItem in QuestItems)
            {
                var playerItem = player.Inventory.Items.Find(ii => ii.Details.ID == questItem.Details.ID);
                if (playerItem == null || playerItem.Quantity < questItem.Quantity)
                    return false;
            }
            return true;
        }

        public void CompleteQuest(Player player)
        {
            if (!CheckCompletion(player)) return;

            

            // Выдача наград
            player.Gold += RewardGold;
            player.CurrentEXP += RewardEXP;
            player.QuestsCompleted++;
            
            // Выдача предметов
            foreach (var rewardItem in RewardItems)
            {
                player.AddItemToInventory(rewardItem.Details, rewardItem.Quantity);
            }

            // Удаление квестовых предметов
            foreach (var questItem in QuestItems)
            {
                var playerItem = player.Inventory.Items.Find(ii => ii.Details.ID == questItem.Details.ID);
                if (playerItem != null)
                {
                    playerItem.Quantity -= questItem.Quantity;
                    if (playerItem.Quantity <= 0)
                        player.Inventory.RemoveItem(playerItem);
                }
            }

            OnQuestComplete?.Invoke(player);
            IsCompleted = true;
            MessageSystem.AddMessage($"Квест '{Name}' завершен!");
        }
    }
}