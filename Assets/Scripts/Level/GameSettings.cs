namespace Level
{
    using UnityEngine;

    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    public static class GameSettings
    {
        private const string PrefKey = "GAME_DIFFICULTY";

        public static GameDifficulty Difficulty { get; private set; } = GameDifficulty.Normal;

        public static void SetDifficulty(GameDifficulty difficulty, bool saveToPrefs = true)
        {
            Difficulty = difficulty;

            if (saveToPrefs)
                PlayerPrefs.SetInt(PrefKey, (int)difficulty);
        }

        public static void LoadDifficulty()
        {
            if (PlayerPrefs.HasKey(PrefKey))
                Difficulty = (GameDifficulty)PlayerPrefs.GetInt(PrefKey);
        }
    }

}