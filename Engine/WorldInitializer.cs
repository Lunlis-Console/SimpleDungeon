namespace Engine
{
    public static class WorldInitializer
    {
        public static void InitializeWithDependencies(IWorldRepository repository)
        {
            // Этот метод будет заполнять репозиторий данными
            // Пока просто инициализируем статический World, но это временное решение
            var _ = GameServices.WorldRepository.GetAllItems; // Принудительная инициализация

            // В будущем здесь будет прямая загрузка в репозиторий
        }
    }
}