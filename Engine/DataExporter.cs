// DataExporter.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Engine
{
    public static class DataExporter
    {
        public static void ExportGameDataToJson(string filePath)
        {
            var worldRepository = GameServices.WorldRepository;
            worldRepository.Initialize();

            var gameData = new GameData();

            // Экспорт предметов
            foreach (var item in worldRepository.GetAllItems())
            {
                var itemData = new ItemData
                {
                    ID = item.ID,
                    Name = item.Name,
                    NamePlural = item.NamePlural,
                    Type = item.Type,
                    Price = item.Price,
                    Description = item.Description
                };

                if (item is Equipment equipment)
                {
                    itemData.AttackBonus = equipment.AttackBonus;
                    itemData.DefenceBonus = equipment.DefenceBonus;
                    itemData.AgilityBonus = equipment.AgilityBonus;
                    itemData.HealthBonus = equipment.HealthBonus;
                }

                if (item is HealingItem healingItem)
                {
                    itemData.AmountToHeal = healingItem.AmountToHeal;
                }

                gameData.Items.Add(itemData);

                CheckForDuplicates(gameData.Items, "Items");
            }

            // Экспорт монстров
            foreach (var monster in worldRepository.GetAllMonsters())
            {
                var monsterData = new MonsterData
                {
                    ID = monster.ID,
                    Name = monster.Name,
                    Level = monster.Level,
                    CurrentHP = monster.CurrentHP,
                    MaximumHP = monster.MaximumHP,
                    RewardEXP = monster.RewardEXP,
                    RewardGold = monster.RewardGold,
                    Attributes = monster.Attributes
                };

                foreach (var lootItem in monster.LootTable)
                {
                    monsterData.LootTable.Add(new LootItemData
                    {
                        ItemID = lootItem.Details.ID,
                        DropPercentage = lootItem.DropPercentage,
                        IsUnique = lootItem.IsUnique
                    });
                }

                gameData.Monsters.Add(monsterData);

                CheckForDuplicates(gameData.Monsters, "Monsters");
            }

            // Экспорт локаций
            foreach (var location in worldRepository.GetAllLocations())
            {
                var locationData = new LocationData
                {
                    ID = location.ID,
                    Name = location.Name,
                    Description = location.Description,
                    ScaleMonstersToPlayerLevel = location.ScaleMonstersToPlayerLevel
                };

                // Добавление ID NPC
                foreach (var npc in location.NPCsHere)
                {
                    locationData.NPCsHere.Add(npc.ID);
                }

                // Добавление ID шаблонов монстров
                // Вместо добавления монстров в список, добавляем данные о спавне
                foreach (var monsterTemplate in location.MonsterTamplates)
                {
                    if (monsterTemplate != null)
                    {
                        locationData.MonsterSpawns.Add(new MonsterSpawnData
                        {
                            MonsterTemplateID = monsterTemplate.ID,
                            Level = monsterTemplate.Level,
                            SpawnWeight = 1 // или любое другое значение
                        });
                    }
                }

                // Связи с другими локациями
                locationData.LocationToNorth = location.LocationToNorth?.ID;
                locationData.LocationToEast = location.LocationToEast?.ID;
                locationData.LocationToSouth = location.LocationToSouth?.ID;
                locationData.LocationToWest = location.LocationToWest?.ID;

                gameData.Locations.Add(locationData);

                CheckForDuplicates(gameData.Locations, "Locations");
            }

            // Экспорт квестов
            foreach (var quest in worldRepository.GetAllQuests())
            {
                var questData = new QuestData
                {
                    ID = quest.ID,
                    Name = quest.Name,
                    Description = quest.Description,
                    RewardEXP = quest.RewardEXP,
                    RewardGold = quest.RewardGold,
                    QuestGiverID = quest.QuestGiver?.ID
                };

                foreach (var questItem in quest.QuestItems)
                {
                    questData.QuestItems.Add(new QuestItemData
                    {
                        ItemID = questItem.Details.ID,
                        Quantity = questItem.Quantity
                    });
                }

                foreach (var rewardItem in quest.RewardItems)
                {
                    questData.RewardItems.Add(new InventoryItemData
                    {
                        ItemID = rewardItem.Details.ID,
                        Quantity = rewardItem.Quantity
                    });
                }

                // Определение типа квеста
                if (quest is CollectibleQuest collectibleQuest)
                {
                    questData.QuestType = "Collectible";
                    foreach (var spawn in collectibleQuest.SpawnLocations)
                    {
                        questData.SpawnLocations.Add(new CollectibleSpawnData
                        {
                            LocationID = spawn.LocationID,
                            ItemID = spawn.ItemID,
                            Quantity = spawn.Quantity
                        });
                    }
                }
                else
                {
                    questData.QuestType = "Standard";
                }

                gameData.Quests.Add(questData);

                CheckForDuplicates(gameData.Quests, "Quests");
            }

            // Экспорт NPC
            foreach (var npc in worldRepository.GetAllNPCs())
            {
                var npcData = new NPCData
                {
                    ID = npc.ID,
                    Name = npc.Name,
                    Greeting = npc.Greeting
                };

                // Добавление ID квестов
                foreach (var quest in npc.QuestsToGive)
                {
                    npcData.QuestsToGive.Add(quest.ID);
                }

                // Данные торговца
                if (npc.Trader is Merchant merchant)
                {
                    npcData.Merchant = new MerchantData
                    {
                        Name = merchant.Name,
                        ShopGreeting = merchant.ShopGreeting,
                        Gold = merchant.Gold
                    };

                    foreach (var item in merchant.ItemsForSale)
                    {
                        npcData.Merchant.ItemsForSale.Add(new InventoryItemData
                        {
                            ItemID = item.Details.ID,
                            Quantity = item.Quantity
                        });
                    }
                }

                gameData.NPCs.Add(npcData);

                CheckForDuplicates(gameData.NPCs, "NPCs");
            }

            // Экспорт титулов
            foreach (var title in worldRepository.GetAllTitles())
            {
                gameData.Titles.Add(new TitleData
                {
                    ID = title.ID,
                    Name = title.Name,
                    Description = title.Description,
                    RequirementType = title.RequirementType,
                    RequirementTarget = title.RequirementTarget,
                    RequirementAmount = title.RequirementAmount,
                    AttackBonus = title.AttackBonus,
                    DefenceBonus = title.DefenceBonus,
                    HealthBonus = title.HealthBonus,
                    BonusAgainstType = title.BonusAgainstType,
                    BonusAgainstAmount = title.BonusAgainstAmount
                });

                CheckForDuplicates(gameData.Titles, "Titles");
            }





            // Сериализация в JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // ← Правильный способ
            };

            string json = JsonSerializer.Serialize(gameData, options);
            File.WriteAllText(filePath, json);

            DebugConsole.Log($"Данные успешно экспортированы в {filePath}");
        }

        private static void CheckForDuplicates<T>(List<T> list, string name) where T : class
        {
            var duplicates = list.GroupBy(x => x.GetType().GetProperty("ID").GetValue(x))
                                .Where(g => g.Count() > 1)
                                .Select(g => g.Key);

            foreach (var dupId in duplicates)
            {
                DebugConsole.Log($"Дубликат в {name}: ID {dupId}");
            }
        }
    }
}