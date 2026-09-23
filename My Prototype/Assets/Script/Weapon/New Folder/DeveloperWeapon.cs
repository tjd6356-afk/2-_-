using UnityEngine;

public class DeveloperWeapon : WeaponBase
{
    [Header("Existing Weapon Systems")]

    [SerializeField]
    private PlayerShooter shooter;

    [SerializeField]
    private PlayerMeleeAttack meleeAttack;

    [SerializeField]
    private WireController wireController;


    public override void Equip()
    {
        base.Equip();


        if (shooter != null)
            shooter.enabled = true;

        if (meleeAttack != null)
            meleeAttack.enabled = true;

        if (wireController != null)
            wireController.enabled = true;
    }


    public override void Unequip()
    {
        if (shooter != null)
            shooter.enabled = false;

        if (meleeAttack != null)
            meleeAttack.enabled = false;

        if (wireController != null)
            wireController.enabled = false;


        base.Unequip();
    }
}