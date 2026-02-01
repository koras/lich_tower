using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System; 


namespace Player
{
    [System.Serializable]
    public class HighScoreEntry
    { 
        public int userId;
        public int rank;
        public string player_name;
        public int total_score;
        public int total_kills;
        public int total_damage;
        public int win_count;
        public string updated_at;
    }
}