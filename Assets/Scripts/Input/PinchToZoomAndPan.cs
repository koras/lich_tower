using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TMPro;
using Heroes;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text; // Для StringBuilder в логировании

namespace Input
{
    public class PinchToZoomAndPan : MonoBehaviour
    {
        // ==================== КОНСТАНТЫ ====================
        private const string _hero_select = "Lich"; // Имя выбираемого героя
        private const string LOG_PREFIX = "[PinchToZoomAndPan] "; // Префикс для логов

        // ==================== КОМПОНЕНТЫ КАМЕРЫ ====================
        [Header("Camera / Map")] 
        public Camera targetCamera; // Целевая камера для управления

        // ==================== ЖИВОТНЫЕ ====================
        [Header("Animals")] 
        [SerializeField] private LayerMask animalMask; // Слой животных (Pig, Boar и т.п.)
        [Tooltip("Включить для отладки кликов по животным")]
        [SerializeField] private bool debugAnimalClicks = true;

        // ==================== ТОЧКИ НА КАРТЕ ====================
        [Header("точка куда кликнул пользователь")] 
        [SerializeField] private GameObject prefabPoint; // Префаб точки для визуализации клика
        private GameObject _lastPointInstance; // Последний созданный экземпляр точки
        [Tooltip("Включить для отладки точек на карте")]
        [SerializeField] private bool debugSpawnPoint = true;

 
 

        // Данные пинча
        private float _previousPinchDistance; // Предыдущее расстояние между пальцами


        // Целевой зум (для плавности)
        private float _targetZoom;

        [SerializeField] private float buttonZoomStep = 1f; // Шаг зума при нажатии кнопок

        // ==================== ВЫБОР ГЕРОЯ ====================
        [Header("Selection")] 
        [SerializeField] private LayerMask heroMask; // Слой героев
        [SerializeField] private LayerMask groundMask; // Слой земли

        [Header("Основной герой")] 
        [SerializeField] private WarriorAI _selectedHero; // Выбранный герой
        [Tooltip("Включить для отладки выбора героя")]
        [SerializeField] private bool debugHeroSelection = true;

        // ==================== СПОСОБНОСТИ ====================
        [Header("Abilities")] 
        private LichFireballAbility _activeFireball; // Текущая активная способность фаербола
        private bool _isAimingFireball; // Флаг: режим прицеливания фаербола активен
        private bool _fireballPointerDown; // Флаг: палец/мышь зажаты в режиме прицеливания
        private Vector2 _lastFireballScreenPos; // Последняя позиция экрана для фаербола

        private int _fireballPointerId = int.MinValue; // ID пальца для фаербола
        private System.Action _onFireballAimingFinished; // Колбэк при завершении прицеливания
        private bool _fireballTargetShown; // Флаг: показана ли цель фаербола
        [Tooltip("Включить для отладки фаербола")]
        [SerializeField] private bool debugFireball = true;

        // ==================== ЗОНЫ ДЖОЙСТИКОВ ====================
        [Header("Joystick Zones")] 
        [SerializeField] private RectTransform leftJoystickZone; // Зона левого джойстика
        [SerializeField] private RectTransform rightJoystickZone; // Зона правого джойстика
        [Tooltip("Включить для отладки джойстиков")]
        [SerializeField] private bool debugJoysticks = true;

        // ID пальцев, захвативших джойстики
        private int _leftStickFinger = int.MinValue;
        private int _rightStickFinger = int.MinValue;

        // ==================== ОБЩИЕ НАСТРОЙКИ ОТЛАДКИ ====================
        [Header("Debug Settings")]
        [Tooltip("Уровень детализации логов")]
        [SerializeField] private LogLevel logLevel = LogLevel.Info;
        private enum LogLevel { None, Error, Warning, Info, Verbose }

        // Счетчики для производительности (опционально)
        private int _touchProcessedCount = 0;
        private float _lastLogTime = 0f;

        // ==================== МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА ====================

        private void Awake()
        {
            Log("Awake() - инициализация компонента", LogLevel.Info);
            
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                Log($"targetCamera не назначена, использую Camera.main: {targetCamera?.name}", LogLevel.Warning);
            }

            if (targetCamera != null)
            {
                _targetZoom = targetCamera.orthographic
                    ? targetCamera.orthographicSize
                    : targetCamera.fieldOfView;
                
                Log($"targetCamera = {targetCamera.name}, режим: {(targetCamera.orthographic ? "Orthographic" : "Perspective")}, начальный зум: {_targetZoom}", LogLevel.Info);
            }
            else
            {
                LogError("Camera.main не найдена! Компонент не будет работать корректно.");
            }

