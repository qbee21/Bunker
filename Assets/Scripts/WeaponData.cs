using UnityEngine;

public enum WeaponProjectilePath { Straight, Arc }

[CreateAssetMenu(fileName = "Weapon")]
public class WeaponData : ScriptableObject
{
	public Sprite sprite;
	public int accessInEra;
    // Old saved weapons keep their indices; retired entries can be excluded from grants.
    public bool grantInPreparation = true;
    [Tooltip("Optional weapon range; 0 uses Tower.attackRange. Tower range is the global maximum.")]
    public float attackRange;
    [Header("Bunker projectile")]
    public Sprite projectileSprite;
    public Color projectileColor = Color.white;
    public WeaponProjectilePath projectilePath = WeaponProjectilePath.Straight;
    [Min(0.1f)] public float projectileSpeed = 12;
    [Min(0)] public float projectileArcHeight = 0.7f;
    [Tooltip("Degrees per second; 0 disables spinning.")]
    public float projectileSpinSpeed = 540;
    [Min(0.01f)] public float projectileSize = 0.24f;
    public float bunkerSize = 0.5f;
    public string titleRu;
    public string titleEn;
    public Vector2Int[] cells;
    public float power = 2;
    public float interval = 0.7f;
    public Color color = Color.yellow;
}
