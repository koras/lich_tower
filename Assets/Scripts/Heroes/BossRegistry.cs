using System.Collections.Generic;
using UnityEngine;

namespace Heroes
{
    public static class BossRegistry
    {
        // team -> boss transform
        private static readonly Dictionary<int, Transform> _bossByTeam = new();

        public static void RegisterBoss(int team, Transform boss) => _bossByTeam[team] = boss;
        public static void UnregisterBoss(int team, Transform boss)
        {
            if (_bossByTeam.TryGetValue(team, out var cur) && cur == boss)
                _bossByTeam.Remove(team);
        }
        public static string DebugInfo()
        {
            var info = $"Всего боссов: {_bossByTeam.Count}\n";
            foreach (var kv in _bossByTeam)
            {
                info += $"  Команда {kv.Key}: {kv.Value?.name ?? "null"}\n";
            }
            return info;
        }
        public static Transform GetEnemyBoss(int myTeam)
        {
            Debug.Log($" myTeam== {myTeam}");
            // если у тебя 2 команды: 0/1
            foreach (var kv in _bossByTeam)
            {
                if (kv.Key == myTeam) continue;
                
                Debug.Log($" myTeam {myTeam} {_bossByTeam}");

                if (kv.Value == null) continue;
                return kv.Value;
            }
            return null;
        }
        
    }
}