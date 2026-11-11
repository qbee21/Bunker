using UnityEngine;
using UnityEngine.UI;

public class LoadoutBoard : MonoBehaviour
{
    public WeaponBoardView visuals;
    public WeaponData[] weapons;
    public LoadoutItem[] items;
    public bool[] cells;
    public Image[] cellViews;
    public Text[] cellTexts;
    public Image[] itemViews;
    public Text[] itemTexts;
    public Button[] itemButtons;
    public Text statusView;
    public Text extensionView;
    public Button rotateButton;
    public Button removeButton;
    public Button extensionButton;
    public Button rewardCellButton;
    public Button rewardWeaponButton;
    public int selected = -1;
    public int extensions;
    public bool extensionMode;
    public int extensionRotation;
    public int dragItem = -1;
    string notice;

    public void Load()
    {
        SaveData save = GameCore.instance.economy.data;
        cells = save.cells;
        items = save.items;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) items[i] = new LoadoutItem();
            if (items[i].kind >= weapons.Length || items[i].kind < -1 || (items[i].kind >= 0 && weapons[items[i].kind] == null)) items[i] = new LoadoutItem();
        }
        if (save.preparedWave == 0)
        {
            for (int y = 2; y <= 3; y++) for (int x = 1; x <= 3; x++) cells[y * 6 + x] = true;
        }
        // Old saves counted pairs; the new counter counts individual cells.
        if (save.slotRulesVersion == 0)
        {
            save.extensions = Mathf.Max(0, save.extensions) * 2;
            if (save.preparedWave == 0) save.extensions += 4;
            save.slotRulesVersion = 1;
        }
        extensions = save.extensions;
        extensionRotation = save.extensionRotation;
        selected = -1;
        extensionMode = false;
        ValidateLayout();
        Refresh();
    }

    public void Prepare()
    {
        GameCore game = GameCore.instance;
        // Remove weapons from other eras, both installed and in reserve.
        bool changed = false;
        for (int i = 0; i < items.Length; i++)
            if (items[i].kind >= 0 && (weapons[items[i].kind] == null || weapons[items[i].kind].accessInEra != game.level))
            {
                items[i] = new LoadoutItem();
                changed = true;
            }
        if (HasCurrentEraWeapons() && (game.economy.data.preparedWave != game.wave || changed || !HasWeapons()))
        {
            AddCurrentEraWeapon();
            AddCurrentEraWeapon();
            game.economy.data.preparedWave = game.wave;
            game.economy.data.prepReward = false;
            Save();
        }
        else if (changed) Save();
        selected = -1;
        notice = HasCurrentEraWeapons() ? "" : LocalText.Pick("Для этой эпохи не настроено оружие (accessInEra)", "No weapons configured for this era (accessInEra)");
        Refresh();
    }

    bool HasWeapons()
    {
        foreach (LoadoutItem item in items) if (item.kind >= 0) return true;
        return false;
    }

    bool HasCurrentEraWeapons()
    {
        foreach (WeaponData weapon in weapons)
            if (weapon != null && weapon.grantInPreparation && weapon.accessInEra == GameCore.instance.level) return true;
        return false;
    }

    public bool AddCurrentEraWeapon()
    {
        int count = 0;
        foreach (WeaponData weapon in weapons)
            if (weapon != null && weapon.grantInPreparation && weapon.accessInEra == GameCore.instance.level) count++;
        if (count == 0) return false;
        int choice = Random.Range(0, count);
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i] != null && weapons[i].grantInPreparation && weapons[i].accessInEra == GameCore.instance.level && choice-- == 0)
                return AddWeapon(i);
        return false;
    }

    public void GrantWaveSlots()
    {
        extensions += 2;
        Save();
    }

    public bool AddWeapon(int kind)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].kind >= 0) continue;
            items[i] = new LoadoutItem { kind = kind };
            return true;
        }
        return false;
    }

    public Vector2Int Rotate(Vector2Int cell, int rotation)
    {
        for (int i = 0; i < rotation % 4; i++) cell = new Vector2Int(-cell.y, cell.x);
        return cell;
    }

    public Vector2Int[] Shape(int kind, int rotation)
    {
        Vector2Int[] original = weapons[kind].cells;
        Vector2Int[] shape = new Vector2Int[original.Length];
        int minX = 100;
        int minY = 100;
        for (int i = 0; i < shape.Length; i++)
        {
            shape[i] = Rotate(original[i], rotation);
            minX = Mathf.Min(minX, shape[i].x);
            minY = Mathf.Min(minY, shape[i].y);
        }
        for (int i = 0; i < shape.Length; i++) shape[i] -= new Vector2Int(minX, minY);
        return shape;
    }

    public int Occupant(int x, int y, int ignore = -1)
    {
        for (int i = 0; i < items.Length; i++)
        {
            LoadoutItem item = items[i];
            if (i == ignore || item.kind < 0 || item.x < 0) continue;
            foreach (Vector2Int cell in Shape(item.kind, item.rotation))
                if (item.x + cell.x == x && item.y + cell.y == y) return i;
        }
        return -1;
    }

    public bool CanPlace(int index, int x, int y, int rotation)
    {
        foreach (Vector2Int cell in Shape(items[index].kind, rotation))
        {
            int cx = x + cell.x;
            int cy = y + cell.y;
            if (cx < 0 || cx >= 6 || cy < 0 || cy >= 6 || !cells[cy * 6 + cx] || Occupant(cx, cy, index) >= 0) return false;
        }
        return true;
    }

    public void Select(int index)
    {
        if (GameCore.instance.inBattle || index < 0 || index >= items.Length || items[index].kind < 0) return;
        selected = index;
        extensionMode = false;
        notice = "";
        Refresh();
    }

    public void Cell(int index)
    {
        if (GameCore.instance.inBattle || index < 0 || index >= cells.Length) return;
        int x = index % 6;
        int y = index / 6;
        if (!cells[index]) { AddCells(x, y); return; }
        if (selected < 0)
        {
            int item = Occupant(x, y);
            if (item >= 0) Select(item);
            return;
        }
        LoadoutItem value = items[selected];
        if (CanPlace(selected, x, y, value.rotation))
        {
            value.x = x;
            value.y = y;
            Save();
            notice = "";
        }
        else notice = LocalText.Pick("Не помещается: поверни или выбери другое место", "Does not fit: rotate or choose another cell");
        Refresh();
    }

    void AddCells(int x, int y)
    {
        if (extensions <= 0)
        {
            notice = LocalText.Pick("Нет доступных слотов. Пройди волну: +2", "No slots available. Clear a wave: +2");
            Refresh();
            return;
        }
        if (cells[y * 6 + x]) return;
        cells[y * 6 + x] = true;
        extensions--;
        notice = "";
        Save();
        Refresh();
    }

    public void RotateSelected()
    {
        if (GameCore.instance.inBattle) return;
        if (selected >= 0)
        {
            LoadoutItem item = items[selected];
            int rotation = (item.rotation + 1) % 4;
            if (item.x >= 0 && !CanPlace(selected, item.x, item.y, rotation)) { item.x = -1; item.y = -1; }
            item.rotation = rotation;
        }
        Save();
        Refresh();
    }

    // Kept for existing serialized UnityEvents; expansion now uses cell pluses.
    public void Extension() { }

    public void RemoveSelected()
    {
        if (GameCore.instance.inBattle || selected < 0) return;
        LoadoutItem item = items[selected];
        if (item.x < 0) return;
        item.x = -1;
        item.y = -1;
        Save();
        Refresh();
    }

    public bool Armed()
    {
        foreach (LoadoutItem item in items) if (item.kind >= 0 && item.x >= 0) return true;
        return false;
    }

    // Compatibility targets for existing UnityEvents. Rewarded-ad grants are removed.
    public void RewardCells() { }
    public void RewardWeapon() { }

    public void Save()
    {
        SaveData save = GameCore.instance.economy.data;
        save.items = items;
        save.cells = cells;
        save.extensions = extensions;
        save.extensionRotation = extensionRotation;
        GameCore.instance.economy.Save();
    }

    void ValidateLayout()
    {
        for (int i = 0; i < items.Length; i++)
        {
            LoadoutItem item = items[i];
            item.rotation = (Mathf.Max(0, item.rotation)) % 4;
            if (item.kind >= 0 && item.x >= 0 && !CanPlace(i, item.x, item.y, item.rotation)) { item.x = -1; item.y = -1; }
        }
    }

    public void Refresh()
    {
        if (items == null) return;
        for (int i = 0; i < cells.Length; i++)
        {
            int item = Occupant(i % 6, i / 6);
            cellViews[i].color = item >= 0 ? weapons[items[item].kind].color : cells[i] ? new Color(0.28f, 0.38f, 0.3f) : new Color(0.12f, 0.15f, 0.17f);
            cellTexts[i].color = item >= 0 ? new Color(0.06f, 0.09f, 0.07f) : new Color(0.95f, 0.95f, 0.89f);
            cellTexts[i].text = item >= 0 ? (item + 1).ToString() : cells[i] ? "" : "+";
            if (item >= 0 && selected == item) cellViews[i].color = Color.Lerp(cellViews[i].color, Color.white, 0.4f);
        }
        for (int i = 0; i < items.Length; i++)
        {
            LoadoutItem item = items[i];
            itemButtons[i].interactable = item.kind >= 0;
            itemViews[i].color = item.kind >= 0 ? weapons[item.kind].color : new Color(0.16f, 0.19f, 0.2f);
            if (selected == i) itemViews[i].color = Color.Lerp(itemViews[i].color, Color.white, 0.4f);
            itemTexts[i].color = item.kind >= 0 ? new Color(0.06f, 0.09f, 0.07f) : new Color(0.95f, 0.95f, 0.89f);
            if (item.kind < 0) itemTexts[i].text = "—";
            else
            {
                WeaponData weapon = weapons[item.kind];
                itemTexts[i].text = LocalText.Pick(weapon.titleRu, weapon.titleEn) + (item.x >= 0 ? " *" : "");
            }
        }
        extensionView.text = LocalText.Pick("Доступно слотов: ", "Slots available: ") + extensions;
        if (extensionButton != null)
        {
            extensionButton.interactable = false;
            extensionButton.enabled = false;
            // Keep its label and RectTransform, remove only button visuals/input.
            if (extensionButton.targetGraphic != null) extensionButton.targetGraphic.enabled = false;
        }
        extensionView.raycastTarget = false;
        rotateButton.interactable = selected >= 0;
        removeButton.interactable = selected >= 0 && items[selected].x >= 0;
        if (rewardCellButton != null) rewardCellButton.gameObject.SetActive(false);
        if (rewardWeaponButton != null) rewardWeaponButton.gameObject.SetActive(false);
        string text = notice;
        if (string.IsNullOrEmpty(text))
        {
            if (selected >= 0)
            {
                WeaponData weapon = weapons[items[selected].kind];
                text = LocalText.Pick(weapon.titleRu, weapon.titleEn) + "  " + ShapeText(items[selected]) + "  •  " + LocalText.Pick("урон ", "damage ") + weapon.power + " / " + weapon.interval + LocalText.Pick(" сек", " sec");
            }
            else text = LocalText.Pick("Перетаскивай оружие на поле и обратно в запас. Плюсик открывает 1 слот.", "Drag weapons onto the board and back to reserve. A plus unlocks 1 slot.");
        }
        statusView.text = text;
        if (visuals != null) visuals.Refresh();
    }

    string ShapeText(LoadoutItem item)
    {
        Vector2Int[] shape = Shape(item.kind, item.rotation);
        int maxX = 0;
        int maxY = 0;
        foreach (Vector2Int cell in shape) { maxX = Mathf.Max(maxX, cell.x); maxY = Mathf.Max(maxY, cell.y); }
        return (maxX + 1) + "x" + (maxY + 1) + " / " + shape.Length;
    }
}
