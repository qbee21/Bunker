using UnityEngine;

public class Projectile : MonoBehaviour
{
    public SpriteRenderer skin;
    CombatEntity target;
    Vector3 start;
    float damage;
    float duration;
    float elapsed;
    float arc;
    float spinSpeed;
    Vector3 arcDirection = Vector3.up;
    bool killsRegularUnitsInOneHit;

    public void Setup(CombatEntity enemy, float power, Sprite sprite, float speed, float size, float height, bool oneHitRegularUnits = false, float rotationSpeed = 0, bool perpendicularArc = false)
    {
        target = enemy;
        damage = power;
        killsRegularUnitsInOneHit = oneHitRegularUnits;
        start = transform.position;
        duration = Mathf.Max(0.08f, Vector2.Distance(start, enemy.transform.position) / Mathf.Max(0.1f, speed));
        elapsed = 0;
        arc = Mathf.Max(0, height);
        spinSpeed = rotationSpeed;
        arcDirection = Vector3.up;
        if (perpendicularArc)
        {
            Vector3 direction = enemy.transform.position - start;
            if (direction.sqrMagnitude > 0.0001f)
            {
                arcDirection = new Vector3(-direction.y, direction.x, 0).normalized;
                if (arcDirection.y < 0) arcDirection = -arcDirection;
            }
        }
        skin.color = Color.white;
        if (sprite != null) skin.sprite = sprite;
        transform.localScale = Vector3.one * size;
    }

    void Update()
    {
        if (target == null || !target.alive || !GameCore.instance.inBattle)
        {
            Destroy(gameObject);
            return;
        }
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(start, target.transform.position, t) + arcDirection * (4 * arc * t * (1 - t));
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime, Space.World);
        if (t < 1) return;
        target.TakeDamage(CombatRules.DamageForHit(damage, target.health,
            killsRegularUnitsInOneHit, target.isBoss, target.isTower));
        Destroy(gameObject);
    }
}
