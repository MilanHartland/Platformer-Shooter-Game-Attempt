using System;
using System.Collections.Generic;
using MilanUtils;
using UnityEngine;
using UnityEngine.UI;

public class WeaponStats : MonoBehaviour
{
    public int bulletSlots;
    public int effectSlots;
    RectTransform panel;
    Transform parent;

    void Start()
    {
        parent = transform.parent;
        DragDrop.pickUpAction += Patricide;
        DragDrop.dragInAction += ResetEditLayout;
    }

    [InspectorButton("Create Edit Panel")]
    void EditorResetEditLayout() { ResetEditLayout(GetComponent<DragDrop>(), GameObject.Find("Weapon Inventory").GetComponent<DragDrop>()); }

    void Patricide(DragDrop item)
    {
        if (item != GetComponent<DragDrop>() || parent.gameObject.name != transform.name + " Parent") return;
        Destroy(parent.gameObject);
    }
    
    void ResetEditLayout(DragDrop item, DragDrop slot)
    {
        if (this == null || item != GetComponent<DragDrop>() || slot.gameObject.name != "Weapon Inventory") return;

        //If not parented to the correct item, instantiate the prefab, set the name, set parent/scale of parent, parent this to parent, and correct scale/position
        if (transform.parent.name != transform.name + " Parent")
        {
            GameObject obj = Instantiate(GameObject.Find("Game Manager").GetComponent<WeaponModules>().weaponPrefab);
            obj.name = transform.name + " Parent";
            obj.transform.SetParent(GameObject.Find("Weapon Inventory").transform);
            obj.transform.localScale = Vector3.one;
            transform.SetParent(obj.transform);
            transform.localScale = Vector3.one;
            GetComponent<RectTransform>().anchoredPosition = new(115, -115);
        }

        parent = transform.parent;
        panel = transform.parent.Find("Background Panel").GetComponent<RectTransform>();

        //Kill all children
        List<Transform> siblings = new();
        foreach (Transform child in panel) { siblings.Add(child); }
        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            if (siblings[i].name.Contains("Slot") && siblings[i] != this.transform) DestroyImmediate(siblings[i].gameObject);
        }

        //For each bullet/effect slot, instantiate it, set parent, set as last sibling, and fix scale
        WeaponModules modules = GameObject.Find("Game Manager").GetComponent<WeaponModules>();
        for (int i = 0; i < bulletSlots; i++)
        {
            GameObject obj = Instantiate(modules.bulletSlotPrefab);
            obj.transform.SetParent(panel);
            obj.transform.SetAsLastSibling();
            obj.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < effectSlots; i++)
        {
            GameObject obj = Instantiate(modules.effectSlotPrefab);
            obj.transform.SetParent(panel);
            obj.transform.SetAsLastSibling();
            obj.transform.localScale = Vector3.one;
        }

        //Force rebuild of layout, and validate panel sizes
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        _OnValidate();
    }

    void OnValidate() { UnityEditor.EditorApplication.delayCall += _OnValidate; }
    void _OnValidate()
    {
        if (this == null) return;
        if (!transform.parent.Find("Background Panel")) return;
        panel = transform.parent.Find("Background Panel").GetComponent<RectTransform>();

        GridLayoutGroup layout = panel.GetComponent<GridLayoutGroup>();
        float panelSize = Mathf.Clamp(panel.childCount, 0, 8) * layout.cellSize.x + Mathf.Clamp(panel.childCount - 1, 0, 8) * layout.spacing.x + layout.padding.left + layout.padding.right;
        panel.sizeDelta = new(panelSize, panel.sizeDelta.y);
    }
}
