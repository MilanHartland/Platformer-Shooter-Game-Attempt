using System;
using System.Collections.Generic;
using MilanUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DragDrop))]
public class WeaponMenuHandler : MonoBehaviour
{
    public int bulletSlots;
    public int effectSlots;
    RectTransform panel;

    [InspectorButton("Create Edit Panel")]
    void EditorResetEditLayout() { Variables.LoadAllResources(); ResetEditLayout(); }
    
    public void ResetEditLayout()
    {
        if (this == null) return;

        //If not parented to the correct item, instantiate the prefab, set the name, set parent/scale of parent, parent this to parent, and correct scale/position
        if (transform.parent.name != transform.name + " Inventory Parent")
        {
            GameObject obj = Instantiate(Variables.prefabs["Weapon Menu Parent"]);
            obj.name = transform.name + " Inventory Parent";
            obj.transform.SetParent(World.FindInactive("Weapon Inventory").transform);
            obj.transform.localScale = Vector3.one;
            transform.SetParent(obj.transform);
            transform.localScale = Vector3.one;
            GetComponent<RectTransform>().anchoredPosition = new(115, -115);
        }

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
            GameObject obj = Instantiate(Variables.prefabs["Bullet Slot"]);
            obj.transform.SetParent(panel);
            obj.transform.SetAsLastSibling();
            obj.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < effectSlots; i++)
        {
            GameObject obj = Instantiate(Variables.prefabs["Effect Slot"]);
            obj.transform.SetParent(panel);
            obj.transform.SetAsLastSibling();
            obj.transform.localScale = Vector3.one;
        }

        //Force rebuild of layout, and validate panel sizes
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        _OnValidate();
    }

    void OnValidate() { EditorApplication.delayCall += _OnValidate; }
    void _OnValidate()
    {
        if (this == null || PrefabUtility.IsPartOfPrefabAsset(this)) return;
        if (!transform.parent.Find("Background Panel")) return;
        panel = transform.parent.Find("Background Panel").GetComponent<RectTransform>();

        GridLayoutGroup layout = panel.GetComponent<GridLayoutGroup>();
        float panelSize = Mathf.Clamp(panel.childCount, 0, 6) * layout.cellSize.x + Mathf.Clamp(panel.childCount - 1, 0, 5) * layout.spacing.x + layout.padding.left + layout.padding.right;
        panel.sizeDelta = new(panelSize, panel.sizeDelta.y);
        transform.parent.GetComponent<RectTransform>().sizeDelta = panel.sizeDelta;
    }
}
