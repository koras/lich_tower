using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Heroes;

namespace Player
{
    public class GameStatsCollector : MonoBehaviour
    {
        public static GameStatsCollector I { get; private set; }

        [SerializeField] private int myTeam = 1; // у тебя _team=1, можно прокинуть из BaseManager

        private bool _subscribed;

        // агрегаты
        public int TotalDamageDone { get; private set; }
        public int TotalKills { get; private set; }

        // по типам
        private readonly Dictionary<HeroesBase.Hero, int> _killsByHero = new();
        private readonly Dictionary<HeroesBase.Hero, int> _damageToHero = new();

        // чтобы не считать дважды смерть/килл из-за повторных событий
        private readonly HashSet<int> _alreadyCountedDeaths = new();

        private void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SubscribeAll();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
        }

        public void ResetStats()
        {
            TotalDamageDone = 0;
            TotalKills = 0;
            _killsByHero.Clear();
            _damageToHero.Clear();
            _alreadyCountedDeaths.Clear();
        }

        public void SetMyTeam(int team) => myTeam = team;

        public void SubscribeAll()
        {
            if (_subscribed) return;

            var all = FindObjectsOfType<HeroesBase>(includeInactive: true);
            foreach (var hb in all)
                Hook(hb);

            _subscribed = true;
        }

        public void UnsubscribeAll()
        {
            if (!_subscribed) return;

            var all = FindObjectsOfType<HeroesBase>(includeInactive: true);
            foreach (var hb in all)
                Unhook(hb);

            _subscribed = false;
        }

        private void Hook(HeroesBase hb)
        {
            if (hb == null) return;
            hb.OnDamage += OnAnyDamage;
            hb.OnKilled += OnAnyKilled;
        }

        private void Unhook(HeroesBase hb)
        {
            if (hb == null) return;
            hb.OnDamage -= OnAnyDamage;
            hb.OnKilled -= OnAnyKilled;
        }

        private void OnAnyDamage( HeroesBase victim, int dmg)
        {
            if (dmg <= 0) return;

            


            

            TotalDamageDone += dmg;

            var victimType = victim.GetHeroType();
            _damageToHero.TryGetValue(victimType, out var cur);
            _damageToHero[victimType] = cur + dmg;
        }

        private void OnAnyKilled( HeroesBase victim)
        {
            if (victim == null) return;

            // защита от двойного килла
            int id = victim.gameObject.GetInstanceID();
            if (_alreadyCountedDeaths.Contains(id)) return;
            _alreadyCountedDeaths.Add(id);
            TotalKills++;

            var victimType = victim.GetHeroType();
            _killsByHero.TryGetValue(victimType, out var cur);
            _killsByHero[victimType] = cur + 1;
        }

        // Собираем JSON-совместимый объект/строку
        public StatsPayload BuildPayload(bool isWin, int sessionId)
        {
            var p = new StatsPayload
            {
                session_id = sessionId,
                team = myTeam,
                is_win = isWin,
                total_damage = TotalDamageDone,
                total_kills = TotalKills,
                damage_by_hero = new List<KeyInt>(),
                kills_by_hero = new List<KeyInt>(),
                timestamp_utc = DateTime.UtcNow.ToString("o")
            };

            foreach (var kv in _damageToHero)
                p.damage_by_hero.Add(new KeyInt(kv.Key.ToString(), kv.Value));

            foreach (var kv in _killsByHero)
                p.kills_by_hero.Add(new KeyInt(kv.Key.ToString(), kv.Value));

            // стабильный canonical string для хэша
            string canonical = p.ToCanonicalString();
            p.hash = HashUtils.Sha256Hex(canonical);

            return p;
        }
    }

    [Serializable]
    public class StatsPayload
    {
        public int session_id;
        public int team;
        public bool is_win;
        public int total_damage;
        public int total_kills;
        public List<KeyInt> damage_by_hero;
        public List<KeyInt> kills_by_hero;
        public string timestamp_utc;

        public string hash; // sha256 от canonical

        public string ToCanonicalString()
        {
            // важно: строгий порядок, чтобы хэш совпадал на сервере
            var sb = new StringBuilder();
            sb.Append("session_id=").Append(session_id).Append("|");
            sb.Append("team=").Append(team).Append("|");
            sb.Append("is_win=").Append(is_win ? "1" : "0").Append("|");
            sb.Append("total_damage=").Append(total_damage).Append("|");
            sb.Append("total_kills=").Append(total_kills).Append("|");
            sb.Append("timestamp_utc=").Append(timestamp_utc).Append("|");

            sb.Append("damage_by_hero=");
            damage_by_hero.Sort((a,b) => string.CompareOrdinal(a.key, b.key));
            for (int i = 0; i < damage_by_hero.Count; i++)
                sb.Append(damage_by_hero[i].key).Append(":").Append(damage_by_hero[i].value).Append(",");

            sb.Append("|kills_by_hero=");
            kills_by_hero.Sort((a,b) => string.CompareOrdinal(a.key, b.key));
            for (int i = 0; i < kills_by_hero.Count; i++)
                sb.Append(kills_by_hero[i].key).Append(":").Append(kills_by_hero[i].value).Append(",");

            return sb.ToString();
        }
    }

    [Serializable]
    public class KeyInt
    {
        public string key;
        public int value;
        public KeyInt(string k, int v) { key = k; value = v; }
    }

    public static class HashUtils
    {
        public static string Sha256Hex(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}