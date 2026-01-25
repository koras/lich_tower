using UnityEngine;
using UnityEngine.SceneManagement;
using Level.Loading;

namespace Level.Loading
{
    public static class SceneTransition
    {
        [SerializeField]  public  static string TargetScene;  
        public static void Load(string targetScene)
        {
            
            AudioSystem.MusicService.I?.Stop();
            Debug.Log($"[SceneTransition] nextSceneName: {TargetScene}");
            SceneLoader.TargetScene = targetScene;
            SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
        }
    }
}