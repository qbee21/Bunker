using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ScreenLayout
{
    public RectTransform rect;
    public Vector2 landscapeMin;
    public Vector2 landscapeMax;
    public Vector2 portraitMin;
    public Vector2 portraitMax;
}

[System.Serializable]
public class ScreenText
{
    public Text view;
    public int size;
}

[ExecuteAlways]
public class ResponsiveUI : MonoBehaviour
{
    public CanvasScaler scaler;
    public ScreenLayout[] items;
    public ScreenText[] texts;
    public RectTransform boardArea;
    public RectTransform boardGrid;
    Vector2 previousSize;
    Vector2 previousBoardArea;

    void OnEnable() { previousSize = Vector2.zero; previousBoardArea = Vector2.zero; }
    void OnValidate() { previousSize = Vector2.zero; previousBoardArea = Vector2.zero; }
    void OnRectTransformDimensionsChange() { previousSize = Vector2.zero; previousBoardArea = Vector2.zero; }

    void Update()
    {
        if (scaler == null) return;
        Vector2 size = ((RectTransform)scaler.transform).rect.size;
        Vector2 area = boardArea != null ? boardArea.rect.size : Vector2.zero;
        if (size == previousSize && area == previousBoardArea) return;
        ApplyLayout();
    }

    [ContextMenu("Apply layout")]
    public void ApplyLayout()
    {
        if (scaler == null) return;
        Vector2 size = ((RectTransform)scaler.transform).rect.size;
        if (size.x <= 0 || size.y <= 0) return;
        bool portrait = size.y > size.x;
        scaler.referenceResolution = portrait ? new Vector2(720, 1280) : new Vector2(1280, 720);
        if (items != null)
        foreach (ScreenLayout item in items)
        {
            if (item.rect == null) continue;
            item.rect.anchorMin = portrait ? item.portraitMin : item.landscapeMin;
            item.rect.anchorMax = portrait ? item.portraitMax : item.landscapeMax;
            item.rect.offsetMin = Vector2.zero;
            item.rect.offsetMax = Vector2.zero;
        }
        if (boardArea != null && boardGrid != null)
        {
            Vector2 area = boardArea.rect.size;
            float side = Mathf.Max(0, Mathf.Min(area.x, area.y));
            boardGrid.anchorMin = new Vector2(0.5f, 0.5f);
            boardGrid.anchorMax = new Vector2(0.5f, 0.5f);
            boardGrid.pivot = new Vector2(0.5f, 0.5f);
            boardGrid.anchoredPosition = Vector2.zero;
            boardGrid.sizeDelta = new Vector2(side, side);
            previousBoardArea = area;
        }
        else previousBoardArea = Vector2.zero;
        previousSize = size;
        if (texts != null)
        foreach (ScreenText text in texts)
            if (text.view != null) text.view.fontSize = Mathf.RoundToInt(text.size * (portrait ? 1.45f : 1));
    }
}
