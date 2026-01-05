using UnityEngine;
using TMPro;
using MilanUtils;

public class TooltipInfoHandler : MonoBehaviour
{
    public TextMeshProUGUI nameTMP, typeTMP, infoTMP, descriptionTMP;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(Input.GetAxis("Mouse X") == 0 && Input.GetAxis("Mouse Y") == 0) return; //If mouse didn't move, don't change anything
        var underMouse = UI.GetObjectsUnderMouse();

        GameObject hoveredObj = null;
        WeaponStats weap = null;
        foreach(var obj in underMouse)
        {
            if(obj.TryGetComponent(out WeaponMenuHandler wmh))
            {
                if(obj.name.Contains("Copy")) weap = ModuleApplyHandler.allWeapons[obj.gameObject.name[..obj.gameObject.name.IndexOf(" Copy")]];
                else weap = ModuleApplyHandler.allWeapons[obj.gameObject.name];
                hoveredObj = obj;
                break;
            }
        }

        if(weap != null)
        {
            nameTMP.text = weap.name;
            typeTMP.text = hoveredObj.GetComponent<DragDrop>().name;
            infoTMP.text = (weap.automatic ? "Automatic\n\n" : "Manual\n\n") + $"Damage: {weap.damage}\nFirerate: {weap.fireRate}/s\nMagazine Size: {weap.magazineSize}"
            + $"\nReload Time: {weap.reloadTime}\nSpread: {weap.spread}°\nBullet Speed: {weap.bulletSpeed} m/s";
        }
    }
}
