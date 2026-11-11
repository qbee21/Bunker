using UnityEngine;

public class Entity : MonoBehaviour
{
    public EntityData entityData;
    public CombatEntity combatState;
    public Rigidbody2D rb;
	public SpriteRenderer skin;
	public GameObject bower;
    public float attackRange = 1;
    public float power;
    public float speed;
    public float cooldown;
    public Projectile projectilePrefab;
    CombatEntity currentTarget;
    float nextAttack;

    public void Setup(EntityData data, bool enemy, float healthScale, float damageScale, bool boss)
    {
        entityData = data;
        combatState.isEnemy = enemy;
        combatState.coins = Mathf.Max(0, Mathf.RoundToInt(data.coinReward * GameCore.instance.balance.coinRewardMultiplier * (boss ? GameCore.instance.balance.bossCoinMultiplier : 1)));
        combatState.isBoss = boss;
        combatState.diesFromOneHit = data.diesFromOneHit;
        combatState.Setup(CombatRules.StartingHealth(data.health, healthScale, data.diesFromOneHit));
        skin.sprite = data.skin;
        skin.color = enemy ? data.enemyCol : Color.white;
        skin.flipX = !enemy;
        power = data.power * damageScale;
        speed = data.speed * (boss ? 0.75f : 1);
        cooldown = Mathf.Max(0.1f, data.cooldown);
	    attackRange = data.attackRange;
	    if(data.ranged)
	    {
	    	bower.SetActive(true);
	    }
        if (boss)
        {
            GetComponent<Animator>().enabled = false;
            transform.localScale *= 1.6f;
        }
        nextAttack = Time.time + 0.3f;
    }

    void Update()
    {
        if (!combatState.alive || !GameCore.instance.inBattle)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        FindTarget();
        if (currentTarget != null)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time < nextAttack) return;
            nextAttack = Time.time + cooldown;
            if (entityData.ranged)
            {
                Projectile shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity, GameCore.instance.map.transform);
                shot.Setup(currentTarget, power, entityData.projectileSprite, entityData.projectileSpeed, entityData.projectileSize, entityData.projectileArc, entityData.killsRegularUnitsInOneHit);
                shot.skin.color = entityData.projectileColor;
            }
            else currentTarget.TakeDamage(CombatRules.DamageForHit(power, currentTarget.health,
                entityData.killsRegularUnitsInOneHit, currentTarget.isBoss, currentTarget.isTower));
            GameCore.instance.sound.PlayAttack();
        }
        else
        {
            Vector2 targetPosition = combatState.isEnemy ? GameCore.instance.pointTower.transform.position : GameCore.instance.pointEnemyTower.transform.position;
            Vector2 direction = targetPosition - (Vector2)transform.position;
            rb.linearVelocity = direction.magnitude > 0.4f ? direction.normalized * speed : Vector2.zero;
        }
    }

    void FindTarget()
    {
        currentTarget = null;
        float distance = attackRange;
        foreach (CombatEntity other in CombatEntity.all)
        {
            if (other == null || !other.alive || other.isEnemy == combatState.isEnemy) continue;
            float next = other.Distance(transform.position);
            if (next > distance) continue;
            distance = next;
            currentTarget = other;
        }
    }
}
