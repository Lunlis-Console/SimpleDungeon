namespace Engine.Core
{
    public static class RandomNumberGenerator
    {
        private static Random _generator = new Random();

        public static int NumberBetween(int minValue, int maxValue)
        {
            return _generator.Next(minValue, maxValue + 1);
        }
    }
}
