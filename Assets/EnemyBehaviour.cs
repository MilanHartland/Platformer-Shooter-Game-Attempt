using UnityEngine;
using MilanUtils;
using static MilanUtils.Objects;

public class EnemyBehaviour : MonoBehaviour
{
    RaycastHit2D[] visionCasts = new RaycastHit2D[9];
    public LayerMask mask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        print(player.position);
        Vector3 playerScale = player.lossyScale / 2f;
        visionCasts[0] = Physics2D.Linecast(transform.position, player.position, mask);
        visionCasts[1] = Physics2D.Linecast(transform.position, player.position + playerScale, mask);
        visionCasts[2] = Physics2D.Linecast(transform.position, player.position - playerScale, mask);
        visionCasts[3] = Physics2D.Linecast(transform.position, player.position + new Vector3(playerScale.x, 0f), mask);
        visionCasts[4] = Physics2D.Linecast(transform.position, player.position - new Vector3(playerScale.x, 0f), mask);
        visionCasts[5] = Physics2D.Linecast(transform.position, player.position + new Vector3(0f, playerScale.x), mask);
        visionCasts[6] = Physics2D.Linecast(transform.position, player.position - new Vector3(0f, playerScale.x), mask);
        visionCasts[7] = Physics2D.Linecast(transform.position, player.position + new Vector3(playerScale.x, -playerScale.y), mask);
        visionCasts[8] = Physics2D.Linecast(transform.position, player.position + new Vector3(-playerScale.x, playerScale.y), mask);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, visionCasts[0].point);
        // foreach(var hit in visionCasts)
        // {
        //     Gizmos.DrawLine(transform.position, hit.point);
        // }  
    }
}
