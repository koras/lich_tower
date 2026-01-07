using System;
using System.Collections.Generic;
using UnityEngine;
using Level; // чтобы видеть SpawnPlatform.State



namespace Config
{
    [Serializable]
    public class HeroCostEntry
    {
        public SpawnInHero.State Hero;
        public int Cost;
    }

    [Serializable]
    public class HeroCostData
    {
        public List<HeroCostEntry> Entries = new List<HeroCostEntry>();
    }
}