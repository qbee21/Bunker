using UnityEngine;

[CreateAssetMenu(fileName = "NewEntity")]
public class EntityData : ScriptableObject
{
    public Sprite skin;
    public Color enemyCol = Color.white;
    public float health = 10;
    public float speed = 1;
    public float power = 1;
    public float cooldown = 1;
    public int price = 3;
    public float attackRange = 0.8f;
    public bool ranged;
    public Sprite projectileSprite;
    public Color projectileColor = Color.white;
    public float projectileSpeed = 7;
    public float projectileSize = 0.18f;
    public float projectileArc = 0.4f;
    public int coinReward = 2;
    public bool killsRegularUnitsInOneHit;
    public bool diesFromOneHit;
}
