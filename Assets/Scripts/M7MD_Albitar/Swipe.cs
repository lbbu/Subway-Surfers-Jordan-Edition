using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class Swipe : MonoBehaviour
{
    public event EventHandler OnSwipeUp, OnSwipeDown, OnSwipeLeft, OnSwipeRight;
    public static Swipe Instance;
    [Tooltip("Swipe distance in pixels at a 1080-pixel short screen edge.")]
    [SerializeField, Min(1)] private float deadZone = 70f;
    [SerializeField, Min(0.1f)] private float maximumGestureTime = 0.7f;
    [SerializeField] private bool ignoreGesturesStartingOverUI = true;
    [SerializeField] private bool desktopTestControls = true;

    private int pointerId = int.MinValue;
    private Vector2 startTouch;
    private float startTime;
    private bool consumed;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    public Vector2 StartTouch => startTouch;
    public bool GameplayEnabled => gameplayEnabled;
    private bool gameplayEnabled = true;

    /// <summary>تبديل استقبال اللعب يلغي اللمسة القديمة حتى لا تُنفذ بعد الاستكمال أو إعادة الجولة.</summary>
    public void SetGameplayEnabled(bool value)
    {
        gameplayEnabled = value;
        CancelGesture();
    }

    private void OnEnable()
    {
        if (Instance && Instance != this) { enabled = false; return; }
        Instance = this;
        CancelGesture();
    }

    private void OnDisable()
    {
        CancelGesture();
        if (Instance == this) Instance = null;
    }

    private void OnApplicationFocus(bool focused) { if (!focused) CancelGesture(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelGesture(); }

    private void Update()
    {
        if (!gameplayEnabled) return;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            bool found = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (pointerId == int.MinValue && touch.phase == TouchPhase.Began)
                    BeginGesture(touch.fingerId, touch.position);
                if (touch.fingerId != pointerId) continue;
                found = true;
                if (touch.phase == TouchPhase.Canceled) CancelGesture();
                else
                {
                    EvaluateGesture(touch.position);
                    if (touch.phase == TouchPhase.Ended) CancelGesture();
                }
                break;
            }
            if (!found && pointerId != int.MinValue) CancelGesture();
            return; // Never process emulated mouse input for the same touch.
        }
        if (pointerId >= 0) CancelGesture();
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!desktopTestControls) return;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) Emit(0);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Emit(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Emit(2);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Emit(3);
        if (Input.GetMouseButtonDown(0)) BeginGesture(-1, Input.mousePosition);
        if (pointerId == -1)
        {
            EvaluateGesture(Input.mousePosition);
            if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) CancelGesture();
        }
#endif
#elif ENABLE_INPUT_SYSTEM
        var screen = Touchscreen.current;
        bool touchActive = false, trackedTouchFound = false;
        if (screen != null)
        {
            foreach (var touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                if (touch.press.isPressed || touch.press.wasReleasedThisFrame) touchActive = true;
                if (pointerId == int.MinValue && touch.press.wasPressedThisFrame)
                    BeginGesture(id, touch.position.ReadValue());
                if (id != pointerId) continue;
                trackedTouchFound = true;
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) CancelGesture();
                else
                {
                    EvaluateGesture(touch.position.ReadValue());
                    if (touch.press.wasReleasedThisFrame || !touch.press.isPressed) CancelGesture();
                }
                break;
            }
        }
        if (pointerId >= 0 && !trackedTouchFound) CancelGesture();
        if (touchActive) return;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!desktopTestControls) return;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Emit(0);
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Emit(1);
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Emit(2);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Emit(3);
        }
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame) BeginGesture(-1, mouse.position.ReadValue());
            if (pointerId == -1)
            {
                EvaluateGesture(mouse.position.ReadValue());
                if (!mouse.leftButton.isPressed) CancelGesture();
            }
        }
#endif
#endif
    }

    private void BeginGesture(int id, Vector2 position)
    {
        if (pointerId != int.MinValue) return;
        pointerId = id;
        startTouch = position;
        startTime = Time.unscaledTime;
        consumed = false;
        if (!ignoreGesturesStartingOverUI || !EventSystem.current) return;
        var data = new PointerEventData(EventSystem.current) { position = position };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        foreach (var hit in uiHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) { consumed = true; break; }
        uiHits.Clear();
    }

    private void EvaluateGesture(Vector2 position)
    {
        if (pointerId == int.MinValue || consumed) return;
        if (Time.unscaledTime - startTime > maximumGestureTime) { consumed = true; return; }
        Vector2 delta = position - startTouch;
        float threshold = Mathf.Max(12f, deadZone * Mathf.Min(Screen.width, Screen.height) / 1080f);
        if (delta.sqrMagnitude < threshold * threshold) return;
        consumed = true; // Exactly one action until this finger is released.
        Emit(Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x < 0 ? 2 : 3) : (delta.y < 0 ? 1 : 0));
    }

    private void Emit(int direction)
    {
        if (!gameplayEnabled) return;
        switch (direction)
        {
            case 0: OnSwipeUp?.Invoke(this, EventArgs.Empty); break;
            case 1: OnSwipeDown?.Invoke(this, EventArgs.Empty); break;
            case 2: OnSwipeLeft?.Invoke(this, EventArgs.Empty); break;
            case 3: OnSwipeRight?.Invoke(this, EventArgs.Empty); break;
        }
    }

    private void CancelGesture()
    {
        pointerId = int.MinValue;
        startTouch = Vector2.zero;
        consumed = false;
    }
}

