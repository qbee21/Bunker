using UnityEngine;

/// <summary>Fits the screen-space game UI into the iOS safe area at runtime.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class SafeAreaLayout : MonoBehaviour
{
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    void OnEnable() { Apply(); }
    void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) Apply();
    }

    void Apply()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        lastSafeArea = safe;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
