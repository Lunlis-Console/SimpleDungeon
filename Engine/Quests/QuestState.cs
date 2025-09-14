namespace Engine.Quests
{
    public enum QuestState
    {
        NotStarted,     // Квест не принят
        InProgress,     // Квест принят, выполняется
        ReadyToComplete,// Все условия выполнены, можно завершить
        Completed,      // Квест завершен
        Failed          // Квест провален (опционально)
    }
}