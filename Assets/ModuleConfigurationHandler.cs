using System.Collections.Generic;
using MilanUtils;
using UnityEngine;

public class ModuleConfigurationHandler : MonoBehaviour
{
    Dictionary<InfoTag, InfoTag> childedItems = new();
    int currentOpenedSibIndex;
    InfoTag currentlyOpened;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UI.GetObjectsUnderMouse().ForEach(x =>
            {
                if (x.GetComponent<DragDrop>() && x.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot)
                { OpenInformation(x.GetComponent<InfoTag>()); return; }
            });
        }
    }

    public void OpenInformation(InfoTag module)
    {
        //If an item is currently opened, set the parent and sibling index back
        if (currentlyOpened != null)
        {
            currentlyOpened.transform.SetParent(transform.parent);
            currentlyOpened.transform.SetSiblingIndex(currentOpenedSibIndex);
        }
        
        currentlyOpened = module;

        RectTransform rectTransf = module.GetComponent<RectTransform>();

        //Because all modules and this have anchor/pivot at topleft and both are childed to the slot parent, you can set this anchor to other
        GetComponent<RectTransform>().anchoredPosition = rectTransf.anchoredPosition;

        currentOpenedSibIndex = module.transform.GetSiblingIndex();
        module.transform.SetParent(transform);

        //Because anchor is topleft for both, you can set anchor position to 0 and have this be anchored to the top left of the panel
        rectTransf.anchoredPosition = Vector2.zero;
    }
}
