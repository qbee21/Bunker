using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatEntity : MonoBehaviour
{
    public static List<CombatEntity> all = new List<CombatEntity>();
    public float health;
    public bool isEnemy;
    public bool isTower;
    public bool isBoss;
    public bool diesFromOneHit;
    public Image healthBar;
    public Collider2D body;
    public bool alive;
    public int coins;
    float totalHealth;

    void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    public void Setup(float value)
    {
        totalHealth = Mathf.Max(1, value);
        health = totalHealth;
        alive = true;
        if (body != null) body.enabled = true;
        ReloadHealth();
    }

    public void ReloadHealth()
    {
        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(health / Mathf.Max(1, totalHealth));
    }

    public void TakeDamage(float damage)
    {
        if (!alive || damage <= 0 || !GameCore.instance.inBattle) return;
        health = Mathf.Max(0, health - CombatRules.DamageTaken(damage, health, diesFromOneHit));
        ReloadHealth();
        if (health > 0) return;
        alive = false;
        if (isTower)
        {
            Tower tower = GetComponent<Tower>();
            if (tower != null) tower.RefreshWeapons(false);
        }
        if (body != null) body.enabled = false;
        GameCore.instance.UnitDied(this);
        if (!isTower) Destroy(gameObject);
    }

    public float Distance(Vector2 point)
    {
        if (body != null && body.enabled) return Vector2.Distance(point, body.ClosestPoint(point));
        return Vector2.Distance(point, transform.position);
    }
}
