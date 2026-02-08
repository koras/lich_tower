using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TMPro;
using Heroes;
using UnityEngine.EventSystems;
using System.Collections.Generic;


namespace Input
{
    public class PinchToZoomAndPan : MonoBehaviour
    {
        private const string _hero_select = "Lich";

        [Header("Camera / Map")] public Camera targetCamera;

        [Header("Animals")] [SerializeField] private LayerMask animalMask; // слой животных (Pig, Boar и т.п.)

        [Header("точка куда кликнул пользователь")] [SerializeField]
        private GameObject prefabPoint;

        private GameObject _lastPointInstance;

        [Header("Zoom")] [SerializeField] public float zoomSpeed = 0.2f;
        [SerializeField] public float minZoom = 3f;
        [SerializeField] public float maxZoom = 10f;

        [SerializeField] private TMP_Text _textMinZoom = null;
        [SerializeField] private TMP_Text _textMaxZoom = null;
        [SerializeField] private TMP_Text _textCurrentZoom = null;

        [Header("Mouse Wheel Zoom")] [SerializeField]
        public float scrollZoomSpeed = 0.02f;

        // данные пинча
        private float _previousPinchDistance;
        private bool _wasPinching;

        // целевой зум (для плавности)
        private float _targetZoom;

        [SerializeField] private float buttonZoomStep = 1f;

        [Header("Selection")] [SerializeField] private LayerMask heroMask;
        [SerializeField] private LayerMask groundMask;

        [Header("Основной герой")] [SerializeField]
        private WarriorAI _selectedHero;

        [Header("Abilities")] private LichFireballAbility _activeFireball; // текущая способность прицеливания
        private bool _isAimingFireball; // режим: кнопка нажата, ждём зажатия на карте
        private bool _fireballPointerDown; // сейчас держим палец/мышь при прицеливании


        private Vector2 _lastFireballScreenPos; // НОВОЕ: запоминаем позицию
        private bool _isImmediateFireballMode = false;


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
        }