            // Проверка необходимых компонентов
            ValidateComponents();
        }

        /// <summary>
        /// Проверяет наличие всех необходимых компонентов и выводит предупреждения
        /// </summary>
        private void ValidateComponents()
        {
            if (prefabPoint == null)
                LogWarning("prefabPoint не назначен - точки кликов не будут отображаться");
            
            if (animalMask == 0)
                LogWarning("animalMask не назначен - клики по животным не будут работать");
            
            if (heroMask == 0)
                LogWarning("heroMask не назначен - выбор героев не будет работать");
            
            if (groundMask == 0)
                LogWarning("groundMask не назначен - клики по земле не будут работать");
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Log("EnhancedTouchSupport включен", LogLevel.Verbose);
        }
        
        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            Log("EnhancedTouchSupport отключен", LogLevel.Verbose);
        }

        private void Update()
        {
            // Плавное применение зума
            if (targetCamera != null)
            {
                if (targetCamera.orthographic)
                {
                    float oldSize = targetCamera.orthographicSize;
                    targetCamera.orthographicSize = Mathf.Lerp(oldSize, _targetZoom, Time.deltaTime * 10f);
                    
                    if (Mathf.Abs(oldSize - targetCamera.orthographicSize) > 0.01f)
                        Log($"Плавный зум: {oldSize:F2} -> {targetCamera.orthographicSize:F2}", LogLevel.Verbose);
                }
                else
                {
                    float oldFOV = targetCamera.fieldOfView;
                    targetCamera.fieldOfView = Mathf.Lerp(oldFOV, _targetZoom, Time.deltaTime * 10f);
                    
                    if (Mathf.Abs(oldFOV - targetCamera.fieldOfView) > 0.01f)
                        Log($"Плавный зум: {oldFOV:F2} -> {targetCamera.fieldOfView:F2}", LogLevel.Verbose);
                }

 
            }
        }
 

        private void LateUpdate()
        {
            // Определяем тип ввода (тач или мышь)
            if (Touchscreen.current != null && Touch.activeTouches.Count > 0)
            {
                if (logLevel == LogLevel.Verbose)
                    Log($"Обработка тач-ввода: {Touch.activeTouches.Count} активных касаний", LogLevel.Verbose);
                
                HandleTouchInput();
            }
            else
            {
                if (logLevel == LogLevel.Verbose && Mouse.current != null)
                    Log("Обработка мыши", LogLevel.Verbose);
                
                HandleMouseInput();
            }
        }

        // ==================== ОБРАБОТКА ТАЧ-ВВОДА ====================

        /// <summary>
        /// Обрабатывает все касания экрана
        /// </summary>
        private void HandleTouchInput()
        {
            int touchCount = Touch.activeTouches.Count;
            _touchProcessedCount++;

            // Логируем количество касаний раз в секунду
            if (Time.time - _lastLogTime > 1f && logLevel == LogLevel.Verbose)
            {
                Log($"Активных касаний: {touchCount}", LogLevel.Verbose);
                _lastLogTime = Time.time;
            }

            // 1) Сначала распределяем новые касания по джойстикам
            foreach (var t in Touch.activeTouches)
            {
                if (t.phase != UnityEngine.InputSystem.TouchPhase.Began)
                    continue;

                Vector2 pos = t.screenPosition;
                Log($"Новое касание ID:{t.touchId}, фаза:Began, позиция:{pos}", LogLevel.Verbose);

                // Левый джойстик
                if (_leftStickFinger == int.MinValue && IsInRect(leftJoystickZone, pos))
                {
                    _leftStickFinger = t.touchId;
                    Log($"Левый джойстик захвачен пальцем ID:{t.touchId}, позиция:{pos}", LogLevel.Info);
                    continue;
                }

                // Правый джойстик
                if (_rightStickFinger == int.MinValue && IsInRect(rightJoystickZone, pos))
                {
                    _rightStickFinger = t.touchId;
                    Log($"Правый джойстик захвачен пальцем ID:{t.touchId}, позиция:{pos}", LogLevel.Info);
                    continue;
                }
            }

            // 2) Освобождение пальцев джойстиков
            foreach (var t in Touch.activeTouches)
            {
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    if (t.touchId == _leftStickFinger)
                    {
                        _leftStickFinger = int.MinValue;
                        Log($"Левый джойстик освобожден (палец ID:{t.touchId})", LogLevel.Info);
                    }
                    if (t.touchId == _rightStickFinger)
                    {
                        _rightStickFinger = int.MinValue;
                        Log($"Правый джойстик освобожден (палец ID:{t.touchId})", LogLevel.Info);
                    }
                }
            }

            // 3) Обработка мультитач для зума (пинч)
            // if (touchCount >= 2)
            // {
            //     HandlePinchZoom();
            // }

            // 4) Обработка каждого пальца, не принадлежащего джойстикам
            foreach (var touch in Touch.activeTouches)
            {
                // Пропускаем пальцы, захваченные джойстиками
                if (touch.touchId == _leftStickFinger || touch.touchId == _rightStickFinger)
                    continue;

                HandleSingleTouch(touch);
            }


        }

        /// <summary>
        /// Проверяет, находится ли точка внутри RectTransform
        /// </summary>
        private bool IsInRect(RectTransform rect, Vector2 screenPos)
        {
            if (rect == null)
            {
                LogWarning("IsInRect: rect не назначен");
                return false;
            }
            
            bool result = RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
            
            if (logLevel == LogLevel.Verbose && result)
                Log($"Точка {screenPos} находится в зоне {rect.name}", LogLevel.Verbose);
            
            return result;
        }
 

        /// <summary>
        /// Обрабатывает одно касание (не джойстик)
        /// </summary>
        private void HandleSingleTouch(Touch touch)
        {
            Vector2 screenPos = touch.screenPosition;
            
            Log($"Обработка касания ID:{touch.touchId}, фаза:{touch.phase}, позиция:{screenPos}", LogLevel.Verbose);

            // ===== Проверка клика по UI =====
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                var clickedUIObject = GetClickedUIObject(touch.touchId, screenPos);

                if (clickedUIObject != null)
                {
                    Log($"Клик по UI: {clickedUIObject.name}, позиция:{screenPos}", LogLevel.Info);

                    // Если это не режим фаербола или палец не тот - выходим
                    if (!(_isAimingFireball && touch.touchId == _fireballPointerId))
                    {
                        Log($"Клик по UI обработан, выход из обработки касания", LogLevel.Verbose);
                        return;
                    }
                }

                // Если палец по UI, не трогаем мир (кроме режима фаербола)
                if (IsPointerOverUI_Touch(touch.touchId) &&
                    !(_isAimingFireball && touch.touchId == _fireballPointerId))
                {
                    Log($"Касание ID:{touch.touchId} перехвачено UI, игнорируем", LogLevel.Verbose);
                    return;
                }
            }

            // ===== РЕЖИМ ПРИЦЕЛИВАНИЯ ФАЕРБОЛА =====
            if (_isAimingFireball && _activeFireball != null)
            {
                HandleFireballTouch(touch);
                return;
            }

            // ===== ОБЫЧНЫЙ РЕЖИМ =====
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Log($"Обычный клик в позиции {screenPos}", LogLevel.Info);
                
                if (TryClickBirdAnimal(screenPos)) return;
          
                if (TryClickAnimal(screenPos)) return;
              //  if (TryClickGroundForSelectedHero(screenPos)) return;
                
                Log("Клик не обработан ни одним из обработчиков", LogLevel.Verbose);
            }
        }

        /// <summary>
        /// Обрабатывает касания в режиме фаербола
        /// </summary>
        private void HandleFireballTouch(Touch touch)
        {
            Vector2 screenPos = touch.screenPosition;
            
            Log($"Режим фаербола: обработка касания ID:{touch.touchId}, фаза:{touch.phase}", LogLevel.Verbose);

            // Нажатие - начало прицеливания
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _fireballPointerDown = true;
                _fireballPointerId = touch.touchId;
                
                Log($"Фаербол: начало прицеливания, палец ID:{touch.touchId}", LogLevel.Info);

                if (TryGetGroundWorld(screenPos, out var world))
                {
                    Log($"Фаербол: начальная позиция в мире: {world}", LogLevel.Info);
                    _activeFireball.StartTargeting();
                    _activeFireball.UpdateTarget(world);
                }
                else
                {
                    LogWarning($"Фаербол: не удалось определить позицию на земле из {screenPos}");
                }
                
                return;
            }

            // Движение - обновление прицела
            if (_fireballPointerDown &&
                (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
            {
                if (TryGetGroundWorld(screenPos, out var world))
                {
                    Log($"Фаербол: обновление прицела, новая позиция: {world}", LogLevel.Verbose);
                    _activeFireball.UpdateTarget(world);
                }
                
                return;
            }

            // Отпускание - подтверждение цели
            if (_fireballPointerDown &&
                (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                 touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
            {
                _fireballPointerDown = false;
                
                Log($"Фаербол: завершение прицеливания, палец ID:{touch.touchId} отпущен", LogLevel.Info);

                if (TryGetGroundWorld(screenPos, out var world))
                {
                    Log($"Фаербол: подтверждение цели в {world}", LogLevel.Info);
                    _activeFireball.ConfirmTarget(world);
                    //SpawnPoint(world);
                }
                else
                {
                    LogWarning($"Фаербол: не удалось определить позицию на земле, отмена цели");
                    _activeFireball.CancelTargeting();
                }

                EndFireballTargetingMode();
            }
        }

        // ==================== ОБРАБОТКА МЫШИ ====================

        /// <summary>
        /// Обрабатывает ввод с мыши
        /// </summary>
        private void HandleMouseInput()
        {
            if (Mouse.current == null)
            {
                LogWarning("Mouse.current == null, обработка мыши невозможна");
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();

            // Отладка позиции мыши в мире
            if (Mouse.current.leftButton.wasPressedThisFrame && logLevel == LogLevel.Verbose)
            {
                var worldPos = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                Log($"Клик мыши: экран={screenPos}, мир={worldPos}", LogLevel.Verbose);
            }

            // Проверка наведения на UI
            if (Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverUI_Mouse() && !_isAimingFireball)
            {
                Log("Клик мыши перехвачен UI, игнорируем", LogLevel.Verbose);
                return;
            }

            // ===== РЕЖИМ ПРИЦЕЛИВАНИЯ ФАЕРБОЛА =====
            if (_isAimingFireball && _activeFireball != null)
            {
                HandleFireballMouse(screenPos);
                return;
            }

            // ===== ОБЫЧНЫЙ РЕЖИМ =====
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Log($"Обычный клик мыши в позиции {screenPos}", LogLevel.Info);
                
               // if (TryClickHero(screenPos)) return;
                if (TryClickBirdAnimal(screenPos)) return;
                if (TryClickAnimal(screenPos)) return;
                if (TryClickGroundForSelectedHero(screenPos)) return;
                
                Log("Клик мыши не обработан ни одним из обработчиков", LogLevel.Verbose);
            }

 
        }

        /// <summary>
        /// Обрабатывает мышь в режиме фаербола
        /// </summary>
        private void HandleFireballMouse(Vector2 screenPos)
        {
            Log($"Режим фаербола (мышь): обработка", LogLevel.Verbose);

            // Нажатие - начало прицеливания
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _fireballPointerDown = true;
                Log("Фаербол (мышь): начало прицеливания", LogLevel.Info);

                if (TryGetGroundWorld(screenPos, out var world))
                {
                    Log($"Фаербол (мышь): начальная позиция: {world}", LogLevel.Info);
                    _activeFireball.StartTargeting();
                    _activeFireball.UpdateTarget(world);
                }

                return;
            }

            // Движение с зажатой кнопкой - обновление прицела
            if (Mouse.current.leftButton.isPressed && _fireballPointerDown)
            {
                if (TryGetGroundWorld(screenPos, out var world))
                {
                    Log($"Фаербол (мышь): обновление прицела, позиция: {world}", LogLevel.Verbose);
                    _activeFireball.UpdateTarget(world);
                }

                return;
            }

            // Отпускание - подтверждение цели
            if (Mouse.current.leftButton.wasReleasedThisFrame && _fireballPointerDown)
            {
                _fireballPointerDown = false;
                Log("Фаербол (мышь): завершение прицеливания", LogLevel.Info);

                if (TryGetGroundWorld(screenPos, out var world))
                {
                    Log($"Фаербол (мышь): подтверждение цели в {world}", LogLevel.Info);
                    _activeFireball.ConfirmTarget(world);
                 //   SpawnPoint(world);
                }
                else
                {
                    LogWarning("Фаербол (мышь): не удалось определить позицию на земле, отмена цели");
                    _activeFireball.CancelTargeting();
                }

                EndFireballTargetingMode();
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ UI ====================

        /// <summary>
        /// Получает UI объект, на который было выполнено нажатие
        /// </summary>
        private GameObject GetClickedUIObject(int touchId, Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                LogWarning("EventSystem.current == null, невозможно определить клик по UI");
                return null;
            }

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
                Log($"Найден UI объект: {results[0].gameObject.name}, иерархия: {GetGameObjectPath(results[0].gameObject)}", LogLevel.Verbose);
                return results[0].gameObject;
            }

            return null;
        }

        /// <summary>
        /// Получает полный путь к объекту в иерархии
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        /// <summary>
        /// Проверяет, находится ли мышь над UI элементом
        /// </summary>
        private bool IsPointerOverUI_Mouse()
        {
            bool result = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            
            if (result && logLevel == LogLevel.Verbose)
                Log("Мышь над UI", LogLevel.Verbose);
            
            return result;
        }

        /// <summary>
        /// Проверяет, находится ли касание над UI элементом
        /// </summary>
        private bool IsPointerOverUI_Touch(int touchId)
        {
            bool result = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
            
            if (result && logLevel == LogLevel.Verbose)
                Log($"Касание ID:{touchId} над UI", LogLevel.Verbose);
            
            return result;
        }

        // ==================== МЕТОДЫ ФАЕРБОЛА ====================

        /// <summary>
        /// Начинает режим прицеливания фаербола (вызывается из UI)
        /// </summary>
        public void BeginFireballTargeting()
        {
            Log("BeginFireballTargeting() - попытка активации режима фаербола", LogLevel.Info);
            
            if (_selectedHero == null)
            {
                LogWarning("Невозможно активировать фаербол: не выбран герой");
                return;
            }

            var ability = _selectedHero.GetComponent<LichFireballAbility>();
            if (ability == null)
            {
                LogWarning($"Герой {_selectedHero.name} не имеет способности LichFireballAbility");
                return;
            }

            _activeFireball = ability;
            _isAimingFireball = true;
            _fireballPointerDown = false;

            Log($"Режим фаербола активирован для героя {_selectedHero.name}. Ожидание нажатия на карте.", LogLevel.Info);
        }

        /// <summary>
        /// Начинает режим прицеливания фаербола с указанного пальца (для UI)
        /// </summary>
        public void BeginFireballTargetingFromUIButton(int pointerId, Vector2 screenPos, System.Action onFinished)
        {
            Log($"BeginFireballTargetingFromUIButton() - pointerId:{pointerId}, pos:{screenPos}", LogLevel.Info);

            if (_selectedHero == null)
            {
                LogWarning("Невозможно активировать фаербол: не выбран герой");
                onFinished?.Invoke();
                return;
            }

            var ability = _selectedHero.GetComponent<LichFireballAbility>();
            if (ability == null)
            {
                LogWarning($"Герой {_selectedHero.name} не имеет способности LichFireballAbility");
                onFinished?.Invoke();
                return;
            }

            _activeFireball = ability;
            _isAimingFireball = true;
            _fireballPointerDown = true;
            _onFireballAimingFinished = onFinished;

            _activeFireball.StartTargeting();

            // Пробуем сразу поставить прицел по месту нажатия
            if (TryGetGroundWorld(screenPos, out var world))
            {
                Log($"Фаербол: начальная позиция из UI: {world}", LogLevel.Info);
                _activeFireball.UpdateTarget(world);
            }

            Log("Режим фаербола активирован (отслеживание одного касания)", LogLevel.Info);
        }

        /// <summary>
        /// Завершает режим прицеливания фаербола
        /// </summary>
        private void EndFireballTargetingMode()
        {
            Log("EndFireballTargetingMode() - завершение режима фаербола", LogLevel.Info);
            
            _isAimingFireball = false;
            _fireballPointerDown = false;
            _activeFireball = null;
            _fireballPointerId = int.MinValue;

            var cb = _onFireballAimingFinished;
            _onFireballAimingFinished = null;
            cb?.Invoke();

            Log("Режим фаербола деактивирован", LogLevel.Info);
        }

        // ==================== МЕТОДЫ ПЕРЕМЕЩЕНИЯ ГЕРОЯ ====================

        /// <summary>
        /// Пытается переместить выбранного героя по клику на землю
        /// </summary>
        private bool TryClickGroundForSelectedHero(Vector2 screenPos)
        {
            if (_selectedHero == null)
            {
                Log("TryClickGroundForSelectedHero: герой не выбран", LogLevel.Verbose);
                return false;
            }

            if (!TryGetGroundWorld(screenPos, out var worldPos))
            {
                Log($"TryClickGroundForSelectedHero: не удалось получить мировые координаты из {screenPos}", LogLevel.Verbose);
                return false;
            }

            // Поиск точки на навигационной сетке
            Vector3 targetPos;
            if (UnityEngine.AI.NavMesh.SamplePosition(worldPos, out var navHit, 1f, UnityEngine.AI.NavMesh.AllAreas))
            {
                targetPos = navHit.position;
                Log($"Найдена точка на NavMesh: {targetPos} (исходная: {worldPos})", LogLevel.Info);
            }
            else
            {
                targetPos = worldPos;
                LogWarning($"Точка {worldPos} не на NavMesh, используется исходная позиция");
            }

            // Здесь должен быть вызов метода перемещения героя
            // _selectedHero.MoveToPointManual(targetPos);

          //  Log($"[PinchToZoomAndPan] Перемещение героя {_selectedHero.name} в {targetPos}", LogLevel.Info);
          //  SpawnPoint(targetPos);
            
            return true;
        }

        /// <summary>
        /// Пытается получить мировые координаты по экранным, проверяя попадание в groundMask
        /// </summary>
        private bool TryGetGroundWorld(Vector2 screenPos, out Vector3 worldPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, groundMask);
            
            if (hit.collider == null)
            {
                Log($"TryGetGroundWorld: нет попадания в groundMask из {screenPos}", LogLevel.Verbose);
                worldPos = default;
                return false;
            }

            worldPos = hit.point;
            worldPos.z = 0f;
            
            Log($"TryGetGroundWorld: успех, позиция {worldPos}, объект: {hit.collider.name}", LogLevel.Verbose);
            return true;
        }

        /// <summary>
        /// Создает визуальную точку в указанной позиции
        /// </summary>
        private void SpawnPoint(Vector3 worldPos)
        {
            if (_isAimingFireball)
            {
                Log("SpawnPoint: пропускаем, активен режим фаербола", LogLevel.Verbose);
                return;
            }

            if (prefabPoint == null)
            {
                LogWarning("SpawnPoint: prefabPoint не назначен");
                return;
            }

            if (_lastPointInstance != null)
            {
                Log($"Удаление предыдущей точки {_lastPointInstance.name}", LogLevel.Verbose);
                Destroy(_lastPointInstance);
            }

            worldPos.z = 0f;
            _lastPointInstance = Instantiate(prefabPoint, worldPos, Quaternion.identity);
            
            if (debugSpawnPoint)
                Log($"Создана точка в {worldPos}", LogLevel.Info);
        }

        // ==================== МЕТОДЫ ВЗАИМОДЕЙСТВИЯ С ЖИВОТНЫМИ ====================

        /// <summary>
        /// Пытается убить обычное животное по клику
        /// </summary>
        private bool TryClickAnimal(Vector2 screenPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, animalMask);
            
            if (hit.collider == null)
            {
                return false;
            }

            var animal = hit.collider.GetComponent<Animals.AnimalsAI>();
            if (animal == null)
            {
                Log($"Объект {hit.collider.name} не имеет компонента AnimalsAI", LogLevel.Verbose);
                return false;
            }

            if (debugAnimalClicks)
                Log($"Убито животное: {animal.name}, позиция: {hit.point}", LogLevel.Info);
            
            animal.Kill();
            return true;
        }

        /// <summary>
        /// Пытается убить птицу по клику
        /// </summary>
        private bool TryClickBirdAnimal(Vector2 screenPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, animalMask);
            
            if (hit.collider == null)
            {
                return false;
            }

            var animalBird = hit.collider.GetComponent<Animals.BirdAI>();
            if (animalBird == null)
            {
                return false;
            }

            if (debugAnimalClicks)
                Log($"Убита птица: {animalBird.name}, позиция: {hit.point}", LogLevel.Info);
            
            animalBird.Kill();
            return true;
        }


        
        

        /// <summary>
        /// Логирует сообщение с учетом уровня детализации
        /// </summary>
        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (logLevel >= level)
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Debug.LogError(LOG_PREFIX + message);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(LOG_PREFIX + message);
                        break;
                    default:
                        Debug.Log(LOG_PREFIX + message);
                        break;
                }
            }
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        private void LogWarning(string message)
        {
            if (logLevel >= LogLevel.Warning)
                Debug.LogWarning(LOG_PREFIX + message);
        }

        /// <summary>
        /// Логирует ошибку
        /// </summary>
        private void LogError(string message)
        {
            if (logLevel >= LogLevel.Error)
                Debug.LogError(LOG_PREFIX + message);
        }

        
 
    }
}