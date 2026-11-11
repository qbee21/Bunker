using System;
using UnityEngine;
using UnityEngine.UI;
using PlayerPrefs = UnityEngine.PlayerPrefs;

public class EconomyCore : MonoBehaviour
{
    public static EconomyCore instance;
    public float money;
    public Text moneyView;
    public SaveData data;
    public Text[] upgradeViews;
    public Button[] upgradeButtons;
    [Tooltip("Optional icon overrides. Empty entries use the child Image on each upgrade button.")]
    public Image[] upgradeIcons;
    public Button dailyButton;
    public Text dailyView;
    public Text bestView;
    float refreshTime;

    void Awake()
    {
        instance = this;
        Load();
    }

    public void Load()
    {
        data = new SaveData();
        if (GameCore.instance != null && GameCore.instance.balance != null) data.coins = Mathf.Max(0, GameCore.instance.balance.startingCoins);
        string json = PlayerPrefs.GetString("BunkerSave1", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                if (Valid(loaded)) data = loaded;
            }
            catch (Exception) { }
        }
        money = data.coins;
    }

    public bool Valid(SaveData value)
    {
        if (value == null || value.version != 1 || value.coins < 0 || value.completedWave < 0 || value.completedWave > 100000) return false;
        if (value.foodUpgrade < 0 || value.foodUpgrade > 1000 || value.revision < 0) return false;
        if (value.unitHealth == null || value.unitHealth.Length != 3 || value.unitPower == null || value.unitPower.Length != 3) return false;
        if (value.cells == null || value.cells.Length != 36 || value.items == null || value.items.Length != 16) return false;
        if (value.bunkerHealth < 0 || value.bunkerHealth > 100 || value.bunkerPower < 0 || value.bunkerPower > 100) return false;
        for (int i = 0; i < 3; i++)
            if (value.unitHealth[i] < 0 || value.unitHealth[i] > 100 || value.unitPower[i] < 0 || value.unitPower[i] > 100) return false;
        return true;
    }

    void Update()
    {
        refreshTime -= Time.unscaledDeltaTime;
        if (refreshTime > 0) return;
        refreshTime = 1;
        ReloadData();
    }

    public void Save()
    {
        if (GameCore.instance != null && !GameCore.instance.initialized) return;
        data.coins = Mathf.RoundToInt(money);
        data.revision = Math.Max(DateTime.UtcNow.Ticks, data.revision + 1);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("BunkerSave1", json);
        PlayerPrefs.Save();
        if (GameCore.instance != null && GameCore.instance.platform != null) GameCore.instance.platform.Save(json);
    }

    public void ReloadData()
    {
        if (moneyView == null || GameCore.instance == null) return;
        RefreshUpgradeIcons();
        moneyView.text = LocalText.Pick("Монеты: ", "Coins: ") + Mathf.RoundToInt(money);
        if (upgradeViews != null)
        for (int i = 0; i < upgradeViews.Length; i++)
        {
            int rank = Rank(i);
            upgradeViews[i].text = UpgradeName(i) + "  " + rank + "\n" + (rank >= 100 ? "MAX" : Cost(i).ToString());
            upgradeButtons[i].interactable = !GameCore.instance.inBattle && rank < 100 && money >= Cost(i);
        }
        long remaining = Math.Max(0, data.dailyTime + TimeSpan.TicksPerDay - DateTime.UtcNow.Ticks);
        dailyButton.interactable = remaining == 0 && !GameCore.instance.inBattle;
        dailyView.text = remaining == 0 ? LocalText.Pick("Подарок +", "Gift +") + GameCore.instance.balance.dailyCoins : LocalText.Pick("Подарок через ", "Gift in ") + TimeSpan.FromTicks(remaining).ToString(@"hh\:mm\:ss");
        bestView.text = LocalText.Pick("Пройдено волн: ", "Waves cleared: ") + data.completedWave;
    }

    public void RefreshUpgradeIcons()
    {
        GameCore game = GameCore.instance;
        if (game == null || upgradeButtons == null || game.entitys == null || game.entitys.Length == 0) return;
        int era = Mathf.Clamp(game.balance.Era(game.wave), 0, game.entitys.Length - 1);
        if (upgradeIcons == null) upgradeIcons = new Image[upgradeButtons.Length];
        else if (upgradeIcons.Length < upgradeButtons.Length) Array.Resize(ref upgradeIcons, upgradeButtons.Length);
        for (int i = 0; i < upgradeButtons.Length && i < 8; i++)
        {
            if (upgradeIcons[i] == null && upgradeButtons[i] != null)
            {
                Transform child = upgradeButtons[i].transform.Find("Image");
                if (child == null) child = upgradeButtons[i].transform.Find("Icon");
                if (child != null) upgradeIcons[i] = child.GetComponent<Image>();
            }
            Image icon = upgradeIcons[i];
            if (icon == null) continue;
            Sprite sprite = null;
            if (i < 2)
            {
                if (game.baseSkin != null && game.baseSkin.Length > 0)
                    sprite = game.baseSkin[Mathf.Min(era, game.baseSkin.Length - 1)];
            }
            else
            {
                EntityData[] units = game.entitys[era].entity;
                int role = (i - 2) / 2;
                if (units != null && role < units.Length && units[role] != null) sprite = units[role].skin;
            }
            if (sprite == null) continue;
            icon.sprite = sprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.type = Image.Type.Simple;
            icon.raycastTarget = false;
        }
    }

    public void AddCoin(int coin)
    {
        money = Mathf.Min(1000000000, money + Mathf.Max(0, coin));
        ReloadData();
        Save();
    }

    int Rank(int index)
    {
        if (index == 0) return data.bunkerHealth;
        if (index == 1) return data.bunkerPower;
        return index % 2 == 0 ? data.unitHealth[(index - 2) / 2] : data.unitPower[(index - 2) / 2];
    }

    public int Cost(int index)
    {
        return Mathf.RoundToInt(Mathf.Min(1000000000, GameCore.instance.balance.upgradePrice * Mathf.Pow(GameCore.instance.balance.upgradePriceGrowth, Rank(index))));
    }

    public void Upgrade(int index)
    {
        if (!GameCore.instance.platform.CanPlay() || index < 0 || index > 7 || GameCore.instance.inBattle || Rank(index) >= 100) return;
        int cost = Cost(index);
        if (money < cost) return;
        money -= cost;
        if (index == 0) data.bunkerHealth++;
        else if (index == 1) data.bunkerPower++;
        else if (index % 2 == 0) data.unitHealth[(index - 2) / 2]++;
        else data.unitPower[(index - 2) / 2]++;
        Save();
        ReloadData();
        GameCore.instance.sound.PlayClick();
    }

    public void Daily()
    {
        long now = DateTime.UtcNow.Ticks;
        if (!GameCore.instance.platform.CanPlay() || GameCore.instance.inBattle || now < data.dailyTime + TimeSpan.TicksPerDay) return;
        data.dailyTime = now;
        AddCoin(GameCore.instance.balance.dailyCoins);
        Save();
    }

    public int FoodCapacity() { return GameCore.instance.balance.FoodCapacity(data.foodUpgrade); }
    public float FoodInterval() { return GameCore.instance.balance.FoodInterval(data.foodUpgrade); }
    public int StartingFood() { return GameCore.instance.balance.StartingFood(data.foodUpgrade); }
    public bool CanUpgradeFood()
    {
        GameCore game = GameCore.instance;
        return game != null && game.initialized && game.menuPanel.activeSelf && !game.inBattle && game.platform.CanPlay()
            && data.foodUpgrade < game.balance.foodUpgradeMaxLevel && money >= game.balance.FoodCost(data.foodUpgrade);
    }
    public void UpgradeFood()
    {
        if (!CanUpgradeFood()) return;
        money -= GameCore.instance.balance.FoodCost(data.foodUpgrade);
        data.foodUpgrade++;
        Save();
        ReloadData();
        GameCore.instance.sound.PlayClick();
    }

    public float BunkerHealth() { return GameCore.instance.balance.playerBunkerHealth * (1 + data.bunkerHealth * GameCore.instance.balance.upgradePower); }
    public float BunkerPower() { return GameCore.instance.balance.playerBunkerPower * (1 + data.bunkerPower * GameCore.instance.balance.upgradePower); }
    public float UnitHealth(int index) { return 1 + data.unitHealth[index] * GameCore.instance.balance.upgradePower; }
    public float UnitPower(int index) { return 1 + data.unitPower[index] * GameCore.instance.balance.upgradePower; }

    string UpgradeName(int index)
    {
        string[] ru = { "Бункер: здоровье", "Бункер: урон", "Пехота: здоровье", "Пехота: урон", "Метатель: здоровье", "Метатель: урон", "Тяжёлый: здоровье", "Тяжёлый: урон" };
        string[] en = { "Bunker: health", "Bunker: damage", "Infantry: health", "Infantry: damage", "Thrower: health", "Thrower: damage", "Heavy: health", "Heavy: damage" };
        return LocalText.english ? en[index] : ru[index];
    }
}