        private void Update()
        {
            // только плавный зум
            if (targetCamera.orthographic)
                targetCamera.orthographicSize =
                    Mathf.Lerp(targetCamera.orthographicSize, _targetZoom, Time.deltaTime * 10f);
            else
                targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, _targetZoom, Time.deltaTime * 10f);
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
            }

            // 1 палец
            if (touchCount == 1)
            {
                var touch = Touch.activeTouches[0];
                Vector2 screenPos = touch.screenPosition;

                // Получаем информацию о клике по UI
                GameObject clickedUIObject = null;
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    clickedUIObject = GetClickedUIObject(touch.touchId, screenPos);

                    // Если кликнули по UI
                    if (clickedUIObject != null)
                    {
                        // Логируем информацию о клике
                        Debug.Log($"Клик по UI: {clickedUIObject.name}, " +
                                  $"Позиция: {screenPos}, " +
                                  $"Координаты: ({screenPos.x}, {screenPos.y})");

                        // Проверяем, какая именно кнопка
                        if (IsClickOnButton(clickedUIObject, "Fireball"))
                        {
                            Debug.Log($"Клик по кнопке Fireball! Координаты: {screenPos}");
                            // Здесь можно вызвать обработку клика по кнопке фаербола
                            // или просто отметить, что клик был по этой кнопке
                        }
                        else if (IsClickOnButton(clickedUIObject, "Attack"))
                        {
                            Debug.Log($"Клик по кнопке Attack! Координаты: {screenPos}");
                        }
                        else if (IsClickOnButton(clickedUIObject, "Move"))
                        {
                            Debug.Log($"Клик по кнопке Move! Координаты: {screenPos}");
                        }

                        if (!(_isAimingFireball && touch.touchId == _fireballPointerId))
                            return;  
                    }
                }

                // Если палец по UI, не трогаем мир
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began
                    && IsPointerOverUI_Touch(touch.touchId)
                    && !(_isAimingFireball && touch.touchId == _fireballPointerId))
                {
                    return;
                }


                // ===== FIREBALL AIM MODE =====
                if (_isAimingFireball && _activeFireball != null)
                {
                    // если пальцев нет — выходим
                    if (Touch.activeTouches.Count == 0)
                    {
                        _activeFireball.CancelTargeting();
                        EndFireballTargetingMode();
                        return;
                    }

                    // берём первый палец (в режиме прицеливания он у тебя один)
                    var t = Touch.activeTouches[0];
                    Vector2 pos = t.screenPosition;

                    // пока держим — обновляем прицел (Moved/Stationary)
                    if (_fireballPointerDown &&
                        (t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                         t.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
                    {
                        if (TryGetGroundWorld(pos, out var world))
                        {
                            _selectedHero.GetComponent<HeroesBase>().SpendManna(100);
                            _activeFireball.UpdateTarget(world);
                        }
 

                        return;
                    }

                    // отпустили — кастуем
                    if (_fireballPointerDown &&
                        (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         t.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
                    {
                        _fireballPointerDown = false;

                        if (TryGetGroundWorld(pos, out var world))
                            _activeFireball.ConfirmTarget(world);
                        else
                            _activeFireball.CancelTargeting();

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

        private GameObject GetClickedUIObject(int touchId, Vector2 screenPosition)
        {
            if (EventSystem.current == null) return null;

            // Создаем PointerEventData
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                pointerId = touchId
            };

            // Делаем рейкаст
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            // Возвращаем первый UI объект
            if (results.Count > 0)
            {
                return results[0].gameObject;
            }

            return null;
        }
        
        
        
        // добавь поля
        private int _fireballPointerId = int.MinValue;
        private System.Action _onFireballAimingFinished;
// новый метод: старт из UI на PointerDown
        private bool _fireballTargetShown;

        public void BeginFireballTargetingFromUIButton(int pointerId, Vector2 screenPos, System.Action onFinished)
        {
            Debug.Log($"[PinchToZoomAndPan] BeginFireballTargetingFromUIButton pointerId={pointerId} pos={screenPos}");

            if (_selectedHero == null) { onFinished?.Invoke(); return; }

            var ability = _selectedHero.GetComponent<LichFireballAbility>();
            if (ability == null) { onFinished?.Invoke(); return; }

            _activeFireball = ability;
            _isAimingFireball = true;
            _fireballPointerDown = true;
            _onFireballAimingFinished = onFinished;

            _activeFireball.StartTargeting();

            // попробуем сразу поставить прицел по месту нажатия
            if (TryGetGroundWorld(screenPos, out var world))
                _activeFireball.UpdateTarget(world);

            Debug.Log("[PinchToZoomAndPan] Fireball mode ON (single touch tracking).");
        }
        // НОВЫЙ метод: проверяет, кликнули ли по конкретной кнопке
        private bool IsClickOnButton(GameObject uiObject, string buttonName)
        {
            if (uiObject == null) return false;
            return uiObject.name.Contains(buttonName);
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

                //    DebugDrawCross(w0, 0.3f, 0.2f);
            }


            if (Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverUI_Mouse() && !_isAimingFireball)
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
                        Debug.Log($"Идём 1");
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
            // выбор игрока
            // _selectedHero.MoveToPointManual(targetPos);

            Debug.Log($"[PinchToZoomAndPan] Идём 2");
            //  SpawnPoint(targetPos);
            return true;
        }

 

 

        /// <summary>
        /// UI: нажали кнопку Fireball.
        /// Мы включаем режим прицеливания, но прицел появится ТОЛЬКО когда пользователь зажмёт палец/мышь на карте.
        /// </summary> 
        public void BeginFireballTargeting()
        {
            Debug.Log("[PinchToZoomAndPan] BeginFireballTargeting");
            if (_selectedHero == null) return;

            var ability = _selectedHero.GetComponent<LichFireballAbility>();
            if (ability == null)
            {
                Debug.Log("[PinchToZoomAndPan] Этот герой не Лич, фаербол недоступен.");
                return;
            }

            //   if (!ability.CanStart())
            //    {
            //        Debug.Log("Недостаточно маны на фаербол.");
            //        return;
            //     }

            _activeFireball = ability;
            _isAimingFireball = true;
            _fireballPointerDown = false;

            //         if (_isAimingFireball && _activeFireball != null)

            Debug.Log("[PinchToZoomAndPan] Fireball mode ON: ждём зажатия на карте (press & hold).");
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

            _fireballPointerId = int.MinValue;

            var cb = _onFireballAimingFinished;
            _onFireballAimingFinished = null;
            cb?.Invoke();
 
            Debug.Log("[PinchToZoomAndPan] Fireball mode OFF.");
        }
        //
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
        private void SpawnPoint(Vector3 worldPos)
        {
            if (_isAimingFireball)
            {
                Debug.Log("[PinchToZoomAndPan] сейчас фаербол у лича");
                // сейчас фаербол у лича
                return;
            }

            if (prefabPoint == null) return;

            if (_lastPointInstance != null)
                Destroy(_lastPointInstance);

            worldPos.z = 0f;
            Debug.Log("[PinchToZoomAndPan] Ставим точку");
            _lastPointInstance = Instantiate(prefabPoint, worldPos, Quaternion.identity);
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
            Debug.Log($"[PinchToZoomAndPan] 🐗 Убили животное: {animal.name}");
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
            if (animalBird == null)
            {
                return false;
            }

            animalBird.Kill();
            Debug.Log($"[PinchToZoomAndPan] 🐗 Убили животное: {animalBird.name}");
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

            Debug.Log($"[PinchToZoomAndPan] Выбран герой: {_selectedHero.name}");
            return true;
        }
    }
}