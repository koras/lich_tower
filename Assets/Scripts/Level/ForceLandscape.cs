using UnityEngine;

namespace Level
{
    public class ForceLandscape : MonoBehaviour
    {
        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft; // или LandscapeRight
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = false; // если хочешь только Left
        }
    }
}