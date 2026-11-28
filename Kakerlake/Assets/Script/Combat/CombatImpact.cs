using UnityEngine;

// Call instead of IHealth.TakeDamage for PLAYER weapon hits only.
public static class CombatImpact
{
    private static IHealth activeTarget;
    public static bool IsApplyingTo(IHealth target) => object.ReferenceEquals(activeTarget, target);
    public static void Apply(IHealth target, float damage, WeaponType weaponType,
        Vector3 attackDirection)
    {
        Apply(target, damage, weaponType == WeaponType.Melee, attackDirection);
    }

    // For projectiles that do not know their WeaponType.
    public static void Apply(IHealth target, float damage, bool isMelee,
        Vector3 attackDirection)
    {
        if (target == null || target.IsDead || damage <= 0f) return;

        Component component = target as Component;
        EnemyStats enemy = component as EnemyStats;
        EnemyHitReaction reaction = enemy != null
            ? enemy.GetComponent<EnemyHitReaction>() : null;

        // Begin BEFORE damage so the existing OnDamaged callback does not
        // start a second knockback. Also permits lethal-hit launch.
        if (reaction != null && reaction.isActiveAndEnabled)
            reaction.ReceiveHit(attackDirection, isMelee);

        float before = target.CurrentHealth;
        IHealth previousTarget = activeTarget;
        activeTarget = target;
        try { target.TakeDamage(damage); }
        finally { activeTarget = previousTarget; }

        // Only feedback when an actual enemy hit removes HP.
        // Bosses share hit stop, but remain immune to physical launch by default.
        if ((enemy != null || component is BossStats) &&
            (target.IsDead || target.CurrentHealth < before))
        {
            CombatHitStop.Request(isMelee);
        }
    }
}
