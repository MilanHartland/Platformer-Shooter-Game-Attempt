using UnityEngine;
using TMPro;
using MilanUtils;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TooltipInfoHandler : MonoBehaviour
{
    public TextMeshProUGUI nameTMP, typeTMP, infoTMP, upgradeButtonTMP, upgradeInfoTMP;
    public GameObject upgradePanel;
    Image img;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        img = GetComponent<Image>();
        upgradeInfoTMP.gameObject.SetActive(false);
        upgradeInfoTMP.gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if((Input.GetAxis("Mouse X") == 0 && Input.GetAxis("Mouse Y") == 0) || MenuManager.menuState != MenuManager.MenuState.Inventory) return; //If mouse didn't move, don't change anything
        var underMouse = UI.GetObjectsUnderMouse();

        GameObject hoveredObj = null;
        WeaponStats weap = null;
        ItemInfo it = null;
        foreach(var obj in underMouse)
        {
            if(obj.TryGetComponent(out WeaponInfo wmh))
            {
                if(obj.name.Contains("Copy")) weap = ModuleApplyHandler.allWeapons[obj.gameObject.name[..obj.gameObject.name.IndexOf(" Copy")]];
                else weap = ModuleApplyHandler.allWeapons[obj.gameObject.name];
                hoveredObj = obj;
                break;
            }
            else if(obj.TryGetComponent(out ItemInfo ii))
            {
                hoveredObj = obj;
                it = ii;
                break;
            }
        }

        if(weap != null)
        {
            if(hoveredObj.GetComponent<DragDrop>().name.Contains("Alt")) img.color = new(1f, .5f, 0f);
            else img.color = new(1f, .25f, .25f);

            upgradePanel.SetActive(false);
            transform.localScale = Vector3.one;
            nameTMP.text = weap.name;
            typeTMP.text = hoveredObj.GetComponent<DragDrop>().name;
            infoTMP.text = (weap.automatic ? "Automatic\n\n" : "Manual\n\n") + $"Damage: {weap.damage}\nFirerate: {weap.fireRate}/s\nMagazine Size: {weap.magazineSize}"
            + $"\nReload Time: {weap.reloadTime}\nSpread: {weap.spread}°\nBullet Speed: {weap.bulletSpeed} m/s";
        }
        else if(it != null)
        {
            if(it.type == ItemInfo.ItemType.Bullet) img.color = new(.5f, 0f, .75f);
            else img.color = new(0f, .75f, .125f);

            upgradePanel.SetActive(true);
            transform.localScale = Vector3.one;
            nameTMP.text = it.name;
            typeTMP.text = it.type.ToString();
            infoTMP.text = it.description;

            if(it.upgrades.Count > 0)
            {
                upgradeButtonTMP.text = $"Cost: {it.upgrades[0].cost}\n[{Keybinds.interact}] Upgrade";
                upgradeInfoTMP.text = it.upgrades[0].description;
                upgradeInfoTMP.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(upgradePanel.GetComponent<RectTransform>());
            }
            else
            {
                upgradeButtonTMP.text = $"Item is fully upgraded";
                upgradeInfoTMP.gameObject.SetActive(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(upgradePanel.GetComponent<RectTransform>());
            }
        }
        else transform.localScale = Vector3.zero;
    }
}
