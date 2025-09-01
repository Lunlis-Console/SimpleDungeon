using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
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
