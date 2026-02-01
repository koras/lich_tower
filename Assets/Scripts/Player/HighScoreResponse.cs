using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System; 



namespace Player
{
    [System.Serializable]
    public class HighScoreResponse
    {
        public bool success;
        public string message;
        public List<HighScoreEntry> data;
    }
}