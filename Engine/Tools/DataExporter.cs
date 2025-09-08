// DataExporter.cs — полная версия, готовая к вставке.
// Замените существующий файл DataExporter.cs на этот или вставьте содержимое в тот же файл/класс.
// Комментарии и логи на русском для удобства отладки.

using Engine.Core;
using Engine.Entities;
using Engine.Quests;
using Engine.Trading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using Engine.Data;

namespace Engine.Tools
{
    public static class DataExporter
    {
        /// <summary>
        /// Экспортирует все игровые данные в JSON.
        /// Параметр может быть либо путем к файлу (например ...\game_data.json),
        /// либо путем к папке (в этом случае создаётся game_data.json в этой папке).
        /// Метод защищён от типичных проблем с записью в bin\Debug и использует fallback в LocalAppData при необходимости.
        /// </summary>
        public static void ExportGameDataToJson(string fileOrFolder)
        {
            // Время старта операции — используем для поиска "свежих" файлов, если потребуется
            var startTimeUtc = DateTime.UtcNow;
            try
            {
                // 1) Подготовка данных (воспользуемся вашим WorldRepository)
                var worldRepository = GameServices.WorldRepository;
                worldRepository.Initialize();

                var gameData = new GameData();

                // -------------------------
                // Здесь помещаем вашу логику заполнения gameData.
                // Я воспроизвожу её по сути такой же, как у вас — просто переносим код внутрь.
                // -------------------------

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

                    // NPC IDs
                    foreach (var npc in location.NPCsHere)
                    {
                        locationData.NPCsHere.Add(npc.ID);
                    }

                    // Monster templates -> MonsterSpawns
                    foreach (var monsterTemplate in location.MonsterTamplates)
                    {
                        if (monsterTemplate != null)
                        {
                            locationData.MonsterSpawns.Add(new MonsterSpawnData
                            {
                                MonsterTemplateID = monsterTemplate.ID,
                                Level = monsterTemplate.Level,
                                SpawnWeight = 1
                            });
                        }
                    }

                    // Связи с другими локациями (по ID)
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

                    foreach (var quest in npc.QuestsToGive)
                    {
                        npcData.QuestsToGive.Add(quest.ID);
                    }

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

                // -------------------------
                // Конец блока формирования gameData
                // -------------------------

                // Сериализация в JSON (с полным набором unicode)
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                };

                string json = JsonSerializer.Serialize(gameData, options);

                // Подготовим корректный путь к файлу (если передали папку — ставим game_data.json внутри)
                string desiredFile = ResolveFilePath(fileOrFolder);

                DebugConsole.Log($"DataExporter: подготовлен путь к файлу: {desiredFile}");

                // Попытка записи с атомарностью + fallback
                if (TryWriteFileWithFallback(desiredFile, json, out string actualPath, out string err))
                {
                    DebugConsole.Log($"Данные успешно экспортированы в {actualPath}");
                }
                else
                {
                    DebugConsole.Log($"Ошибка при экспорте: {err}");
                    throw new IOException($"Не удалось записать файл экспорта: {err}");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"DataExporter: непредвиденная ошибка при экспорте: {ex.GetType().Name}: {ex.Message}");
                DebugConsole.Log(ex.StackTrace ?? "<no-stack>");
                throw;
            }
        }

        // -----------------------
        // Вспомогательные методы
        // -----------------------

        /// <summary>
        /// Если пользователь передал файл — используем его.
        /// Если передал папку — формируем file = folder\game_data.json.
        /// Если ничего не передано — пытаемся найти корень проекта и создать game_data.json там.
        /// </summary>
        private static string ResolveFilePath(string fileOrFolder)
        {
            if (string.IsNullOrWhiteSpace(fileOrFolder))
            {
                var projRoot = FindProjectRoot();
                var folder = Path.Combine(projRoot ?? AppDomain.CurrentDomain.BaseDirectory ?? ".", "game_data");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "game_data.json");
            }

