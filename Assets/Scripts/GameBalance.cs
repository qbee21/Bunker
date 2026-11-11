using UnityEngine;

[System.Serializable]
public class WaveTuning
{
    [Min(1)] public int firstWave = 1;
    [Min(1)] public int lastWave = 1;
    [Tooltip("-1 keeps the normal count. Bosses are counted separately.")]
    public int regularEnemyCount = -1;
    [Min(0.01f)] public float health = 1;
    [Min(0.01f)] public float damage = 1;
    [Min(0.01f)] public float count = 1;
    [Min(0.01f)] public float spawnInterval = 1;
    [Min(0)] public float coins = 1;
    [Min(0.01f)] public float bunkerHealth = 1;
}

[CreateAssetMenu(fileName = "GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("Progression")]
    public int startingCoins = 60;
    [Min(1)] public int wavesPerEra = 5;
    [Min(1)] public int campaignWaves = 50;
    [Min(1)] public int maxAlliedUnits = 20;
    [Header("Food: base and upgrade limits")]
    public int startingFood = 12;
    public int maxFood = 60;
    [Min(0.05f)] public float secondsPerFood = 0.5f;
    [Min(0)] public int foodUpgradeMaxLevel = 12;
    public int foodCapacityPerLevel = 4;
    public int foodCapacityLimit = 108;
    public int startingFoodPerLevel = 1;
    public int startingFoodLimit = 24;
    [Min(0)] public float foodIntervalReduction = 0.02f;
    [Min(0.05f)] public float foodMinimumInterval = 0.25f;
    public int foodUpgradePrice = 20;
    [Min(1)] public float foodUpgradePriceGrowth = 1.15f;
    public int foodUpgradePriceMin = 20;
    public int foodUpgradePriceMax = 300;
    [Header("Player bunker (Tower range stays unchanged unless override >= 0)")]
    [Min(1)] public float playerBunkerHealth = 100;
    [Min(0)] public float playerBunkerPower = 1;
    [Min(0.01f)] public float playerWeaponDamage = 1;
    [Min(0.05f)] public float playerWeaponInterval = 1;
    public float playerBunkerRangeOverride = -1;
    [Header("Wave difficulty: applies to configured AND generated waves")]
    [Min(0.01f)] public float localDifficulty = 1;
    [Min(0.01f)] public float enemyHealthMultiplier = 1;
    [Min(0.01f)] public float enemyDamageMultiplier = 1;
    [Min(0.01f)] public float enemyCountMultiplier = 1;
    [Min(0.05f)] public float enemySpawnIntervalMultiplier = 1;
    [Min(1)] public int maxConcurrentEnemies = 14;
    public WaveTuning[] waveOverrides = new WaveTuning[0];
    [Header("Generated waves (after the configured list)")]
    public bool countFromLocalEraWave = true;
    public int firstWaveEnemies = 6;
    public int extraEnemies = 2;
    public int extraEnemiesPerEra = 3;
    public int maxEnemies = 30;
    [Min(0.1f)] public float spawnInterval = 2.5f;
    [Min(0.1f)] public float minimumSpawnInterval = 1;
    [Min(0)] public float spawnIntervalReduction = 0.03f;
    [Tooltip("Maximum gap between remaining defenders after the enemy bunker is destroyed; alive enemy cap still applies.")]
    [Min(0.05f)] public float destroyedBunkerSpawnInterval = 0.25f;
    public bool growthFromLocalEraWave = true;
    [Min(0.01f)] public float generatedHealthBase = 1.15f;
    [Min(0.01f)] public float generatedDamageBase = 1.05f;
    [Min(0)] public float enemyHealthGrowth = 0.04f;
    [Min(0)] public float enemyDamageGrowth = 0.03f;
    [Min(0)] public float enemyHealthPerEra = 0.04f;
    [Min(0)] public float enemyDamagePerEra = 0.03f;
    [Min(0.01f)] public float generatedHealthCap = 2.5f;
    [Min(0.01f)] public float generatedDamageCap = 2;
    [Tooltip("Infantry, thrower, heavy. Zero weight excludes a role.")]
    public Vector3 enemyRoleWeights = new Vector3(5, 3, 2);
    [Header("Boss")]
    [Min(0)] public int bossEvery = 5;
    [Min(0)] public int bossEraOffset = 0;
    [Min(0.01f)] public float bossHealth = 3;
    [Min(0.01f)] public float bossDamage = 1.4f;
    [Header("Enemy bunker")]
    [Min(0)] public int enemyBunkerEvery = 3;
    [Min(1)] public float enemyBunkerHealthBase = 35;
    [Min(0)] public float enemyBunkerHealthPerWave = 5;
    [Min(0.01f)] public float enemyBunkerHealthMultiplier = 1;
    [Min(0.01f)] public float enemyBunkerDamage = 1;
    [Min(0)] public float enemyBunkerDamagePerWave = 0.03f;
    [Min(0.05f)] public float enemyBunkerInterval = 1;
    public float enemyBunkerRangeOverride = -1;
    [Header("Economy and recovery")]
    public int waveCoins = 20;
    public int waveCoinsPerWave = 3;
    [Min(0)] public float waveRewardMultiplier = 1.25f;
    public int dailyCoins = 100;
    public int upgradePrice = 25;
    [Min(1)] public float upgradePriceGrowth = 1.15f;
    [Min(0)] public float upgradePower = 0.15f;
    [Range(0, 1)] public float coinChance = 0.8f;
    [Min(0)] public float coinRewardMultiplier = 1;
    [Min(0)] public float bossCoinMultiplier = 12;
    public int defeatCoins = 5;
    public int defeatCoinsPerEra = 2;
    [Min(1)] public int defeatMinimumKills = 3;
    [Min(0)] public float defeatMinimumSeconds = 20;
    public int Era(int wave) { return Mathf.Max(0, wave - 1) / Mathf.Max(1, wavesPerEra); }
    public int LocalWave(int wave) { return Mathf.Max(0, wave - 1) % Mathf.Max(1, wavesPerEra); }
    public static float Round2(float value) { return Mathf.Round(value * 100) / 100; }
    public WaveTuning Tuning(int wave)
    {
        if (waveOverrides != null)
            for (int i = waveOverrides.Length - 1; i >= 0; i--)
            {
                WaveTuning item = waveOverrides[i];
                if (item != null && wave >= item.firstWave && wave <= Mathf.Max(item.firstWave, item.lastWave)) return item;
            }
        return null;
    }
    public float HealthScale(int wave, float configured = -1)
    {
        int step = growthFromLocalEraWave ? LocalWave(wave) : Mathf.Max(0, wave - 1);
        float raw = configured >= 0 ? configured : Mathf.Min(generatedHealthCap, generatedHealthBase + step * enemyHealthGrowth + Era(wave) * enemyHealthPerEra);
        WaveTuning t = Tuning(wave);
        return Round2(Mathf.Max(0.01f, raw * Mathf.Max(0.01f, localDifficulty) * Mathf.Max(0.01f, enemyHealthMultiplier) * (t != null ? Mathf.Max(0.01f, t.health) : 1)));
    }
    public float DamageScale(int wave, float configured = -1)
    {
        int step = growthFromLocalEraWave ? LocalWave(wave) : Mathf.Max(0, wave - 1);
        float raw = configured >= 0 ? configured : Mathf.Min(generatedDamageCap, generatedDamageBase + step * enemyDamageGrowth + Era(wave) * enemyDamagePerEra);
        WaveTuning t = Tuning(wave);
        return Round2(Mathf.Max(0.01f, raw * Mathf.Max(0.01f, localDifficulty) * Mathf.Max(0.01f, enemyDamageMultiplier) * (t != null ? Mathf.Max(0.01f, t.damage) : 1)));
    }
    public int EnemyCount(int wave, int configured = -1)
    {
        WaveTuning t = Tuning(wave);
        int step = countFromLocalEraWave ? LocalWave(wave) : Mathf.Max(0, wave - 1);
        float raw = configured >= 0 ? configured : firstWaveEnemies + step * extraEnemies + Era(wave) * extraEnemiesPerEra;
        int count = t != null && t.regularEnemyCount >= 0 ? t.regularEnemyCount : Mathf.RoundToInt(raw * Mathf.Max(0.01f, enemyCountMultiplier) * (t != null ? Mathf.Max(0.01f, t.count) : 1));
        return configured >= 0 ? Mathf.Clamp(count, 0, 500) : Mathf.Clamp(count, 1, Mathf.Max(1, maxEnemies));
    }
    public float SpawnDelay(int wave, float configured = -1)
    {
        float raw = configured >= 0 ? configured : Mathf.Max(minimumSpawnInterval, spawnInterval - LocalWave(wave) * spawnIntervalReduction);
        WaveTuning t = Tuning(wave);
        return Round2(Mathf.Max(0.1f, raw * Mathf.Max(0.05f, enemySpawnIntervalMultiplier) * (t != null ? Mathf.Max(0.01f, t.spawnInterval) : 1)));
    }
    public int VictoryCoins(int wave, int configured = -1)
    {
        WaveTuning t = Tuning(wave);
        float raw = configured >= 0 ? configured : waveCoins + wave * (float)waveCoinsPerWave;
        return Mathf.Clamp(Mathf.RoundToInt(raw * Mathf.Max(0, waveRewardMultiplier) * (t != null ? Mathf.Max(0, t.coins) : 1)), 0, 1000000);
    }
    public float BunkerHealth(int wave, float configured = -1)
    {
        WaveTuning t = Tuning(wave);
        float raw = configured >= 0 ? configured : enemyBunkerHealthBase + wave * enemyBunkerHealthPerWave;
        return Round2(Mathf.Max(1, raw * Mathf.Max(0.01f, enemyBunkerHealthMultiplier) * (t != null ? Mathf.Max(0.01f, t.bunkerHealth) : 1)));
    }
    public int FoodLevel(int rank) { return Mathf.Clamp(rank, 0, Mathf.Clamp(foodUpgradeMaxLevel, 0, 1000)); }
    public int FoodCapacity(int rank) { return Mathf.Clamp(maxFood + FoodLevel(rank) * Mathf.Max(0, foodCapacityPerLevel), Mathf.Max(1, maxFood), Mathf.Max(maxFood, foodCapacityLimit)); }
    public int StartingFood(int rank) { return Mathf.Clamp(startingFood + FoodLevel(rank) * Mathf.Max(0, startingFoodPerLevel), 0, Mathf.Min(FoodCapacity(rank), Mathf.Max(startingFood, startingFoodLimit))); }
    public float FoodInterval(int rank) { return Round2(Mathf.Max(0.05f, Mathf.Max(foodMinimumInterval, secondsPerFood - FoodLevel(rank) * Mathf.Max(0, foodIntervalReduction)))); }
    public int FoodCost(int rank)
    {
        float cost = Mathf.Max(0, foodUpgradePrice) * Mathf.Pow(Mathf.Max(1, foodUpgradePriceGrowth), FoodLevel(rank));
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(1000000, cost)), Mathf.Max(0, foodUpgradePriceMin), Mathf.Max(foodUpgradePriceMin, foodUpgradePriceMax));
    }
}
