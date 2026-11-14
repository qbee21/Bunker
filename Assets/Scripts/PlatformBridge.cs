using UnityEngine;

/// <summary>
/// Native iOS platform behaviour. Native full-screen ads are delegated to AdsService.
/// </summary>
public class PlatformBridge : MonoBehaviour
{
    public UnityEngine.UI.Text leaderboardView;
    public GameObject leaderboardPanel;
    bool focused = true;

    public bool DataReady { get { return true; } }
    public bool IsAdBusy { get { return AdsService.Instance.IsBusy; } }
    public bool IsRewardedReady { get { return AdsService.Instance.IsRewardedReady; } }
    public int RewardCoins { get { return AdsService.Instance.RewardCoins; } }

    public void CompleteInitialization() { SyncPause(); AdsService.Instance.Initialize(); }
    public void ApplyEnvironmentLanguage() { }
    public bool RestoreProgress() { return false; }

    public bool CanPlay()
    {
        return GameCore.instance != null && GameCore.instance.initialized && !GameCore.instance.paused && !IsAdBusy;
    }

    public void Reward(System.Action callback) { AdsService.Instance.ShowRewarded(callback, null); }
    public void Interstitial(System.Action next) { AdsService.Instance.ShowInterstitial(next); }
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
