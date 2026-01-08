using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TMPro;
using Heroes;
using UnityEngine.EventSystems;


namespace Input
{
    public class PinchToZoomAndPan : MonoBehaviour
    {
        private const string _hero_select = "Lich";

        [Header("Camera / Map")]
        public Camera targetCamera;
        
        [Header("Animals")]
        [SerializeField] private LayerMask animalMask; // слой животных (Pig, Boar и т.п.)
        
        [Header("точка куда кликнул пользователь")]
        [SerializeField] private GameObject prefabPoint;
        private GameObject _lastPointInstance;

        [Header("Zoom")]
        [SerializeField] public float zoomSpeed = 0.2f;
        [SerializeField] public float minZoom = 3f;
        [SerializeField] public float maxZoom = 10f;

        [SerializeField] private TMP_Text _textMinZoom = null;
        [SerializeField] private TMP_Text _textMaxZoom = null;
        [SerializeField] private TMP_Text _textCurrentZoom = null;

        [Header("Mouse Wheel Zoom")]
        [SerializeField] public float scrollZoomSpeed = 0.02f;

        // данные пинча
        private float _previousPinchDistance;
        private bool _wasPinching;

        // целевой зум (для плавности)
        private float _targetZoom;

        [SerializeField] private float buttonZoomStep = 1f;

        [Header("Selection")]
        [SerializeField] private LayerMask heroMask;
        [SerializeField] private LayerMask groundMask;

        [Header("Основной герой")]
        [SerializeField] private WarriorAI _selectedHero;

        [Header("Abilities")]
        private LichFireballAbility _activeFireball; // текущая способность прицеливания
        private bool _isAimingFireball;              // режим: кнопка нажата, ждём зажатия на карте
        private bool _fireballPointerDown;           // сейчас держим палец/мышь при прицеливании

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            _targetZoom = targetCamera.orthographic
                ? targetCamera.orthographicSize
                : targetCamera.fieldOfView;
            
         //   Debug.Log($"[Input] targetCamera = {targetCamera.name}, rect={targetCamera.rect}, pixelRect={targetCamera.pixelRect}");

        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Start()
        {
            UpdateMinZoomText();
            UpdateMaxZoomText();
            UpdateCurrentZoomText();
        }

        private void Update()
        {
            // только плавный зум
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, _targetZoom, Time.deltaTime * 10f);
            else
                targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, _targetZoom, Time.deltaTime * 10f);

