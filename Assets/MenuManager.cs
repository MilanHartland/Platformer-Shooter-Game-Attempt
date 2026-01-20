using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.PostProcessing;
using MilanUtils;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;

public class MenuManager : MonoBehaviour
{
    public enum MenuState {MainMenu, Game, Pause, Settings, Inventory, Storage}
    public static MenuState menuState;

    public static bool IsPaused => menuState != MenuState.Game;
    public static bool tooltipShownDefault;

    bool isMainMenu => SceneManager.GetActiveScene().name.Contains("Main Menu");

    [Header("Post Processing")]
    [ShowIf("!isMainMenu")]public PostProcessProfile gameProfile;
    [ShowIf("!isMainMenu")]public PostProcessProfile pauseProfile;

    [Header("GameObjects")]
    [ShowIf("!isMainMenu")]public GameObject hud;
    [ShowIf("!isMainMenu")]public GameObject inventory;
    [ShowIf("!isMainMenu")]public GameObject pauseMenu;
    [ShowIf("!isMainMenu")]public GameObject settingsMenu;

    [Header("Buttons")]
    [ShowIf("!isMainMenu")]public Button resumeButton;
    [ShowIf("isMainMenu")]public Button startGameButton;
    public Button settingsButton;
    public Button quitButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        if(resumeButton) resumeButton.onClick.AddListener(ContinueGame);
        if(startGameButton) startGameButton.onClick.AddListener(async () => {await StartGame();});
        settingsButton.onClick.AddListener(OpenSettingsMenu);
        quitButton.onClick.AddListener(Application.Quit);

        if(World.TryFindInactive("DragDrop Hold Toggle", out GameObject ddHoldToggle)) 
        {
            ddHoldToggle.GetComponent<Toggle>().onValueChanged.AddListener(SettingsDragDropToggle);
            ddHoldToggle.GetComponent<Toggle>().isOn = World.FindInactive("Item Inventory").GetComponent<DragDrop>().holdToDrag;
            SettingsDragDropToggle(ddHoldToggle.GetComponent<Toggle>().isOn);
        }
        if(World.TryFindInactive("Reset Save Button", out GameObject rsb)){rsb.GetComponent<Button>().onClick.AddListener(SaveSystem.ResetSave);}
        if(World.TryFindInactive("Tooltip Show Button", out GameObject tooltipToggle))
        {
            tooltipToggle.GetComponent<Toggle>().onValueChanged.AddListener(x => 
            {
                tooltipShownDefault = x; 
                GameObject.Find("Show Tooltip").transform.Find("Keybind Name").GetComponent<TextMeshProUGUI>().text = x ? "Hide Tooltip" : "Show Tooltip";
            });
            tooltipShownDefault = tooltipToggle.GetComponent<Toggle>().isOn;
        }

        if(isMainMenu) menuState = MenuState.MainMenu;
        else ContinueGame();
    }

    // Update is called once per frame
    void Update()
    {
        if(isMainMenu) return;

        if (Input.GetKeyDown(Keybinds.pause))
        {
            switch(menuState)
            {
                case MenuState.Settings:
                case MenuState.Inventory:
                case MenuState.Storage:
                case MenuState.Game:
                    OpenPauseMenu();
                    break;
                case MenuState.Pause:
                    ContinueGame();
                    break;
            }
        }
    }

    public void OpenInventoryMenu()
    {
        PauseGame();
        inventory.SetActive(true);
        menuState = MenuState.Inventory;
    }

    public void OpenPauseMenu()
    {
        PauseGame();
        pauseMenu.SetActive(true);
        menuState = MenuState.Pause;
        Camera.main.GetComponent<PostProcessVolume>().profile = pauseProfile;
    }

    async Task StartGame()
    {
        var loading = SceneManager.LoadSceneAsync(1);
        await loading;
        Variables.TryAutoSetValues();
        Time.timeScale = 1f;
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        inventory.SetActive(false);
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);
        hud.SetActive(true);
        menuState = MenuState.Game;
        Camera.main.GetComponent<PostProcessVolume>().profile = gameProfile;
        DragDrop.StopDragging();
    }

    public void OpenSettingsMenu()
    {
        PauseGame();
        settingsMenu.SetActive(true);
        menuState = MenuState.Settings;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        inventory.SetActive(false);
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);
        hud.SetActive(false);
        DragDrop.StopDragging();
    }

    private void SettingsDragDropToggle(bool isToggledOn)
    {
        World.AllGameObjects(true, typeof(DragDrop)).ForEach(x => {x.GetComponent<DragDrop>().holdToDrag = isToggledOn;});
    }
}