            // Если строка имеет расширение — однозначно это файл.
            if (Path.HasExtension(fileOrFolder))
            {
                var dir = Path.GetDirectoryName(fileOrFolder);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);
                return Path.GetFullPath(fileOrFolder);
            }

            // Если существующий файл
            if (File.Exists(fileOrFolder))
                return Path.GetFullPath(fileOrFolder);

            // Если существующая папка
            if (Directory.Exists(fileOrFolder))
                return Path.Combine(Path.GetFullPath(fileOrFolder), "game_data.json");

            // Иначе создаём папку и используем её
            Directory.CreateDirectory(fileOrFolder);
            return Path.Combine(Path.GetFullPath(fileOrFolder), "game_data.json");
        }

        /// <summary>
        /// Попытка безопасно записать файл: атомарная запись tmp->replace/move с повторными попытками.
        /// При неудаче — fallback в LocalAppData.
        /// </summary>
        private static bool TryWriteFileWithFallback(string destFilePath, string content, out string actualPath, out string error)
        {
            actualPath = destFilePath;
            error = null;

            if (TryAtomicWriteWithRetries(destFilePath, content, maxAttempts: 5, waitMs: 500, out string attemptError))
            {
                return true;
            }

            DebugConsole.Log($"DataExporter: запись в желаемый путь не удалась: {attemptError}. Попытка fallback.");

            try
            {
                string fallbackDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleDungeon", "game_data");
                Directory.CreateDirectory(fallbackDir);
                var fallbackFile = Path.Combine(fallbackDir, Path.GetFileName(destFilePath));
                if (TryAtomicWriteWithRetries(fallbackFile, content, maxAttempts: 5, waitMs: 500, out string fbErr))
                {
                    actualPath = fallbackFile;
                    return true;
                }
                else
                {
                    error = $"fallback failed: {fbErr}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"fallback exception: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Атомарная запись: create tmp file, затем Replace/Move в целевой. Делает несколько попыток при ошибках.
        /// </summary>
        private static bool TryAtomicWriteWithRetries(string destFilePath, string content, int maxAttempts, int waitMs, out string error)
        {
            error = null;
            try
            {
                var destDir = Path.GetDirectoryName(destFilePath);
                if (string.IsNullOrWhiteSpace(destDir)) destDir = AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(destDir);

                string tmpFile = Path.Combine(destDir, $".tmp_{Guid.NewGuid():N}.json");
                File.WriteAllText(tmpFile, content);

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        if (File.Exists(destFilePath))
                        {
                            // Попытка atomically заменить целевой файл
                            File.Replace(tmpFile, destFilePath, null);
                        }
                        else
                        {
                            File.Move(tmpFile, destFilePath);
                        }

                        // Успех
                        return true;
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        DebugConsole.Log($"DataExporter: попытка {attempt}: UnauthorizedAccess при перемещении/замене: {uaEx.Message}");
                        try
                        {
                            if (File.Exists(destFilePath))
                            {
                                var attrs = File.GetAttributes(destFilePath);
                                if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                {
                                    File.SetAttributes(destFilePath, attrs & ~FileAttributes.ReadOnly);
                                    DebugConsole.Log($"DataExporter: снят ReadOnly для {destFilePath}");
                                }
                            }
                        }
                        catch (Exception attrEx)
                        {
                            DebugConsole.Log($"DataExporter: не удалось снять ReadOnly: {attrEx.GetType().Name}: {attrEx.Message}");
                        }
                    }
                    catch (IOException ioEx)
                    {
                        DebugConsole.Log($"DataExporter: попытка {attempt}: IOException при перемещении/замене: {ioEx.Message}");
                        // файл, возможно, занят - попробуем снова после паузы
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log($"DataExporter: попытка {attempt}: неожиданная ошибка при перемещении/замене: {ex.GetType().Name}: {ex.Message}");
                    }

                    // Подождать перед повторной попыткой
                    Thread.Sleep(waitMs);
                }

                // Удаляем временный файл, если он остался
                try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { }

                error = "max attempts exceeded while trying to replace/move temp file to destination.";
                return false;
            }
            catch (Exception ex)
            {
                error = $"Exception preparing write: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Пытаемся обнаружить корень проекта (где есть .csproj) — поднимаемся вверх не глубже 10 уровней.
        /// Если не найдено — возвращаем null.
        /// </summary>
        private static string FindProjectRoot()
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory());
                for (int i = 0; i < 10 && dir != null; i++)
                {
                    if (dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        /// <summary>
        /// Ваша проверка на дубликаты — почти идентична оригиналу, но с дополнительными логами.
        /// </summary>
        private static void CheckForDuplicates<T>(List<T> list, string name) where T : class
        {
            if (list == null || list.Count == 0)
            {
                return;
            }

            var prop = typeof(T).GetProperty("ID", BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                DebugConsole.Log($"CheckForDuplicates: тип {typeof(T).FullName} не содержит публичного свойства 'ID'.");
                return;
            }

            var duplicates = list
                .GroupBy(x => prop.GetValue(x))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count == 0) return;

            foreach (var dupId in duplicates)
            {
                string idText = dupId == null ? "<null>" : dupId.ToString();
                DebugConsole.Log($"Дубликат в {name}: ID {idText}");
            }
        }
    }
}