            UpdateCurrentZoomText();
        }

        private void LateUpdate()
        {
            if (Touchscreen.current != null && Touch.activeTouches.Count > 0)
                HandleTouchInput();
            else
                HandleMouseInput();
        }

        // ==================== TOUCH INPUT ====================
        private void HandleTouchInput()
        {
            int touchCount = Touch.activeTouches.Count;

            // 2 пальца = зум (если не прицеливаемся)
            if (!_isAimingFireball && touchCount >= 2)
            {
                var t0 = Touch.activeTouches[0];
                var t1 = Touch.activeTouches[1];

                float currentDistance = Vector2.Distance(t0.screenPosition, t1.screenPosition);

                if (!_wasPinching)
                {
                    _previousPinchDistance = currentDistance;
                    _wasPinching = true;
                }
                else
                {
                    float delta = currentDistance - _previousPinchDistance;
                    _previousPinchDistance = currentDistance;

                    float zoomChange = -delta * zoomSpeed * 0.01f;
                    ApplyZoom(zoomChange);
                }

                return;
            }

            // 1 палец
            if (touchCount == 1)
            {
                var touch = Touch.activeTouches[0];
                Vector2 screenPos = touch.screenPosition;
                
                // Если палец по UI, не трогаем мир
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && IsPointerOverUI_Touch(touch.touchId))
                    return;
                
                // ===== FIREBALL AIM MODE =====
                if (_isAimingFireball && _activeFireball != null)
                {
                    // Начали держать палец -> показываем прицел
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        _fireballPointerDown = true;

                        Vector3 world;
                        if (TryGetGroundWorld(screenPos, out world))
                        {
                            _activeFireball.StartTargeting();     // спаун/показ прицела
                            _activeFireball.UpdateTarget(world);  // поставить в точку
                        }
                        return;
                    }

                    // Двигаем палец -> двигаем прицел
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && _fireballPointerDown)
                    {
                        Vector3 world;
                        if (TryGetGroundWorld(screenPos, out world))
                        {
                            _activeFireball.UpdateTarget(world);
                        }
                        return;
                    }

                    // Отпустили -> фиксируем точку и завершаем прицеливание
                    if ((touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled) && _fireballPointerDown)
                    {
                        _fireballPointerDown = false;

                        Vector3 world;
                        if (TryGetGroundWorld(screenPos, out world))
                        {
                            _activeFireball.ConfirmTarget(world); // <- тут начнётся анимация/каст по твоей логике
                            SpawnPoint(world);                    // если хочешь визуальный маркер
                        }
                        else
                        {
                            // если промазали в "землю" - просто отменяем
                            _activeFireball.CancelTargeting();
                        }

                        EndFireballTargetingMode();
                        return;
                    }

                    return;
                }

                // ===== NORMAL MODE (не прицеливаемся) =====
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (TryClickBirdAnimal(screenPos)) return;
                    if (TryClickHero(screenPos)) return; 
                    if (TryClickAnimal(screenPos)) return;
                    if (TryClickGroundForSelectedHero(screenPos)) return;
                    
                }

                _wasPinching = false;
                return;
            }

            _wasPinching = false;
        }
        private void DebugDrawCross(Vector3 world, float size, float time)
        {
            world.z = 0;
            Debug.DrawLine(world + Vector3.left * size, world + Vector3.right * size, Color.magenta, time);
            Debug.DrawLine(world + Vector3.up * size, world + Vector3.down * size, Color.magenta, time);
        }
        // ==================== MOUSE INPUT ====================
        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            // Если клик по UI, не трогаем мир
            
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var w0 = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                Debug.Log($"screen={screenPos} world(ScreenToWorldPoint)={w0}");

                DebugDrawCross(w0, 0.3f, 0.2f);
            }
            
            
            if (Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverUI_Mouse())
                return;
            
            // ===== FIREBALL AIM MODE =====
            if (_isAimingFireball && _activeFireball != null)
            {
                // Нажали -> показать прицел
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _fireballPointerDown = true;

                    if (TryGetGroundWorld(screenPos, out var world))
                    {
                        _activeFireball.StartTargeting();
                        _activeFireball.UpdateTarget(world);
                    }
                    return;
                }

                // Держим -> двигаем прицел
                if (Mouse.current.leftButton.isPressed && _fireballPointerDown)
                {
                    if (TryGetGroundWorld(screenPos, out var world))
                    {
                        _activeFireball.UpdateTarget(world);
                    }
                    return;
                }

                // Отпустили -> подтвердить
                if (Mouse.current.leftButton.wasReleasedThisFrame && _fireballPointerDown)
                {
                    _fireballPointerDown = false;

                    if (TryGetGroundWorld(screenPos, out var world))
                    {
                        _activeFireball.ConfirmTarget(world);
                        SpawnPoint(world);
                    }
                    else
                    {
                        _activeFireball.CancelTargeting();
                    }

                    EndFireballTargetingMode();
                    return;
                }

                // колесо зума можно оставить даже в прицеливании (на вкус)
            }

            // ===== NORMAL MODE =====
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (TryClickHero(screenPos)) return;
                if (TryClickBirdAnimal(screenPos)) return;
                if (TryClickAnimal(screenPos)) return;
                if (TryClickGroundForSelectedHero(screenPos)) return;
            }

            // зум колесом
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float zoomChange = -scroll * scrollZoomSpeed;
                ApplyZoom(zoomChange);
            }
        }
        private bool TryClickAnimal(Vector2 screenPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, animalMask);
            if (hit.collider == null) return false;
            
            // Если кликнули по коллайдеру дочернего объекта, берём AI у родителя
            var animal = hit.collider.GetComponent<Animals.AnimalsAI>();
            if (animal == null) return false;
            animal.Kill();
            Debug.Log($"🐗 Убили животное: {animal.name}");
            return true;
        }
        private bool TryClickBirdAnimal(Vector2 screenPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, animalMask);
            if (hit.collider == null)
            {
                return false;
            }

            // Если кликнули по коллайдеру дочернего объекта, берём AI у родителя
            var animalBird = hit.collider.GetComponent<Animals.BirdAI>();
            if (animalBird == null) {
                return false;
            }
            animalBird.Kill();
            Debug.Log($"🐗 Убили животное: {animalBird.name}");
            return true;
        }
        
        // ==================== HERO SELECT ====================
        private bool TryClickHero(Vector2 screenPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, heroMask);
            if (hit.collider == null) return false;

            var hero = hit.collider.GetComponentInParent<WarriorAI>();
            if (hero == null) return false;

            if (hero.name != _hero_select)
                return false;

            if (_selectedHero != null)
                _selectedHero.SetSelected(false);

            _selectedHero = hero;
            _selectedHero.SetSelected(true);

            Debug.Log($"Выбран герой: {_selectedHero.name}");
            return true;
        }

        // ==================== MOVE HERO ====================
        private bool TryClickGroundForSelectedHero(Vector2 screenPos)
        {
            if (_selectedHero == null) return false;

            if (!TryGetGroundWorld(screenPos, out var worldPos))
                return false;

            Vector3 targetPos;
            if (UnityEngine.AI.NavMesh.SamplePosition(worldPos, out var navHit, 1f, UnityEngine.AI.NavMesh.AllAreas))
                targetPos = navHit.position;
            else
                targetPos = worldPos;

            _selectedHero.MoveToPointManual(targetPos);
            SpawnPoint(targetPos);
            return true;
        }

        // ==================== WORLD HIT ====================
        /// <summary>
        /// Получаем world позицию по клику/тачу, только если попали в groundMask.
        /// </summary>
        private bool TryGetGroundWorld(Vector2 screenPos, out Vector3 worldPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, groundMask);
            if (hit.collider == null)
            {
                worldPos = default;
                return false;
            }

            worldPos = hit.point;
            worldPos.z = 0f;
            return true;
        }

        // ==================== ZOOM ====================
        private void ApplyZoom(float zoomChange)
        {
            _targetZoom += zoomChange;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }

        // ==================== UI BUTTONS ====================
        public void ZoomInButton() => ApplyZoom(-buttonZoomStep);
        public void ZoomOutButton() => ApplyZoom(buttonZoomStep);

        public void AddMinZoom() { minZoom += 0.1f; UpdateMinZoomText(); }
        public void SubMinZoom() { minZoom -= 0.1f; UpdateMinZoomText(); }
        public void AddMaxZoom() { maxZoom += 1.0f; UpdateMaxZoomText(); }
        public void SubMaxZoom() { maxZoom -= 1.0f; UpdateMaxZoomText(); }

        private void UpdateCurrentZoomText()
        {
            if (_textCurrentZoom == null) return;

            float zoomValue = targetCamera.orthographic ? targetCamera.orthographicSize : targetCamera.fieldOfView;
            _textCurrentZoom.text = "z " + zoomValue.ToString("0.0");
        }

        private void UpdateMaxZoomText()
        {
            if (_textMaxZoom != null)
                _textMaxZoom.text = "" + maxZoom.ToString("0.0");
        }

        private void UpdateMinZoomText()
        {
            if (_textMinZoom != null)
                _textMinZoom.text = "" + minZoom.ToString("0.0");
        }

        private void SpawnPoint(Vector3 worldPos)
        {
            if (_isAimingFireball)
            {
                Debug.Log("сейчас фаербол у лича");
                // сейчас фаербол у лича
                return;
            }

            if (prefabPoint == null) return;

            if (_lastPointInstance != null)
                Destroy(_lastPointInstance);

            worldPos.z = 0f;
            Debug.Log("Ставим точку");
            _lastPointInstance = Instantiate(prefabPoint, worldPos, Quaternion.identity);
        }

        /// <summary>
        /// UI: нажали кнопку Fireball.
        /// Мы включаем режим прицеливания, но прицел появится ТОЛЬКО когда пользователь зажмёт палец/мышь на карте.
        /// </summary>
        public void BeginFireballTargeting()
        {
            if (_selectedHero == null) return;

            var ability = _selectedHero.GetComponent<LichFireballAbility>();
            if (ability == null)
            {
                Debug.Log("Этот герой не Лич, фаербол недоступен.");
                return;
            }

            if (!ability.CanStart())
            {
                Debug.Log("Недостаточно маны на фаербол.");
                return;
            }

            _activeFireball = ability;
            _isAimingFireball = true;
            _fireballPointerDown = false;

            Debug.Log("Fireball mode ON: ждём зажатия на карте (press & hold).");
        }
        
        private bool IsPointerOverUI_Mouse()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool IsPointerOverUI_Touch(int touchId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
        }
        
        private void EndFireballTargetingMode()
        {
            _isAimingFireball = false;
            _fireballPointerDown = false;
            _activeFireball = null;
            Debug.Log("Fireball mode OFF.");
        }
        
    }
}
