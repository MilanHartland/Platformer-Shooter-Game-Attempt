using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.PostProcessing;
using MilanUtils;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class MenuManager : MonoBehaviour
{
    public enum MenuState {MainMenu, Game, Pause, Settings, Inventory}
    public static MenuState menuState;

    public static bool IsPaused => menuState != MenuState.Game;

    bool isMainMenu => SceneManager.GetActiveScene().name.Contains("Main Menu");

    [Header("Post Processing")]
    [ShowIf("!isMainMenu")]public PostProcessProfile gameProfile;
    [ShowIf("!isMainMenu")]public PostProcessProfile pauseProfile;

    [Header("GameObjects")]
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
        settingsButton.onClick.AddListener(SettingsMenu);
        quitButton.onClick.AddListener(Application.Quit);

        if(isMainMenu) menuState = MenuState.MainMenu;
        else ContinueGame();
    }

    // Update is called once per frame
    void Update()
    {
        if(isMainMenu) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch(menuState)
            {
                case MenuState.Settings:
                case MenuState.Inventory:
                case MenuState.Game:
                    PauseMenu();
                    break;
                case MenuState.Pause:
                    ContinueGame();
                    break;
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && menuState == MenuState.Inventory) ContinueGame();
    }

    public void InventoryMenu()
    {
        inventory.SetActive(true);
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);
        menuState = MenuState.Inventory;
        Camera.main.GetComponent<PostProcessVolume>().profile = pauseProfile;
        DragDrop.StopDragging();
    }

    void PauseMenu()
    {
        Time.timeScale = 0f;
        inventory.SetActive(false);
        pauseMenu.SetActive(true);
        settingsMenu.SetActive(false);
        menuState = MenuState.Pause;
        Camera.main.GetComponent<PostProcessVolume>().profile = pauseProfile;
        DragDrop.StopDragging();
    }

    async Task StartGame()
    {
        var loading = SceneManager.LoadSceneAsync(1);
        await loading;
        Variables.TryAutoSetValues();
    }

    void ContinueGame()
    {
        Time.timeScale = 1f;
        inventory.SetActive(false);
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);
        menuState = MenuState.Game;
        Camera.main.GetComponent<PostProcessVolume>().profile = gameProfile;
        DragDrop.StopDragging();
    }

    void SettingsMenu()
    {
        Time.timeScale = 0f;
        inventory.SetActive(false);
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);
        menuState = MenuState.Settings;
        Camera.main.GetComponent<PostProcessVolume>().profile = pauseProfile;
        DragDrop.StopDragging();
    }
}
