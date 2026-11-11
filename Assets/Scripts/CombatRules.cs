// Pure rules shared by melee and projectiles; bosses and bunkers are never
// promoted to an instant-kill target by the drone flag.
public static class CombatRules
{
    public static float StartingHealth(float baseHealth, float scale, bool fragile)
    {
        return fragile ? 1 : baseHealth * scale;
    }

    public static float DamageForHit(float damage, float targetHealth, bool oneHitRegularUnits, bool isBoss, bool isTower)
    {
        if (damage <= 0) return 0;
        return oneHitRegularUnits && !isBoss && !isTower ? targetHealth : damage;
    }

    public static float DamageTaken(float damage, float health, bool fragile)
    {
        if (damage <= 0) return 0;
        return fragile ? health : damage;
    }
}
