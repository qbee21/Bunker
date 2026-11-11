using UnityEngine;
using UnityEngine.UI;

// Updates existing scene controls. Does not create UI objects or change their layout.
public class BunkerPlatformUI : MonoBehaviour
{
    GameCore game;
    Button foodUpgrade;
    Text foodLabel;

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
        Refresh();
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
    }
}
