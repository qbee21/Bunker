using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public Spawner enemySpawner;
    public EnemyWave[] waves = new EnemyWave[0];
    public bool endlessAfterLast = true;
    [Tooltip("Legacy fields retained for scene compatibility. GameBalance now controls generated scaling.")]
    public float endlessHealthStep = 0.1f;
    public float endlessDamageStep = 0.075f;
    public int bossEvery = 5;
    public int maxAlive = 30;
    public bool finishedSpawning;
    public int remaining;
    public int alive;
    public bool hasBunker;
    public float bunkerHealth;
    public int victoryCoins;
    public int currentWave;
    public int killed;
    public float BattleSeconds { get { return Mathf.Max(0, Time.time - battleStarted); } }
    float battleStarted;
    int concurrentLimit;
    int[] groupCounts;
    Coroutine routine;
    bool acceleratedSpawn;

	public bool CanStart(int number)
	{
		return number > 0 && (number <= waves.Length || endlessAfterLast);
	}
	public void Begin(int number)
    {
        Stop();
        currentWave = Mathf.Max(1, number);
        alive = 0;
        killed = 0;
        battleStarted = Time.time;
        finishedSpawning = false;
        acceleratedSpawn = false;
        GameBalance balance = GameCore.instance.balance;
        concurrentLimit = Mathf.Max(1, balance.maxConcurrentEnemies);
        EnemyWave settings = currentWave <= waves.Length ? waves[currentWave - 1] : null;
        if (settings != null)
        {
            hasBunker = balance.enemyBunkerEvery == 1 || settings.enemyBunker;
            bunkerHealth = balance.BunkerHealth(currentWave, settings.bunkerHealth);
            victoryCoins = balance.VictoryCoins(currentWave, settings.victoryCoins);
            BuildGroupCounts(settings, balance);
            routine = StartCoroutine(SpawnConfigured(settings));
        }
        else
        {
            hasBunker = balance.enemyBunkerEvery > 0 && currentWave % balance.enemyBunkerEvery == 0;
            bunkerHealth = balance.BunkerHealth(currentWave);
            victoryCoins = balance.VictoryCoins(currentWave);
            remaining = balance.EnemyCount(currentWave);
            routine = StartCoroutine(SpawnEndless());
        }
    }
    void BuildGroupCounts(EnemyWave settings, GameBalance balance)
    {
        EnemyGroup[] groups = settings.groups ?? new EnemyGroup[0];
        groupCounts = new int[groups.Length];
        int normalTotal = 0;
        foreach (EnemyGroup g in groups) if (g != null && g.unit != null && !g.boss) normalTotal += Mathf.Max(0, g.count);
        int desired = normalTotal > 0 ? balance.EnemyCount(currentWave, normalTotal) : 0;
        int cumulative = 0;
        int assigned = 0;
        remaining = 0;
        for (int i = 0; i < groups.Length; i++)
        {
            EnemyGroup group = groups[i];
            if (group == null || group.unit == null) continue;
            if (group.boss) groupCounts[i] = Mathf.Clamp(group.count, 0, 100);
            else if (normalTotal > 0)
            {
                cumulative += Mathf.Max(0, group.count);
                int target = Mathf.RoundToInt(desired * (float)cumulative / normalTotal);
                groupCounts[i] = target - assigned;
                assigned = target;
            }
            remaining += groupCounts[i];
        }
    }
    IEnumerator SpawnConfigured(EnemyWave settings)
    {
        GameBalance balance = GameCore.instance.balance;
        EnemyGroup[] groups = settings.groups ?? new EnemyGroup[0];
        for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            EnemyGroup group = groups[groupIndex];
            if (group == null || group.unit == null || groupCounts[groupIndex] <= 0) continue;
            if (group.boss) while (alive > 0 && !acceleratedSpawn) yield return null;
            if (group.delay > 0) yield return WaitForSpawnDelay(group.delay);
            for (int i = 0; i < groupCounts[groupIndex]; i++)
            {
                while (alive >= concurrentLimit) yield return null;
                EntityData data = group.unit;
                if (group.boss)
                {
                    // Configured waves use the same boss-era offset as generated waves.
                    GameCore game = GameCore.instance;
                    int era = Mathf.Clamp(game.enemyLevel + balance.bossEraOffset, 0, game.entitys.Length - 1);
                    data = game.entitys[era].entity[2];
                }
                Create(data, balance.HealthScale(currentWave, settings.healthScale), balance.DamageScale(currentWave, settings.damageScale), group.boss);
                remaining--;
                if (remaining > 0) yield return WaitForSpawnDelay(balance.SpawnDelay(currentWave, group.interval));
            }
        }
        finishedSpawning = true;
    }
    IEnumerator SpawnEndless()
    {
        GameCore game = GameCore.instance;
        GameBalance balance = game.balance;
        int total = remaining;
        for (int i = 0; i < total; i++)
        {
            while (alive >= concurrentLimit) yield return null;
            bool boss = balance.bossEvery > 0 && currentWave % balance.bossEvery == 0 && i == total - 1;
            if (boss) while (alive > 0 && !acceleratedSpawn) yield return null;
            int index = boss ? 2 : PickRole(balance.enemyRoleWeights);
            int era = Mathf.Clamp(game.enemyLevel + (boss ? balance.bossEraOffset : 0), 0, game.entitys.Length - 1);
            Create(game.entitys[era].entity[index], balance.HealthScale(currentWave), balance.DamageScale(currentWave), boss);
            remaining--;
            if (remaining > 0) yield return WaitForSpawnDelay(balance.SpawnDelay(currentWave));
        }
        finishedSpawning = true;
    }
    // Called only by the actual enemy-tower death event, not by its stale state at Begin.
    public void OnEnemyBunkerDestroyed()
    {
        if (hasBunker && !finishedSpawning) acceleratedSpawn = true;
    }
    IEnumerator WaitForSpawnDelay(float normalDelay)
    {
        float elapsed = 0;
        normalDelay = Mathf.Max(0, normalDelay);
        while (true)
        {
            float delay = acceleratedSpawn
                ? Mathf.Min(normalDelay, Mathf.Max(0.05f, GameCore.instance.balance.destroyedBunkerSpawnInterval))
                : normalDelay;
            if (elapsed >= delay) yield break;
            yield return null;
            elapsed += Time.deltaTime; // Scaled time: manual pause and ads do not drain the wait.
        }
    }
    public static int PickRole(Vector3 weights)
    {
        float a = Mathf.Max(0, weights.x), b = Mathf.Max(0, weights.y), c = Mathf.Max(0, weights.z);
        float total = a + b + c;
        if (total <= 0) return 0;
        float value = Random.value * total;
        if (value < a) return 0;
        return value < a + b ? 1 : 2;
    }
    void Create(EntityData data, float health, float damage, bool boss)
    {
        GameCore game = GameCore.instance;
        if (boss) { health *= game.balance.bossHealth; damage *= game.balance.bossDamage; }
        Vector3 position = enemySpawner.spawnPoint.position + (game.pointTower.transform.position - enemySpawner.spawnPoint.position).normalized * 0.9f;
        Entity unit = Instantiate(game.entityPrefab, position, Quaternion.identity, game.map.transform).GetComponent<Entity>();
        unit.Setup(data, true, GameBalance.Round2(health), GameBalance.Round2(damage), boss);
        alive++;
    }
    public void EnemyDied() { alive = Mathf.Max(0, alive - 1); killed++; }
    public bool Cleared() { return finishedSpawning && alive == 0; }
    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        finishedSpawning = true;
        remaining = 0;
    }
}
