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
    public OpenCloseVisuals visuals;

    //IDEA: BOOL TO CHANGE IF NON-SELECTED OPTIONS SHOULD HIDE VERTICALL (GO UP/DOWN) OR HORIZONTALLY (MOVE A BIT LEFT TO GO BEHIND THE INVENTORY)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CloseInformation(new OpenCloseVisuals());

        basePositions = new();
        thisBaseSize = GetComponent<RectTransform>().sizeDelta;

        World.AllGameObjectsWhere((obj) => { return obj.GetComponent<DragDrop>() && obj.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot; })
        .ForEach(x => { basePositions.Add(x, x.transform.position); if (x.transform.position.y > baseSelectedPosition.y) baseSelectedPosition = x.transform.position; });

        foreach (Transform transf in transform) { baseSizes.Add(transf.gameObject, transf.GetComponent<RectTransform>().sizeDelta); }
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
                { OpenInformation(x.GetComponent<InfoTag>(), visuals); openedSomething = true; return; }
            });
            if (!openedSomething) CloseInformation(visuals);
        }
    }

    public void CloseInformation(OpenCloseVisuals vis) { StartCoroutine(CloseInformationCoroutine(vis)); }
    //In total: resets all slot positions to the base position, if currently something open move this to that base, then 'close' this to slot size (first y then x)
    public IEnumerator CloseInformationCoroutine(OpenCloseVisuals vis)
    {
        isTweening = true;

        List<GameObject> allSlots = World.AllGameObjectsWhere((obj) =>
            { return obj.GetComponent<DragDrop>() && obj.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot; });

        foreach (Transform obj in transform)
        {
            LeanTween.size(obj.GetComponent<RectTransform>(), Vector3.zero, vis.itemSpeed).setEase(vis.itemEase);
        }
        yield return new WaitForSeconds(vis.itemSpeed);

        //Moves objects back to base position
        foreach (GameObject obj in allSlots) { LeanTween.move(obj, basePositions[obj], vis.panelSpeed).setEase(vis.moduleMoveEase); }

        //If something opened, move this to that base position. Also size down to height of slot in half time and in other half size down to width
        if (currentlyOpened) LeanTween.move(gameObject, basePositions[currentlyOpened.gameObject], vis.panelSpeed).setEase(vis.moduleMoveEase);
        LeanTween.size(GetComponent<RectTransform>(), new Vector3(GetComponent<RectTransform>().sizeDelta.x, 200f), vis.panelYSpeed).setEase(vis.panelEase);
        yield return new WaitForSeconds(vis.panelYSpeed);
        LeanTween.size(GetComponent<RectTransform>(), new Vector3(200f, 200f), vis.panelXSpeed).setEase(vis.panelEase);
        yield return new WaitForSeconds(vis.panelXSpeed);

        isTweening = false;
        currentlyOpened = null;
        transform.position = new(1000f, 1000f);
    }

    public void OpenInformation(InfoTag module, OpenCloseVisuals vis) { StartCoroutine(OpenInformationCoroutine(module, vis)); }
    //In total: closes, then moves non-selected out of the way, moves this and selected to correct position, then opens this to base size (first x then y)
    public IEnumerator OpenInformationCoroutine(InfoTag module, OpenCloseVisuals vis)
    {
        isTweening = true;
        RectTransform rectTransf = module.GetComponent<RectTransform>();

        List<GameObject> allSlots = World.AllGameObjectsWhere((obj) =>
            { return obj.GetComponent<DragDrop>() && obj.GetComponent<DragDrop>().type == DragDrop.DragDropType.Slot; });
        allSlots.Remove(module.gameObject);

        if (currentlyOpened)
        {
            CloseInformation(vis);
            yield return new WaitForSeconds(vis.panelSpeed);
            yield return new WaitForSeconds(vis.closeopenCooldown);
        }

        currentlyOpened = module;

        //Because all modules and this have anchor/pivot at topleft and both are childed to the slot parent, you can set this anchor to other
        GetComponent<RectTransform>().anchoredPosition = rectTransf.anchoredPosition;

        LeanTween.move(rectTransf, baseSelectedPosition, vis.panelSpeed).setEase(vis.panelEase);

        foreach (GameObject obj in allSlots)
        {
            if (hideVertically)
            {
                if (obj.transform.position.y > module.transform.position.y)
                {
                    //Should move to this y + (difference this y and selected y)
                    LeanTween.moveY(obj, obj.transform.position.y + (baseSelectedPosition.y - module.transform.position.y), vis.panelSpeed).setEase(vis.moduleMoveEase);
                }
                else if (obj.transform.position.y < module.transform.position.y)
                {
                    //Should move to 0 - (difference module and this y) + difference between slots
                    print(module.transform.position.y - obj.transform.position.y);
                    LeanTween.moveY(obj, -(module.transform.position.y - obj.transform.position.y) + 57.03704f, vis.panelSpeed).setEase(vis.moduleMoveEase);
                }
            }
            else LeanTween.moveX(obj, obj.transform.position.x - 200f, vis.panelSpeed).setEase(vis.moduleMoveEase);
        }
        LeanTween.move(currentlyOpened.gameObject, baseSelectedPosition, vis.panelSpeed).setEase(vis.moduleMoveEase);
        LeanTween.move(gameObject, baseSelectedPosition, vis.panelSpeed).setEase(vis.moduleMoveEase);
        LeanTween.size(GetComponent<RectTransform>(), new Vector2(thisBaseSize.x, 200f), vis.panelXSpeed).setEase(vis.panelEase);
        yield return new WaitForSeconds(vis.panelXSpeed);
        LeanTween.size(GetComponent<RectTransform>(), thisBaseSize, vis.panelYSpeed / 2f).setEase(vis.panelEase);
        yield return new WaitForSeconds(vis.panelYSpeed);

        foreach (Transform transf in transform)
        {
            LeanTween.size(transf.GetComponent<RectTransform>(), baseSizes[transf.gameObject], vis.itemSpeed).setEase(vis.itemEase);
        }
        yield return new WaitForSeconds(vis.itemSpeed);

        isTweening = false;
    }

    [System.Serializable]
    public struct OpenCloseVisuals
    {
        public float itemSpeed, panelXSpeed, panelYSpeed, closeopenCooldown;
        public readonly float panelSpeed => panelXSpeed + panelYSpeed;
        public LeanTweenType itemEase, panelEase, moduleMoveEase;

        public OpenCloseVisuals(float _itemSpeed = 0f, float _panelXSpeed = 0f, float _panelYSpeed = 0f, float _closeOpenCooldown = 0f,
        LeanTweenType _itemEase = LeanTweenType.notUsed, LeanTweenType _panelEase = LeanTweenType.notUsed, LeanTweenType _moduleMoveEase = LeanTweenType.notUsed)
        { itemSpeed = _itemSpeed; panelXSpeed = _panelXSpeed; panelYSpeed = _panelYSpeed; closeopenCooldown = _closeOpenCooldown; 
        itemEase = _itemEase; panelEase = _panelEase; moduleMoveEase = _moduleMoveEase; }
    }
}
