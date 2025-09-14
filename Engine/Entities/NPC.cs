using Engine.Core;
using Engine.Dialogue;
using Engine.Quests;
using Engine.Trading;
using Engine.UI;
using Engine.World;

namespace Engine.Entities
{
    public class NPC : IInteractable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Greeting { get; set; }
        public List<int> QuestsToGive { get; set; }
        public virtual List<InventoryItem> ItemsForSale { get; set; }
        public virtual int Gold { get; set; }
        public virtual int BuyPriceModifier => 100; // 100% по умолчанию
        public virtual int SellPriceModifier => 80; // 80% по умолчанию
        public ITrader Trader { get; set; } // Добавляем это свойство
        public DialogueSystem.DialogueNode GreetingDialogue { get; set; }

        private readonly IWorldRepository _worldRepository;

        public NPC(int id, string name, string greeting = "", IWorldRepository worldRepository = null)
        {
            ID = id;
            Name = name;
            Greeting = greeting;
            QuestsToGive = new List<int>();
            _worldRepository = worldRepository;
        }

        // Новый метод для инициализации магазина
        public virtual void InitializeShop(Player player)
        {
            // Базовая реализация - может быть переопределена
            ItemsForSale = new List<InventoryItem>();
        }

        public virtual bool CanAfford(Item item, int quantity, Player player)
        {
            return player.Gold >= item.Price * BuyPriceModifier / 100 * quantity;
        }

        public virtual string GetShopGreeting()
        {
            return $"{Name}: Что желаете приобрести?";
        }

        // В методе GetAvailableActions добавьте:
        private class MenuOption
        {
            public string DisplayText { get; }
            public Action Action { get; }

            public MenuOption(string displayText, Action action)
            {
                DisplayText = displayText;
                Action = action;
            }
        }

        // Новый метод Talk - просто начинает разговор
        public virtual void Talk(Player player)
        {
            if (GreetingDialogue != null)
            {
                // Создаём DialogueScreen, передаём player и, если есть, заранее подготовленного трейдера.
                // Это соответствует конструктору DialogueScreen(NPC npc, Player player, ITrader traderForDialogue = null)
                var dialogueScreen = new DialogueScreen(this, player, this.Trader);

                // Устанавливаем текущий узел диалога — GreetingDialogue (чтобы показать нужный текст).
                // SetCurrentNode безопасен — он проверяет null внутри себя.
                try
                {
                    dialogueScreen.SetCurrentNode(GreetingDialogue);
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"NPC.Talk: SetCurrentNode failed: {ex.Message}");
                }

                ScreenManager.PushScreen(dialogueScreen);
            }
            else
            {
                // Fallback к старой системе
                HaveConversation(player);
            }
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Поговорить" };

            // Автоматически добавляем торговлю если NPC - торговец
            if (Trader != null)
            {
                actions.Add("Торговать");
            }

            // Автоматически добавляем квесты если они доступны
            if (HasAvailableQuests(player) || HasCompletableQuests(player))
            {
                actions.Add("Задания");
            }

            actions.Add("Осмотреть");
            return actions;
        }
        private bool HasAvailableQuests(Player player)
        {
            return QuestsToGive?.Any(questID =>
                !player.QuestLog.ActiveQuests.Any(q => q.ID == questID) &&
                !player.QuestLog.CompletedQuests.Any(q => q.ID == questID)) ?? false;
        }

        private bool HasCompletableQuests(Player player)
        {
            return QuestsToGive?.Any(questID =>
                player.QuestLog.ActiveQuests.Any(q => q.ID == questID && q.State == QuestState.ReadyToComplete)) ?? false;
        }

        protected virtual void HaveConversation(Player player)
        {
            Console.Clear();
            Console.WriteLine($"╔{new string('═', Name.Length + 4)}╗");
            Console.WriteLine($"║  {Name}  ║");
            Console.WriteLine($"╚{new string('═', Name.Length + 4)}╝");
            Console.WriteLine();
            Console.WriteLine(Greeting);
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            Console.ReadKey(true);
        }


        public void AddQuest(int questID)
        {
            if (QuestsToGive == null)
            {
                QuestsToGive = new List<int>();
            }

            if (!QuestsToGive.Contains(questID))
            {
                QuestsToGive.Add(questID);
            }
        }

        // В методе ExecuteAction добавьте:
        public void ExecuteAction(Player player, string action)
        {
            switch (action)
            {
                case "Поговорить":
                    Talk(player);
                    break;
                case "Осмотреть":
                    Examine(player);
                    break;
                case "Торговать":
                    if (Trader != null)
                    {
                        // подготовка ассортимента (как было)
                        Trader.InitializeShop(player);

                        // Открываем новый экран торговли через ScreenManager
                        var tradeScreen = new Engine.UI.TradeScreen(Trader, player);
                        ScreenManager.PushScreen(tradeScreen);
                        // ScreenManager.PushScreen уже вызывает RequestFullRedraw() внутри себя,
                        // поэтому дополнительный вызов чаще не обязателен.
                    }
                    break;
                case "Квесты":
                    ShowQuestMenu(player);
                    break;
            }
        }

        private void ShowQuestMenu(Player player)
        {
            // Логика меню квестов (перенести из старого Talk метода)
        }

        // Новый метод для осмотра NPC
        public virtual void Examine(Player player)
        {
            Console.Clear();
            Console.WriteLine($"============ ОСМОТР: {Name} ============");
            Console.WriteLine("Это житель деревни.");

            // Информация о квестах
            if (QuestsToGive?.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Возможные задания:");

                foreach (var questID in QuestsToGive)
                {
                    var quest = player.QuestLog.GetQuest(questID);
                    if (quest == null) continue;

                    string status = "❓"; // Доступен
                    if (player.QuestLog.ActiveQuests.Any(q => q.ID == questID))
                        status = quest.State == QuestState.ReadyToComplete ? "✅" : "⏳";
                    else if (player.QuestLog.CompletedQuests.Any(q => q.ID == questID))
                        status = "✔️";

                    Console.WriteLine($" {status} {quest.Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            Console.ReadKey(true);
        }
    }
}