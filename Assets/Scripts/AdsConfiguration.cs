using UnityEngine;

[CreateAssetMenu(fileName = "AdsConfiguration", menuName = "Bunker/Ads Configuration")]
public sealed class AdsConfiguration : ScriptableObject
{
    [SerializeField] string interstitialAdUnitId = "R-M-20415900-1";
    [SerializeField] string rewardedAdUnitId = "R-M-20415900-2";
    [Min(1)] public int rewardedCoins = 30;
    [Range(0, 3)] public int retryCount = 2;
    [Min(1)] public float retryDelaySeconds = 5;

    public string InterstitialId { get { return Unit(interstitialAdUnitId, "demo-interstitial-yandex"); } }
    public string RewardedId { get { return Unit(rewardedAdUnitId, "demo-rewarded-yandex"); } }

    static string Unit(string production, string demo)
    {
#if UNITY_EDITOR
        return demo;
#else
        return production;
#endif
    }
}
