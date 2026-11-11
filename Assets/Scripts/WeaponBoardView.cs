using UnityEngine;
using UnityEngine.UI;

public class WeaponBoardView : MonoBehaviour
{
    public LoadoutBoard board;
    public RectTransform grid;
    public RectTransform overlay;
    public Image[] reserveIcons;
    public Image[] placedIcons;
    public RectTransform dragRoot;
    public Image dragIcon;
    public Image[] dragCells;
    public Sprite fallbackSprite;
    public Color validColor = new Color(0.3f, 0.9f, 0.5f, 0.55f);
    public Color invalidColor = new Color(1, 0.3f, 0.25f, 0.55f);
    Vector2 pointer;
    Camera eventCamera;
    Vector2Int grabbed;
    Vector2Int hovered;
    Vector2 previousGridSize;
    bool overGrid;
    bool dragging;

    void Start()
    {
        // Accept drops on reserve slots AND the gaps between them.
        if (reserveIcons == null || reserveIcons.Length == 0 || reserveIcons[0] == null) return;
        Transform reserve = reserveIcons[0].transform.parent.parent;
        Image background = reserve.GetComponent<Image>();
        if (background == null)
        {
            background = reserve.gameObject.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0);
        }
        background.raycastTarget = true;
        BoardPointer target = reserve.GetComponent<BoardPointer>();
        if (target == null) target = reserve.gameObject.AddComponent<BoardPointer>();
        target.board = board;
        target.isCell = false;
        target.index = -1;
    }

    Sprite Icon(WeaponData weapon) { return weapon.sprite != null ? weapon.sprite : fallbackSprite; }

    public void Refresh()
    {
        if (board == null || board.items == null || reserveIcons == null || placedIcons == null || grid == null) return;
        previousGridSize = grid.rect.size;
        for (int i = 0; i < board.items.Length; i++)
        {
            LoadoutItem item = board.items[i];
            bool exists = item.kind >= 0;
            reserveIcons[i].gameObject.SetActive(exists);
            placedIcons[i].gameObject.SetActive(exists && item.x >= 0 && (!dragging || board.dragItem != i));
            if (!exists) continue;
            WeaponData weapon = board.weapons[item.kind];
            reserveIcons[i].sprite = Icon(weapon);
            reserveIcons[i].color = Color.white;
            if (item.x < 0) continue;
            Vector2Int[] shape = board.Shape(item.kind, item.rotation);
            Vector2Int size = Bounds(shape);
            RectTransform rect = placedIcons[i].rectTransform;
            Vector2 center = new Vector2((item.x + size.x * 0.5f) / 6, 1 - (item.y + size.y * 0.5f) / 6);
            rect.anchorMin = center;
            rect.anchorMax = center;
            rect.anchoredPosition = Vector2.zero;
            FitIcon(placedIcons[i], Icon(weapon), new Vector2(grid.rect.width * size.x / 6, grid.rect.height * size.y / 6), item.rotation);
        }
    }

    public static Vector2Int Bounds(Vector2Int[] shape)
    {
        int x = 1;
        int y = 1;
        foreach (Vector2Int cell in shape) { x = Mathf.Max(x, cell.x + 1); y = Mathf.Max(y, cell.y + 1); }
        return new Vector2Int(x, y);
    }

    public static Vector2Int CellAt(Rect rect, Vector2 point)
    {
        if (rect.width <= 0 || rect.height <= 0) return Vector2Int.zero;
        return new Vector2Int(Mathf.FloorToInt((point.x - rect.xMin) / rect.width * 6), Mathf.FloorToInt((rect.yMax - point.y) / rect.height * 6));
    }

    void FitIcon(Image view, Sprite sprite, Vector2 size, int rotation)
    {
        view.sprite = sprite;
        view.color = Color.white;
        view.preserveAspect = true;
        view.rectTransform.sizeDelta = (rotation % 2 == 0 ? size : new Vector2(size.y, size.x)) * 0.82f;
        view.rectTransform.localRotation = Quaternion.Euler(0, 0, -90 * rotation);
    }

    public void BeginDrag(int item, int sourceCell, Vector2 screenPoint, Camera camera)
    {
        if (board == null || item < 0 || board.items[item].kind < 0 || dragRoot == null || dragIcon == null) return;
        EndDrag();
        board.dragItem = item;
        grabbed = sourceCell < 0 ? Vector2Int.zero : new Vector2Int(sourceCell % 6 - board.items[item].x, sourceCell / 6 - board.items[item].y);
        dragging = true;
        dragRoot.gameObject.SetActive(true);
        dragRoot.SetAsLastSibling();
        MoveDrag(screenPoint, camera);
    }

    public void MoveDrag(Vector2 screenPoint, Camera camera)
    {
        if (!dragging) return;
        pointer = screenPoint;
        eventCamera = camera;
        PaintDrag();
    }

    void LateUpdate()
    {
        if (grid != null && !dragging && grid.rect.size != previousGridSize && board != null && board.items != null) Refresh();
        if (!dragging) return;
        if (GameCore.instance.inBattle || grid == null || overlay == null || !grid.gameObject.activeInHierarchy) { EndDrag(); return; }
        PaintDrag();
    }

    void PaintDrag()
    {
        int index = board.dragItem;
        if (index < 0 || board.items[index].kind < 0 || grid.rect.width <= 0 || grid.rect.height <= 0) { EndDrag(); return; }
        board.Refresh();
        LoadoutItem item = board.items[index];
        Vector2Int[] shape = board.Shape(item.kind, item.rotation);
        Vector2Int size = Bounds(shape);
        Vector2 cellSize = grid.rect.size / 6;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(grid, pointer, eventCamera, out local);
        overGrid = grid.rect.Contains(local);
        hovered = CellAt(grid.rect, local) - grabbed;
        bool valid = overGrid && board.CanPlace(index, hovered.x, hovered.y, item.rotation);
        Color color = overGrid ? valid ? validColor : invalidColor : new Color(0.6f, 0.7f, 0.8f, 0.5f);
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, pointer, eventCamera, out position);
        position -= new Vector2(grabbed.x * cellSize.x, -grabbed.y * cellSize.y);
        if (overGrid)
        {
            Vector3 corner = grid.TransformPoint(new Vector3(grid.rect.xMin + hovered.x * cellSize.x, grid.rect.yMax - hovered.y * cellSize.y));
            position = overlay.InverseTransformPoint(corner);
        }
        dragRoot.anchoredPosition = position;
        dragRoot.sizeDelta = new Vector2(size.x * cellSize.x, size.y * cellSize.y);
        for (int i = 0; i < dragCells.Length; i++)
        {
            bool visible = i < shape.Length;
            dragCells[i].gameObject.SetActive(visible);
            if (!visible) continue;
            dragCells[i].color = color;
            dragCells[i].rectTransform.anchoredPosition = new Vector2(shape[i].x * cellSize.x, -shape[i].y * cellSize.y);
            dragCells[i].rectTransform.sizeDelta = cellSize * 0.94f;
            int x = hovered.x + shape[i].x;
            int y = hovered.y + shape[i].y;
            if (overGrid && x >= 0 && x < 6 && y >= 0 && y < 6) board.cellViews[y * 6 + x].color = Color.Lerp(board.cellViews[y * 6 + x].color, color, 0.65f);
        }
        FitIcon(dragIcon, Icon(board.weapons[item.kind]), dragRoot.sizeDelta, item.rotation);
        dragIcon.transform.SetAsLastSibling();
    }

    public void Drop()
    {
        if (!dragging || !overGrid) return;
        int index = board.dragItem;
        if (index < 0 || hovered.x < 0 || hovered.x >= 6 || hovered.y < 0 || hovered.y >= 6) return;
        // A drag onto a locked cell must not spend an expansion slot.
        if (!board.cells[hovered.y * 6 + hovered.x]) { EndDrag(); return; }
        board.Select(index);
        board.Cell(hovered.y * 6 + hovered.x);
        EndDrag();
    }

    public void DropToReserve()
    {
        if (!dragging || GameCore.instance.inBattle) return;
        int index = board.dragItem;
        if (index < 0 || index >= board.items.Length) return;
        board.Select(index);
        board.RemoveSelected();
        EndDrag();
    }

    public void EndDrag()
    {
        dragging = false;
        overGrid = false;
        board.dragItem = -1;
        if (dragRoot != null) dragRoot.gameObject.SetActive(false);
        if (board != null && board.items != null) board.Refresh();
    }

    void OnDisable() { EndDrag(); }
}
