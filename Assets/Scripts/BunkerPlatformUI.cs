using UnityEngine;
using UnityEngine.UI;

// Updates existing controls and creates one clean rewarded-ad button at runtime.
public class BunkerPlatformUI : MonoBehaviour
{
    GameCore game;
    Button foodUpgrade;
    Text foodLabel;
    Button rewardedCoins;
    Text rewardedLabel;

    public void Initialize(GameCore value)
    {
        if (game != null) return;
        game = value;
        foreach (Button button in game.menuPanel.transform.root.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.tag == "FoodButton")
            {
                foodUpgrade = button;
                foodLabel = button.GetComponentInChildren<Text>(true);
            }
            if (IsRemovedPlatformControl(button)) button.gameObject.SetActive(false);
        }
        CreateRewardedButton();
        Refresh();
    }

    void CreateRewardedButton()
    {
        if (game.economy == null || game.economy.moneyView == null) return;
        Transform parent = game.economy.moneyView.transform.parent;
        if (parent == null) return;
        GameObject view = new GameObject("RewardedCoinsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        view.transform.SetParent(parent, false);
        RectTransform rect = view.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(90, -28);
        rect.sizeDelta = new Vector2(179, 54);
        Image background = view.GetComponent<Image>();
        Image source = parent.GetComponent<Image>();
        if (source != null) { background.sprite = source.sprite; background.type = Image.Type.Sliced; }
        background.color = new Color(1f, 0.746f, 0f, 1f);
        rewardedCoins = view.GetComponent<Button>();
        rewardedCoins.onClick.AddListener(game.ShowRewardedCoins);
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(view.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        rewardedLabel = label.GetComponent<Text>();
        rewardedLabel.font = game.economy.moneyView.font;
        rewardedLabel.fontSize = 18;
        rewardedLabel.alignment = TextAnchor.MiddleCenter;
        rewardedLabel.color = Color.white;
        rewardedLabel.raycastTarget = false;
    }

    static bool IsRemovedPlatformControl(Button button)
    {
        string name = button.gameObject.name;
        if (name == "ad_Button" || name == "RewardButton" || name == "RewardExtension" || name == "RewardWeapon"
            || name == "RewardFood" || name == "Telegram" || name == "Authorize") return true;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            string method = button.onClick.GetPersistentMethodName(i);
            if (method == "RewardMenuCoins" || method == "RewardFood" || method == "RewardCells" || method == "RewardWeapon"
                || method == "Telegram" || method == "Authorize") return true;
        }
        return false;
    }
    void LateUpdate() { if (game != null) Refresh(); }
    void Refresh()
    {
        bool visible = game.initialized && !game.inBattle && (game.menuPanel.activeSelf || game.prepPanel.activeSelf);
        if (foodUpgrade != null)
        {
            foodUpgrade.gameObject.SetActive(game.initialized && game.menuPanel.activeSelf && !game.inBattle);
            int rank = game.economy.data.foodUpgrade;
            bool maxed = rank >= game.balance.foodUpgradeMaxLevel;
            if (foodLabel != null)
                foodLabel.text = " +1 / " + game.economy.FoodInterval().ToString("0.##") + LocalText.Pick(" с", " s")
                    + "\n" + (maxed ? "MAX" : LocalText.Pick("Улучшить: ", "Upgrade: ") + game.balance.FoodCost(rank) + LocalText.Pick(" монет", " coins"));
            foodUpgrade.interactable = game.economy.CanUpgradeFood();
        }
        if (rewardedCoins != null)
        {
            rewardedCoins.gameObject.SetActive(visible);
            rewardedCoins.interactable = game.CanShowRewardedCoins();
            if (rewardedLabel != null)
                rewardedLabel.text = game.platform != null && game.platform.IsAdBusy
                    ? LocalText.Pick("Реклама...", "Loading...")
                    : "▶ +" + (game.platform != null ? game.platform.RewardCoins : 30) + LocalText.Pick(" монет", " coins");
        }
    }
}
