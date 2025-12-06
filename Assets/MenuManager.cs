using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.PostProcessing;

public class MenuManager : MonoBehaviour
{
    enum MenuState {Game, Pause, Inventory}
    static MenuState menuState;

    public static bool IsPaused => menuState != MenuState.Game;

    [Header("Post Processing")]
    public PostProcessProfile gameProfile;
    public PostProcessProfile pauseProfile;

    [Header("GameObjects")]
    public GameObject inventory;
    public GameObject pauseMenu;

    [Header("Buttons")]
    public Button resumeButton;
    public Button inventoryButton;
    public Button quitButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resumeButton.onClick.AddListener(ContinueGame);
        inventoryButton.onClick.AddListener(InventoryMenu);
        quitButton.onClick.AddListener(Application.Quit);

        menuState = MenuState.Game;
        ContinueGame();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch(menuState)
            {
                case MenuState.Game:
                    PauseMenu();
                    break;
                case MenuState.Pause:
                    ContinueGame();
                    break;
                case MenuState.Inventory:
                    PauseMenu();
                    break;
            }
        }
    }

    void InventoryMenu()
    {
        inventory.SetActive(true);
        pauseMenu.SetActive(false);
        menuState = MenuState.Inventory;
        Camera.main.GetComponent<PostProcessVolume>().profile = pauseProfile;
    }

    void PauseMenu()
    {
        Time.timeScale = 0f;
        inventory.SetActive(false);
        pauseMenu.SetActive(true);
        menuState = MenuState.Pause;
        Camera.main.GetComponent<PostProcessVolume>().profile = pauseProfile;
    }

    void ContinueGame()
    {
        Time.timeScale = 1f;
        inventory.SetActive(false);
        pauseMenu.SetActive(false);
        menuState = MenuState.Game;
        Camera.main.GetComponent<PostProcessVolume>().profile = gameProfile;
    }
}
