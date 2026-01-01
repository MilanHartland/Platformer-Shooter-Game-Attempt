using TMPro;
using UnityEngine;
using MilanUtils;

public class GuiManager : MonoBehaviour
{
    public TextMeshProUGUI hpTMP;
    public TextMeshProUGUI mainMagazineTMP;
    public TextMeshProUGUI altMagazineTMP;

    PlayerManager playerManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerManager = Variables.player.GetComponent<PlayerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        hpTMP.text = $"{playerManager.hp} / {playerManager.maxHP}";
        
        mainMagazineTMP.gameObject.SetActive(PlayerManager.mainWeapon != null);
        altMagazineTMP.gameObject.SetActive(PlayerManager.altWeapon != null);
        
        if(PlayerManager.mainWeapon != null) mainMagazineTMP.text = $"{PlayerManager.mainWeaponBullets} / {PlayerManager.mainWeapon.magazineSize}";
        if(PlayerManager.altWeapon != null) altMagazineTMP.text = $"{PlayerManager.altWeaponBullets} / {PlayerManager.altWeapon.magazineSize}";
    }
}
