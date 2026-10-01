using System;
using static Readme;

public interface IAmmoWeapon
{
    int CurrentAmmo { get; }

    int MagazineSize { get; }

    int ReserveMagazines { get; }

    bool IsReloading { get; }


    event Action OnAmmoStateChanged;
}