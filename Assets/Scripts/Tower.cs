using UnityEngine;

public class Tower : MonoBehaviour
{
    public CombatEntity combatState;
    public int healthBase = 100;
    public int powerBase = 1;
    [Tooltip("Global maximum firing distance in world units. A weapon may have a shorter range.")]
    [Min(0)] public float attackRange;
    public float attackInterval = 0.5f;
    public SpriteRenderer bunkerSkin;
    public Projectile projectilePrefab;
    public Sprite projectileSprite;
    public Sprite fallbackWeapon;
    public SpriteRenderer[] weaponViews;
    float[] timers = new float[16];
    float enemyTimer;

    public void ResetTower(float health, bool active)
    {
        combatState.Setup(health);
        combatState.alive = active;
        combatState.body.enabled = active;
        bunkerSkin.enabled = active;
        if (combatState.healthBar != null) combatState.healthBar.transform.parent.gameObject.SetActive(active);
        for (int i = 0; i < timers.Length; i++) timers[i] = 0;
        enemyTimer = 0;
        RefreshWeapons(active);
    }

    void Update()
    {
        GameCore game = GameCore.instance;
        if (!game.inBattle || !combatState.alive) return;
        if (combatState.isEnemy)
        {
            enemyTimer -= Time.deltaTime;
            if (enemyTimer > 0) return;
            CombatEntity enemy = Target();
            if (enemy == null) return;
            enemyTimer = Mathf.Max(0.05f, attackInterval * game.balance.enemyBunkerInterval);
            Shoot(enemy, powerBase * game.balance.enemyBunkerDamage * (1 + game.wave * game.balance.enemyBunkerDamagePerWave), transform.position);
            return;
        }
        for (int i = 0; i < game.board.items.Length; i++)
        {
            LoadoutItem item = game.board.items[i];
            if (item.kind < 0 || item.kind >= game.board.weapons.Length || item.x < 0 || i >= timers.Length) continue;
            timers[i] -= Time.deltaTime;
            if (timers[i] > 0) continue;
            WeaponData weapon = game.board.weapons[item.kind];
            if (weapon == null) continue;
            CombatEntity enemy = Target(weapon.attackRange);
            if (enemy == null) continue;
            timers[i] = Mathf.Max(0.05f, weapon.interval * game.balance.playerWeaponInterval);
            Vector3 muzzle = weaponViews != null && i < weaponViews.Length && weaponViews[i] != null ? weaponViews[i].transform.position : transform.position;
            Shoot(enemy, weapon.power * game.economy.BunkerPower() * game.balance.playerWeaponDamage, muzzle, weapon);
        }
    }

    public float EffectiveRange(float weaponRange = 0)
    {
        GameBalance balance = GameCore.instance.balance;
        float configured = combatState.isEnemy ? balance.enemyBunkerRangeOverride : balance.playerBunkerRangeOverride;
        float globalRange = Mathf.Max(0, configured >= 0 ? configured : attackRange);
	    return weaponRange > 0 ? Mathf.Min(globalRange, weaponRange+attackRange) : globalRange;
    }

    CombatEntity Target(float weaponRange = 0)
    {
        CombatEntity result = null;
        float nearest = EffectiveRange(weaponRange);
        if (nearest <= 0) return null;
        foreach (CombatEntity other in CombatEntity.all)
        {
            if (other == null || !other.alive || other.isEnemy == combatState.isEnemy) continue;
            if (combatState.isEnemy && other.isTower) continue; // Enemy gun attacks the player army, not its bunker.
            float distance = other.Distance(transform.position);
            if (distance > nearest) continue;
            nearest = distance;
            result = other;
        }
        return result;
    }

    public void RefreshWeapons(bool active)
    {
        if (weaponViews == null) return;
        for (int i = 0; i < weaponViews.Length; i++)
        {
            SpriteRenderer view = weaponViews[i];
            if (view == null) continue;
            bool visible = active && !combatState.isEnemy && GameCore.instance.board.items != null && i < GameCore.instance.board.items.Length;
            LoadoutItem item = visible ? GameCore.instance.board.items[i] : null;
            visible = visible && item != null && item.kind >= 0 && item.kind < GameCore.instance.board.weapons.Length && item.x >= 0;
            visible = visible && GameCore.instance.board.weapons[item.kind] != null;
            view.enabled = visible;
            if (!visible) continue;
            WeaponData weapon = GameCore.instance.board.weapons[item.kind];
            view.sprite = weapon.sprite != null ? weapon.sprite : fallbackWeapon;
            if (view.sprite == null) { view.enabled = false; continue; }
            Vector2Int size = WeaponBoardView.Bounds(GameCore.instance.board.Shape(item.kind, item.rotation));
            view.transform.localPosition = new Vector3((item.x + size.x * 0.5f - 3) * 0.28f, (3 - item.y - size.y * 0.5f) * 0.28f + 0.2f, 0);
            view.transform.localRotation = Quaternion.identity;
            float length = Mathf.Max(view.sprite.bounds.size.x, view.sprite.bounds.size.y);
            view.transform.localScale = Vector3.one * Mathf.Max(0.1f, weapon.bunkerSize) / Mathf.Max(0.01f, length);
            view.color = Color.white;
            view.flipX = false;
        }
    }

    void Shoot(CombatEntity enemy, float power, Vector3 position, WeaponData weapon = null)
    {
        Projectile shot = Instantiate(projectilePrefab, position, Quaternion.identity, GameCore.instance.map.transform);
        Sprite sprite = weapon != null && weapon.projectileSprite != null ? weapon.projectileSprite : projectileSprite;
        if (sprite == null) sprite = Resources.Load<Sprite>("BunkerProjectileCircle");
        float speed = weapon != null ? weapon.projectileSpeed : 12;
        float size = weapon != null ? weapon.projectileSize : 0.24f;
        float arc = weapon != null && weapon.projectilePath == WeaponProjectilePath.Arc ? weapon.projectileArcHeight : 0;
        float spin = weapon != null ? weapon.projectileSpinSpeed : 0;
        shot.Setup(enemy, power, sprite, Mathf.Max(0.1f, speed), Mathf.Max(0.01f, size), arc, false, spin, true);
        shot.skin.color = weapon != null ? weapon.projectileColor : Color.white;
        shot.skin.sortingLayerID = bunkerSkin.sortingLayerID;
        int sortingOrder = bunkerSkin.sortingOrder;
        if (weaponViews != null)
            foreach (SpriteRenderer view in weaponViews)
                if (view != null && view.sortingLayerID == shot.skin.sortingLayerID)
                    sortingOrder = Mathf.Max(sortingOrder, view.sortingOrder);
        shot.skin.sortingOrder = Mathf.Max(shot.skin.sortingOrder, sortingOrder + 1);
        GameCore.instance.sound.PlayAttack();
    }
}
