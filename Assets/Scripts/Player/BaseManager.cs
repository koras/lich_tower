using System.IO;
using UnityEngine;

namespace Player
{
    public class BaseManager : MonoBehaviour
    {  
        public static BaseManager Instance { get; private set; }
        
        [Header("Команда")]
        [SerializeField] protected int _team = 1; 

        
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }
            Instance = this;

        }

        
        
        public int GetMyTeam() 
        {
            return _team;
        }
        
        public int GetRandomNumber(int maxExclusive)
        {
            return 500;
            return Random.Range(0, maxExclusive);
        }
    }
}