namespace Engine
{
    public class Attributes
    {
        public int Strength { get; set; }      // Сила - влияет на физическую атаку, перенос веса
        public int Constitution { get; set; }  // Телосложение - влияет на HP, защиту
        public int Dexterity { get; set; }     // Ловкость - уже есть, влияет на инициативу, уклонение
        public int Intelligence { get; set; }  // Интеллект - магическая атака, знания
        public int Wisdom { get; set; }        // Мудрость - восприятие, магическая защита
        public int Charisma { get; set; }      // Харизма - убеждение, торговля

        public Attributes(int strength = 10, int constitution = 10, int dexterity = 10,
                         int intelligence = 10, int wisdom = 10, int charisma = 10)
        {
            Strength = strength;
            Constitution = constitution;
            Dexterity = dexterity;
            Intelligence = intelligence;
            Wisdom = wisdom;
            Charisma = charisma;
        }
    }
}
