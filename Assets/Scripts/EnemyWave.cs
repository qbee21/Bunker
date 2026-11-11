using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    public EntityData unit;
    public int count = 3;
    public float delay;
    public float interval = 2;
    public bool boss;
}

[CreateAssetMenu(fileName = "EnemyWave")]
public class EnemyWave : ScriptableObject
{
    public EnemyGroup[] groups = new EnemyGroup[0];
    public float healthScale = 1;
    public float damageScale = 1;
    public bool enemyBunker;
    public float bunkerHealth = 60;
    public int victoryCoins = 15;
}
