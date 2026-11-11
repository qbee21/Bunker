using UnityEngine;

public class OrientationChecker : MonoBehaviour
{
    public Camera cam;
    [Header("Field dimensions (world units)")]
    [Min(0.1f)] public float fieldWidth = 20;
    [Min(0.1f)] public float fieldHeight = 9;
    [Header("Viewport (normalised screen coordinates)")]
    public bool manageViewport = true;
    public Rect landscapeViewport = new Rect(0, 0.19f, 1, 0.66f);
    public Rect portraitViewport = new Rect(0, 0.23f, 1, 0.62f);
    [Header("Field rotation")]
    public bool rotateField = true;
    public float landscapeAngle;
    public float portraitAngle = 90;
    [Header("Camera zoom")]
    public bool autoFit = true;
    [Min(0.01f)] public float landscapeOrthographicSize = 4.5f;
    [Min(0.01f)] public float portraitOrthographicSize = 10;
    int width;
    int height;
    bool dirty = true;
    Rect lastViewport;

    void OnEnable() { dirty = true; }
    void OnValidate() { dirty = true; }

    void LateUpdate()
    {
        if (cam == null) return;
        if (!dirty && Screen.width == width && Screen.height == height && cam.rect == lastViewport) return;
        ApplyLayout();
    }

    public static Rect ClampViewport(Rect value)
    {
        float x = Mathf.Clamp(value.x, 0, 0.99f);
        float y = Mathf.Clamp(value.y, 0, 0.99f);
        return new Rect(x, y, Mathf.Min(Mathf.Max(0.01f, value.width), 1 - x), Mathf.Min(Mathf.Max(0.01f, value.height), 1 - y));
    }

    [ContextMenu("Apply camera layout")]
    public void ApplyLayout()
    {
        if (cam == null || Screen.width <= 0 || Screen.height <= 0) return;
        width = Screen.width;
        height = Screen.height;
        bool portrait = height > width;
        if (rotateField) transform.rotation = Quaternion.Euler(0, 0, portrait ? portraitAngle : landscapeAngle);
        if (manageViewport) cam.rect = ClampViewport(portrait ? portraitViewport : landscapeViewport);
        float aspect = width * Mathf.Max(0.01f, cam.rect.width) / (height * Mathf.Max(0.01f, cam.rect.height));
        float halfWidth = Mathf.Max(0.1f, fieldWidth) * 0.5f;
        float halfHeight = Mathf.Max(0.1f, fieldHeight) * 0.5f;
        // Bounds of the rotated field, also valid for custom Inspector angles.
        float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
        float fitWidth = Mathf.Abs(Mathf.Cos(angle)) * halfWidth + Mathf.Abs(Mathf.Sin(angle)) * halfHeight;
        float fitHeight = Mathf.Abs(Mathf.Sin(angle)) * halfWidth + Mathf.Abs(Mathf.Cos(angle)) * halfHeight;
        cam.orthographicSize = autoFit ? Mathf.Max(fitHeight, fitWidth / aspect)
            : Mathf.Max(0.01f, portrait ? portraitOrthographicSize : landscapeOrthographicSize);
        lastViewport = cam.rect;
        dirty = false;
    }
}
