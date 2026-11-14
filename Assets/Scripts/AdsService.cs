using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
#if UNITY_IOS || UNITY_EDITOR
using YandexMobileAds;
using YandexMobileAds.Base;
#endif

/// <summary>Persistent coordinator for native iOS full-screen ads.</summary>
public sealed class AdsService : MonoBehaviour
{
    static AdsService instance;
    AdsConfiguration config;
    bool initialized;
    bool presenting;
    bool interstitialLoading;
    bool rewardedLoading;
    int interstitialRetries;
    int rewardedRetries;
    Action interstitialDone;
    Action rewardAction;
    Action rewardDone;
    bool rewardGranted;

#if UNITY_IOS || UNITY_EDITOR
    InterstitialAdLoader interstitialLoader;
    RewardedAdLoader rewardedLoader;
    Interstitial interstitial;
    RewardedAd rewardedAd;
#endif

    public static AdsService Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindObjectOfType<AdsService>();
            if (instance != null) return instance;
            instance = new GameObject("AdsService").AddComponent<AdsService>();
            return instance;
        }
    }

    public bool IsBusy { get { return presenting; } }
    public bool IsRewardedReady
    {
        get
        {
#if UNITY_IOS || UNITY_EDITOR
            return initialized && !presenting && rewardedAd != null;
#else
            return false;
#endif
        }
    }
    public int RewardCoins { get { return config != null ? Mathf.Max(1, config.rewardedCoins) : 30; } }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        if (initialized) return;
        config = Resources.Load<AdsConfiguration>("AdsConfiguration");
        if (config == null) { DevelopmentLog("configuration missing"); return; }
        initialized = true;
#if UNITY_IOS || UNITY_EDITOR
        interstitialLoader = new InterstitialAdLoader();
        rewardedLoader = new RewardedAdLoader();
        LoadInterstitial();
        LoadRewarded();
#endif
    }

    public void ShowInterstitial(Action completed)
    {
        Initialize();
#if UNITY_IOS || UNITY_EDITOR
        if (presenting || interstitial == null) { Complete(completed); LoadInterstitial(); return; }
        presenting = true;
        interstitialDone = completed;
        Interstitial ad = interstitial;
        interstitial = null;
#if UNITY_EDITOR
        // Editor dummy ads have no close callback. Do not block the result screen.
        ad.Destroy();
        FinishInterstitial();
#else
        ad.OnAdDismissed += (sender, args) => FinishInterstitial(ad);
        ad.OnAdFailedToShow += (sender, args) => FinishInterstitial(ad);
        try { ad.Show(); }
        catch (Exception error) { DevelopmentLog("interstitial show failed: " + error.GetType().Name); FinishInterstitial(ad); }
#endif
#else
        Complete(completed);
#endif
    }

    public void ShowRewarded(Action onRewarded, Action completed)
    {
        Initialize();
#if UNITY_IOS || UNITY_EDITOR
        if (presenting || rewardedAd == null) { Complete(completed); LoadRewarded(); return; }
        presenting = true;
        rewardAction = onRewarded;
        rewardDone = completed;
        rewardGranted = false;
        RewardedAd ad = rewardedAd;
        rewardedAd = null;
#if UNITY_EDITOR
        // The Editor uses the SDK demo ID but never gives a synthetic reward.
        ad.Destroy();
        FinishRewarded();
#else
        ad.OnRewarded += (sender, args) => GrantReward();
        ad.OnAdDismissed += (sender, args) => FinishRewarded(ad);
        ad.OnAdFailedToShow += (sender, args) => FinishRewarded(ad);
        try { ad.Show(); }
        catch (Exception error) { DevelopmentLog("rewarded show failed: " + error.GetType().Name); FinishRewarded(ad); }
#endif
#else
        Complete(completed);
#endif
    }

#if UNITY_IOS || UNITY_EDITOR
    void LoadInterstitial()
    {
        if (!initialized || presenting || interstitialLoading || interstitial != null) return;
        interstitialLoading = true;
        try { interstitialLoader.LoadAd(new AdRequest(config.InterstitialId), OnInterstitialLoaded, OnInterstitialFailed); }
        catch (Exception error) { OnInterstitialLoadException(error); }
    }

    void LoadRewarded()
    {
        if (!initialized || presenting || rewardedLoading || rewardedAd != null) return;
        rewardedLoading = true;
        try { rewardedLoader.LoadAd(new AdRequest(config.RewardedId), OnRewardedLoaded, OnRewardedFailed); }
        catch (Exception error) { OnRewardedLoadException(error); }
    }

    void OnInterstitialLoaded(Interstitial ad)
    {
        interstitialLoading = false;
        interstitialRetries = 0;
        if (presenting) { ad.Destroy(); return; }
        interstitial = ad;
    }

    void OnInterstitialFailed(AdFailedToLoadEventArgs args)
    {
        interstitialLoading = false;
        Retry(ref interstitialRetries, LoadInterstitial);
    }

    void OnInterstitialLoadException(Exception error)
    {
        DevelopmentLog("interstitial load failed: " + error.GetType().Name);
        interstitialLoading = false;
        Retry(ref interstitialRetries, LoadInterstitial);
    }

    void OnRewardedLoaded(RewardedAd ad)
    {
        rewardedLoading = false;
        rewardedRetries = 0;
        if (presenting) { ad.Destroy(); return; }
        rewardedAd = ad;
    }

    void OnRewardedFailed(AdFailedToLoadEventArgs args)
    {
        rewardedLoading = false;
        Retry(ref rewardedRetries, LoadRewarded);
    }

    void OnRewardedLoadException(Exception error)
    {
        DevelopmentLog("rewarded load failed: " + error.GetType().Name);
        rewardedLoading = false;
        Retry(ref rewardedRetries, LoadRewarded);
    }

    void FinishInterstitial(Interstitial ad = null)
    {
        if (!presenting) return;
        if (ad != null) ad.Destroy();
        presenting = false;
        Action done = interstitialDone;
        interstitialDone = null;
        Complete(done);
        LoadInterstitial();
        LoadRewarded();
    }

    void GrantReward()
    {
        if (rewardGranted) return;
        rewardGranted = true;
        Action grant = rewardAction;
        rewardAction = null;
        Complete(grant);
    }

    void FinishRewarded(RewardedAd ad = null)
    {
        if (!presenting) return;
        if (ad != null) ad.Destroy();
        presenting = false;
        rewardAction = null;
        Action done = rewardDone;
        rewardDone = null;
        Complete(done);
        LoadRewarded();
        LoadInterstitial();
    }
#endif

    void Retry(ref int retries, Action load)
    {
        if (config == null || retries >= config.retryCount) return;
        retries++;
        StartCoroutine(RetryAfterDelay(load));
    }

    IEnumerator RetryAfterDelay(Action load)
    {
        yield return new WaitForSecondsRealtime(config.retryDelaySeconds);
        if (!presenting) load();
    }

    static void Complete(Action action)
    {
        if (action == null) return;
        try { action(); }
        catch (Exception error) { DevelopmentLog("callback failed: " + error.GetType().Name); }
    }

    [Conditional("DEVELOPMENT_BUILD")]
    [Conditional("UNITY_EDITOR")]
    static void DevelopmentLog(string message) { UnityEngine.Debug.Log("[Ads] " + message); }
}
