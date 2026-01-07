using UnityEngine; 
using Level;
[CreateAssetMenu(fileName = "HeroCosts", menuName = "Scriptable Objects/HeroCosts")]
public class HeroCosts : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public SpawnInHero.State State;
        [Min(0)] public int Cost;
    }

    public Entry[] Entries;

    public int GetCost(SpawnInHero.State state, int fallback = 0)
    {
        foreach (var e in Entries)
            if (e.State == state) return e.Cost;
        return fallback;
    }
}
