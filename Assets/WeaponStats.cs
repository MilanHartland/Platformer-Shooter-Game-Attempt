using System;
using System.Collections.Generic;
using MilanUtils;
using UnityEngine;
using UnityEngine.UI;

public class WeaponStats : MonoBehaviour
{
    [Serializable]
    class WeaponModuleTriggerEffect
    {
        public int triggerCount, effectCount;
    }

    [SerializeField] List<WeaponModuleTriggerEffect> triggersAndEffects;
    RectTransform panel;

    [InspectorButton]
    void ResetEditLayout()
    {
        panel = transform.parent.Find("Background Panel").GetComponent<RectTransform>();

        List<Transform> siblings = new();
        foreach (Transform child in panel) { siblings.Add(child); }
        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            if (siblings[i].name.Contains("Trigger Effect Pair")) DestroyImmediate(siblings[i].gameObject);
        }

        WeaponModules modules = GameObject.Find("Game Manager").GetComponent<WeaponModules>();
        for(int i = 0; i < triggersAndEffects.Count; i++)
        {
            WeaponModuleTriggerEffect trigEff = triggersAndEffects[i];
            GameObject pair = GameObject.Instantiate(modules.triggerEffectPairPrefab);
            pair.transform.SetParent(panel);
            pair.transform.localScale = Vector3.one;
            pair.transform.SetAsLastSibling();
            
            for (int j = 0; j < trigEff.triggerCount; j++)
            {
                GameObject trig = GameObject.Instantiate(modules.triggerSlotPrefab);
                trig.transform.SetParent(pair.transform);
                trig.transform.localScale = Vector3.one;
                trig.transform.SetAsLastSibling();
            }
            
            GameObject arrow = GameObject.Instantiate(modules.arrowFillerPrefab);
            arrow.transform.SetParent(pair.transform);
            arrow.transform.localScale = Vector3.one;
            arrow.transform.SetAsLastSibling();

            for (int j = 0; j < trigEff.effectCount; j++)
            {
                GameObject eff = GameObject.Instantiate(modules.effectSlotPrefab);
                eff.transform.SetParent(pair.transform);
                eff.transform.localScale = Vector3.one;
                eff.transform.SetAsLastSibling();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            _OnValidate();
        }
    }

    void OnValidate() { UnityEditor.EditorApplication.delayCall += _OnValidate; }
    void _OnValidate()
    {
        if (!Objects.prefabs.ContainsKey("Trigger Slot")) Objects.prefabs.Add("Trigger Slot", GameObject.Find("Game Manager").GetComponent<WeaponModules>().triggerSlotPrefab);
        panel = transform.parent.Find("Background Panel").GetComponent<RectTransform>();

        RectTransform biggestChild = null;
        foreach (Transform child in panel)
        {
            if (child.name != "Background Panel" && (biggestChild == null || child.GetComponent<RectTransform>().sizeDelta.x > biggestChild.sizeDelta.x))
                biggestChild = child.GetComponent<RectTransform>();
        }

        if (biggestChild != null)
            panel.sizeDelta = new(245f + biggestChild.sizeDelta.x, 230f);
        else panel.sizeDelta = new(230f, 230f);
    }
}
