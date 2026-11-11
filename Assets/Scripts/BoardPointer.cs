using UnityEngine;
using UnityEngine.EventSystems;

public class BoardPointer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public LoadoutBoard board;
    public int index;
    public bool isCell;

    public void OnBeginDrag(PointerEventData data)
    {
        int item = isCell ? board.Occupant(index % 6, index / 6) : index;
        if (GameCore.instance.inBattle || item < 0 || board.items[item].kind < 0 || board.visuals == null) return;
        board.Select(item);
        board.visuals.BeginDrag(item, isCell ? index : -1, data.position, data.pressEventCamera);
    }

    public void OnDrag(PointerEventData data)
    {
        if (board.visuals != null) board.visuals.MoveDrag(data.position, data.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (board.visuals != null) board.visuals.EndDrag();
    }

    public void OnDrop(PointerEventData data)
    {
        if (GameCore.instance.inBattle || board.visuals == null) return;
        board.visuals.MoveDrag(data.position, data.pressEventCamera);
        if (isCell) board.visuals.Drop();
        else board.visuals.DropToReserve();
    }
}
