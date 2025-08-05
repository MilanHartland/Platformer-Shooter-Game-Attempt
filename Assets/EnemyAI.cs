using System.Collections.Generic;
using UnityEngine;
using MilanUtils;
using System.Linq;
using System;
using System.Collections;

[RequireComponent(typeof(Pathfinding), typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public float moveSpeed;
    public float maxJumpHeight;

    Pathfinding pathfinder;
    Rigidbody2D rb;
    Transform player;
    Vector2 pathTarget;

    List<Vector3> path = new();
    Vector2 curTarget, prevTarget, startPosition;
    RaycastHit2D line;

    [Tooltip("The bounds of the ground check boxcast"), SerializeField] private Bounds groundCheck;
    public bool grounded { get; private set; } = false;

    Timer lastTargetChangeTimer, lastSeenTimer;
    RaycastHit2D[] vision = new RaycastHit2D[4];

    int frameCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathfinder = GetComponent<Pathfinding>();
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.Find("Player").transform;

        lastTargetChangeTimer = new(1f);
        lastSeenTimer = new(5f);

        startPosition = transform.position;

        StartCoroutine(UpdateAI());
    }

    // Update is called once per frame
    IEnumerator UpdateAI()
    {
        while (true)
        {
            frameCount++;

            vision[0] = Physics2D.Linecast(transform.position, player.position + 0.5f * new Vector3(player.lossyScale.x, player.lossyScale.y), ~(1 << gameObject.layer));
            vision[1] = Physics2D.Linecast(transform.position, player.position + 0.5f * new Vector3(-player.lossyScale.x, player.lossyScale.y), ~(1 << gameObject.layer));
            vision[2] = Physics2D.Linecast(transform.position, player.position + 0.5f * new Vector3(player.lossyScale.x, -player.lossyScale.y), ~(1 << gameObject.layer));
            vision[3] = Physics2D.Linecast(transform.position, player.position + 0.5f * new Vector3(-player.lossyScale.x, -player.lossyScale.y), ~(1 << gameObject.layer));

            bool seesPlayer = false;
            foreach (RaycastHit2D hit in vision) { if (!hit.collider) { pathTarget = player.position; seesPlayer = true; } }
            if (seesPlayer)
            {
                pathTarget = player.position;
                lastSeenTimer.ResetTimer();
                lastTargetChangeTimer.EndTimer();
            }

            if (lastTargetChangeTimer)
            {
                if (!seesPlayer) SetPath(player.position);
                lastTargetChangeTimer.ResetTimer();
            }

            if (lastSeenTimer)
            {
                pathTarget = startPosition;
            }

            if (frameCount % 5 == 0) SetPath(pathTarget);

            MoveTowardsTarget();

            yield return new WaitForFixedUpdate();
        }
    }

    void MoveTowardsTarget()
    {
        //If there are no more points in the path, return. Then, get the LineCast between this object and all points
        if (path.Count <= 0) return;
        GetAllLines();

        //If target is less than 0.5 units above transform, set x velocity. Otherwise, set it to 0
        if (curTarget.y - transform.position.y <= 0.5f)
            rb.linearVelocityX = GetXVelocity(Angle2D.GetAngle<Vector2>(transform.position, curTarget).x);
        else rb.linearVelocityX = 0f;

        if (path.Count < 2) return;

        //.First gives error if nothing found, so try-catch. OverlapBoxAll because idk if otherwise it'd get something other than the map
        try { grounded = Physics2D.OverlapBoxAll(transform.position + groundCheck.center, groundCheck.size, 0f).First(x => x.CompareTag("Map")); }
        catch { grounded = false; }

        //If the linecast to current and next are both unobstructed, or this is close enough, set next item as target
        if (Vector2.Distance(transform.position, curTarget) <= 0.05f) GetNext();

        //If grounded and target is more than 0.5 above transform, jump x tiles (calculated with the sqrt function). If not grounded, set x velocity
        if (grounded && curTarget.y - transform.position.y > 0.5f)
            rb.linearVelocityY = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * Mathf.Clamp((curTarget.y + 0.5f) - transform.position.y, 0f, maxJumpHeight));
        else if (!grounded && !line && curTarget.y - transform.position.y > 0.5f)
            rb.linearVelocityX = GetXVelocity(Angle2D.GetAngle<Vector2>(transform.position, prevTarget).x);
    }

    void SetPath(Vector3 target){ path = pathfinder.FindPath(transform.position, target); }

    void GetNext() { prevTarget = path[0]; curTarget = path[1]; path.RemoveAt(0); lastTargetChangeTimer.ResetTimer(); }
    float GetXVelocity(float unsignedDir)
    {
        //If the distance moved in 1 fixedUpdate is longer than the distance, set velocity so that it is exactly the distance. Otherwise, move with max speed
        if (moveSpeed * Time.fixedDeltaTime >= Mathf.Abs(transform.position.x - curTarget.x))
            return (Mathf.Sign(unsignedDir) * Mathf.Abs(transform.position.x - curTarget.x)) / Time.fixedDeltaTime;
        else
            return Mathf.Sign(unsignedDir) * moveSpeed;
    }

    void GetAllLines()
    {
        line = Physics2D.Linecast(transform.position, curTarget, ~(1 << gameObject.layer));
    }

    void OnDrawGizmos()
    {
        player = GameObject.Find("Player").transform;
        Gizmos.DrawLine(transform.position, player.position + 0.5f * new Vector3(player.lossyScale.x, player.lossyScale.y));
        Gizmos.DrawLine(transform.position, player.position + 0.5f * new Vector3(-player.lossyScale.x, player.lossyScale.y));
        Gizmos.DrawLine(transform.position, player.position + 0.5f * new Vector3(player.lossyScale.x, -player.lossyScale.y));
        Gizmos.DrawLine(transform.position, player.position + 0.5f * new Vector3(-player.lossyScale.x, -player.lossyScale.y));
    }
}
