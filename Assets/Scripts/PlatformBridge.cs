using UnityEngine;

/// <summary>
/// Native/offline platform behaviour. The App Store edition deliberately has no ads,
/// no external authentication, no remote leaderboard and no cloud-save dependency.
/// </summary>
public class PlatformBridge : MonoBehaviour
{
    public UnityEngine.UI.Text leaderboardView;
    public GameObject leaderboardPanel;
    bool focused = true;

    public bool DataReady { get { return true; } }
    public bool IsAdBusy { get { return false; } }

    public void CompleteInitialization() { SyncPause(); }
    public void ApplyEnvironmentLanguage() { }
    public bool RestoreProgress() { return false; }

    public bool CanPlay()
    {
        return GameCore.instance != null && GameCore.instance.initialized && !GameCore.instance.paused;
    }

    // Compatibility methods for existing serialized UnityEvents. Monetized SDK calls were removed.
    public void Reward(System.Action callback) { }
    public void Interstitial(System.Action next) { if (next != null) next(); }
    public void Save(string json) { }
    public void Score(int score) { }
    public void Gameplay(bool playing) { }

    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        if (leaderboardView != null && GameCore.instance != null)
            leaderboardView.text = LocalText.Pick("Локальный рекорд: ", "Local best: ")
                + GameCore.instance.economy.data.completedWave;
    }

    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    public void Authorize()
    {
        if (GameCore.instance != null)
            GameCore.instance.Message(LocalText.Pick("Вход недоступен в этой версии", "Sign-in is not available in this version"));
    }

    public void SyncPause()
    {
        if (GameCore.instance == null) return;
        bool pause = !focused || GameCore.instance.paused;
        Time.timeScale = pause ? 0 : 1;
        AudioListener.pause = pause;
    }

    void OnApplicationFocus(bool focus)
    {
        focused = focus;
        SyncPause();
    }
}
