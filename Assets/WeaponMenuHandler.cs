using System;
using System.Collections.Generic;
using MilanUtils;
using UnityEngine;
using UnityEngine.UI;

public class WeaponMenuHandler : MonoBehaviour
{
    public int bulletSlots;
    public int effectSlots;
    RectTransform panel;
    Transform inventoryParent;

    void Start()
    {
        inventoryParent = transform.parent;
        DragDrop.pickUpAction += Patricide;
        DragDrop.dragInAction += ResetEditLayout;

        ResetEditLayout(GetComponent<DragDrop>(), GameObject.Find("Weapon Inventory").GetComponent<DragDrop>());
    }

    [InspectorButton("Create Edit Panel")]
    void EditorResetEditLayout() { Objects.LoadAllResources(); ResetEditLayout(GetComponent<DragDrop>(), GameObject.Find("Weapon Inventory").GetComponent<DragDrop>()); }

    //Kill the parent
    void Patricide(DragDrop item)
    {
        if (this == null || item != GetComponent<DragDrop>() || (inventoryParent && inventoryParent.gameObject.name != transform.name + " Inventory Parent") || !inventoryParent) return;
        inventoryParent.SetParent(this.transform);
        inventoryParent.gameObject.SetActive(false);
        // Destroy(inventoryParent.gameObject);
        GetComponent<DragDrop>().SetFallbackParent(GameObject.Find("Weapon Inventory").transform);
    }
    
    void ResetEditLayout(DragDrop item, DragDrop slot)
    {
        if (this == null || item != GetComponent<DragDrop>() || slot.gameObject.name != "Weapon Inventory") return;

        bool hadAsChild = false;
        if (transform.Find(transform.name + " Inventory Parent"))
        {
            hadAsChild = true;
            Transform obj = transform.Find(transform.name + " Inventory Parent");
            obj.gameObject.SetActive(true);
            obj.SetParent(GameObject.Find("Weapon Inventory").transform);
            transform.SetParent(obj);
            transform.SetAsLastSibling();
            transform.localPosition = new(115f, -115f);
        }
        //If not parented to the correct item, instantiate the prefab, set the name, set parent/scale of parent, parent this to parent, and correct scale/position
        else if (transform.parent.name != transform.name + " Inventory Parent")
        {
            GameObject obj = Instantiate(Objects.prefabs["Weapon Menu Parent"]);
            obj.name = transform.name + " Inventory Parent";
            obj.transform.SetParent(GameObject.Find("Weapon Inventory").transform);
            obj.transform.localScale = Vector3.one;
            transform.SetParent(obj.transform);
            transform.localScale = Vector3.one;
            GetComponent<RectTransform>().anchoredPosition = new(115, -115);
        }

        if (hadAsChild) return;

        inventoryParent = transform.parent;
        panel = transform.parent.Find("Background Panel").GetComponent<RectTransform>();

        //Kill all children
        List<Transform> siblings = new();
        foreach (Transform child in panel) { siblings.Add(child); }
        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            if (siblings[i].name.Contains("Slot") && siblings[i] != this.transform) DestroyImmediate(siblings[i].gameObject);
        }

        //For each bullet/effect slot, instantiate it, set parent, set as last sibling, and fix scale
        for (int i = 0; i < bulletSlots; i++)
        {
            GameObject obj = Instantiate(Objects.prefabs["Bullet Slot"]);
            obj.transform.SetParent(panel);
            obj.transform.SetAsLastSibling();
            obj.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < effectSlots; i++)
        {
            GameObject obj = Instantiate(Objects.prefabs["Effect Slot"]);
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
