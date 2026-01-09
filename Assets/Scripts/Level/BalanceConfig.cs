using System;
using System.Collections.Generic;

namespace Level
{
 

    [Serializable]
    public class BalanceConfigRoot
    {
        public List<DifficultyBlock> difficulties = new();
    }

    [Serializable]
    public class DifficultyBlock
    {
        // Храним строкой, потому что JsonUtility плохо работает с enum в виде поля
        public string difficulty; // "Easy" / "Normal" / "Hard"
        public List<HeroBalance> heroes = new();
    }

    [Serializable]
    public class HeroBalance
    {
        public string hero; // "Lich", "Skeleton", ...
        public int maxHp = 100;
        public int maxMana = 100;
        public int xpReward = 0;
        public int damage = 13;
        public int cost = 3;
    }

// Удобная структура для выдачи результата
    public struct HeroBalanceData
    {
        public int MaxHp;
        public int MaxMana;
        public int XpReward;
        public int Damage;
        public int Cost;
    }

}