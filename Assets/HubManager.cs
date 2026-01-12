using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;

public class HubManager : MonoBehaviour
{
    [Tooltip("The area in which the player can interact with the gun bench/inventory")]public Bounds gunBenchUseArea;
    GameObject gunBenchButtonUI;
    
    [Tooltip("The area in which guns are enabled")]public Bounds shootingArea;

    public static bool inGunBenchArea, inShootingArea;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gunBenchUseArea.size += Vector3.forward * 999f;
        shootingArea.size += Vector3.forward * 999f;
        gunBenchButtonUI = GameObject.Find("Gun Bench Use");
    }

    // Update is called once per frame
    void Update()
    {
        if(MenuManager.menuState == MenuManager.MenuState.Pause || MissionManager.isTransitioning) return;

        //If in the gun bench area (which is when the gun bench exists and the use area contains player), enable the UI if it exists, and set the follow profile
        inGunBenchArea = gunBenchUseArea.Contains(player.position);
        gunBenchButtonUI.SetActive(inGunBenchArea);
        if (inGunBenchArea)
        {
            Camera.main.GetComponent<CameraFollow>().ImportFollowProfile(CameraFollow.allProfiles["Gun Bench Profile"]);

            if(Input.GetKeyDown(Keybinds.interact))
            {
                if(MenuManager.menuState == MenuManager.MenuState.Game)
                    GameObject.Find("Game Manager").GetComponent<MenuManager>().OpenInventoryMenu();
                else if(MenuManager.menuState == MenuManager.MenuState.Inventory)
                    GameObject.Find("Game Manager").GetComponent<MenuManager>().ContinueGame();
            }
        }

        inShootingArea = shootingArea.Contains(player.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(gunBenchUseArea.center, gunBenchUseArea.size);

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(shootingArea.center, shootingArea.size);
    }
}
