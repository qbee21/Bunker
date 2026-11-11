using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform spawnPoint;
    public int fore;
    public int interval;
    public bool autoSpawn;
    public bool isEnemy;

    public void SpawnEnemy(int index, bool boss)
    {
        GameCore game = GameCore.instance;
        EntityData[] units = game.entitys[game.enemyLevel].entity;
        Entity unit = Instantiate(game.entityPrefab, spawnPoint.position, Quaternion.identity, game.map.transform).GetComponent<Entity>();
        float healthScale = 1 + (game.wave - 1) * game.balance.enemyHealthGrowth;
        float damageScale = 1 + (game.wave - 1) * game.balance.enemyDamageGrowth;
        if (boss)
        {
            healthScale *= game.balance.bossHealth;
            damageScale *= game.balance.bossDamage;
        }
        unit.Setup(units[Mathf.Clamp(index, 0, units.Length - 1)], true, healthScale, damageScale, boss);
    }

    public void Spawn()
    {
        if (GameCore.instance.inBattle && isEnemy) SpawnEnemy(Random.Range(0, 3), false);
    }

    public void SpawnWithIndex(int index)
    {
        GameCore game = GameCore.instance;
        Entity unit = Instantiate(game.entityPrefab, spawnPoint.position, Quaternion.identity, game.map.transform).GetComponent<Entity>();
        unit.Setup(game.entitys[game.level].entity[index], false, game.economy.UnitHealth(index), game.economy.UnitPower(index), false);
    }
}
