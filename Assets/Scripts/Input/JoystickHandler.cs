using UnityEngine;
using UnityEngine.EventSystems;

namespace Heroes
{
    public class JoystickHandler : MonoBehaviour
    {
        public System.Action<Vector2> OnJoystickInputChanged;
        
        private FloatingJoystick _joystick;
        
        private void Start()
        {
            _joystick = GetComponent<FloatingJoystick>();
            if (_joystick == null)
                Debug.LogError("JoystickHandler требует компонент FloatingJoystick!");
        }
        
        private void Update()
        {
            if (_joystick != null && OnJoystickInputChanged != null)
            {
                OnJoystickInputChanged(_joystick.Direction);
            }
        }
    }
}