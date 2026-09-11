using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerShooter playerShooter;

    [SerializeField] private TMP_Text ammoText;


    private void Awake()
    {
        if (ammoText == null)
        {
            ammoText =
                GetComponent<TMP_Text>();
        }
    }


    private void OnEnable()
    {
        if (playerShooter == null)
            return;


        playerShooter.OnAmmoChanged +=
            HandleAmmoChanged;

        playerShooter.OnReloadStateChanged +=
            HandleReloadStateChanged;
    }


    private void Start()
    {
        RefreshUI();
    }


    private void OnDisable()
    {
        if (playerShooter == null)
            return;


        playerShooter.OnAmmoChanged -=
            HandleAmmoChanged;

        playerShooter.OnReloadStateChanged -=
            HandleReloadStateChanged;
    }


    private void HandleAmmoChanged(
        int currentAmmo,
        int magazineSize,
        int reserveMagazines
    )
    {
        RefreshUI();
    }


    private void HandleReloadStateChanged(
        bool isReloading
    )
    {
        RefreshUI();
    }


    private void RefreshUI()
    {
        if (playerShooter == null ||
            ammoText == null)
        {
            return;
        }


        string reloadText = "";


        if (playerShooter.IsReloading)
        {
            reloadText =
                "\n재장전 중...";
        }


        ammoText.text =
            $"탄창 {playerShooter.ReserveMagazines}" +
            $"\n탄약 {playerShooter.CurrentAmmo} / {playerShooter.MagazineSize}" +
            reloadText;
    }
}