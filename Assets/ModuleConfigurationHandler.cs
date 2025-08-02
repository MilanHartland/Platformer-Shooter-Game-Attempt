using System.Collections;
using System.Collections.Generic;
using MilanUtils;
using UnityEngine;

public class ModuleConfigurationHandler : MonoBehaviour
{
    InfoTag currentlyOpened;

    Dictionary<GameObject, Vector3> basePositions = new();
    Dictionary<GameObject, Vector2> baseSizes = new();
    Vector3 baseSelectedPosition;
    Vector2 thisBaseSize;
    bool isTweening;

    public bool hideVertically;
    public float itemsCloseSpeed, panelCloseSpeed, waitSpeed, panelOpenSpeed, itemsOpenSpeed;

    //IDEA: BOOL TO CHANGE IF NON-SELECTED OPTIONS SHOULD HIDE VERTICALL (GO UP/DOWN) OR HORIZONTALLY (MOVE A BIT LEFT TO GO BEHIND THE INVENTORY)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        basePositions = new();
        thisBaseSize = GetComponent<RectTransform>().sizeDelta;

        World.AllGameObjectsWhere((obj) => { return obj.GetComponent<DragDrop>() && obj.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot; })
        .ForEach(x => { basePositions.Add(x, x.transform.position); if (x.transform.position.y > baseSelectedPosition.y) baseSelectedPosition = x.transform.position; });
        
        foreach(Transform transf in transform){ baseSizes.Add(transf.gameObject, transf.GetComponent<RectTransform>().sizeDelta); }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isTweening)
        {
            bool openedSomething = false;
            UI.GetObjectsUnderMouse().ForEach(x =>
            {
                if (currentlyOpened != x.GetComponent<InfoTag>() && x.GetComponent<DragDrop>() && x.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot)
                { OpenInformation(x.GetComponent<InfoTag>()); openedSomething = true; return; }
            });
            if (!openedSomething) CloseInformation();
        }
    }
    
    public void CloseInformation(){ StartCoroutine(CloseInformationCoroutine()); }
    //In total: resets all slot positions to the base position, if currently something open move this to that base, then 'close' this to slot size (first y then x)
    public IEnumerator CloseInformationCoroutine()
    {
        isTweening = true;

        List<GameObject> allSlots = World.AllGameObjectsWhere((obj) =>
            { return obj.GetComponent<DragDrop>() && obj.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot; });

        foreach (Transform obj in transform)
        {
            LeanTween.size(obj.GetComponent<RectTransform>(), Vector3.zero, itemsCloseSpeed);
        }
        yield return new WaitForSeconds(itemsCloseSpeed);

        //Moves objects back to base position
        foreach (GameObject obj in allSlots) { LeanTween.move(obj, basePositions[obj], panelCloseSpeed); }

        //If something opened, move this to that base position. Also size down to height of slot in half time and in other half size down to width
        if (currentlyOpened) LeanTween.move(gameObject, basePositions[currentlyOpened.gameObject], panelCloseSpeed);
        LeanTween.size(GetComponent<RectTransform>(), new Vector3(GetComponent<RectTransform>().sizeDelta.x, 200f), panelCloseSpeed / 2f);
        yield return new WaitForSeconds(panelCloseSpeed / 2f);
        LeanTween.size(GetComponent<RectTransform>(), new Vector3(200f, 200f), panelCloseSpeed / 2f);
        yield return new WaitForSeconds(panelCloseSpeed / 2f);

        isTweening = false;
        currentlyOpened = null;
    }

    public void OpenInformation(InfoTag module) { StartCoroutine(OpenInformationCoroutine(module)); }
    //In total: closes, then moves non-selected out of the way, moves this and selected to correct position, then opens this to base size (first x then y)
    public IEnumerator OpenInformationCoroutine(InfoTag module)
    {
        isTweening = true;
        RectTransform rectTransf = module.GetComponent<RectTransform>();

        List<GameObject> allSlots = World.AllGameObjectsWhere((obj) =>
            { return obj.GetComponent<DragDrop>() && obj.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot; });
        allSlots.Remove(module.gameObject);

        if (currentlyOpened)
        {
            CloseInformation();
            yield return new WaitForSeconds(panelCloseSpeed);
            yield return new WaitForSeconds(waitSpeed);
        }

        currentlyOpened = module;

        //Because all modules and this have anchor/pivot at topleft and both are childed to the slot parent, you can set this anchor to other
        GetComponent<RectTransform>().anchoredPosition = rectTransf.anchoredPosition;

        LeanTween.move(rectTransf, baseSelectedPosition, panelOpenSpeed);

        foreach (GameObject obj in allSlots)
        {
            if (hideVertically)
            {
                if (obj.transform.position.y > module.transform.position.y)
                {
                    //Should move to this y + (difference this y and selected y)
                    LeanTween.moveY(obj, obj.transform.position.y + (baseSelectedPosition.y - module.transform.position.y), panelOpenSpeed);
                }
                else if (basePositions[module.gameObject] == baseSelectedPosition)
                {
                    //Should move to basepos y - this size y
                    LeanTween.moveY(obj, obj.transform.position.y - (thisBaseSize.y - 293.33333f), panelOpenSpeed);
                }
            }
            else LeanTween.moveX(obj, obj.transform.position.x - 200f, panelOpenSpeed);
        }
        LeanTween.move(currentlyOpened.gameObject, baseSelectedPosition, panelOpenSpeed);
        LeanTween.move(gameObject, baseSelectedPosition, panelOpenSpeed);
        LeanTween.size(GetComponent<RectTransform>(), new Vector2(thisBaseSize.x, 200f), panelOpenSpeed / 2f);
        yield return new WaitForSeconds(panelOpenSpeed / 2f);
        LeanTween.size(GetComponent<RectTransform>(), thisBaseSize, panelOpenSpeed / 2f);

        foreach (Transform transf in transform)
        {
            LeanTween.size(transf.GetComponent<RectTransform>(), baseSizes[transf.gameObject], itemsOpenSpeed);
        }
        yield return new WaitForSeconds(itemsOpenSpeed);

        isTweening = false;
    }
}
