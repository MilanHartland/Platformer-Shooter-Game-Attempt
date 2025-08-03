using System.Collections.Generic;
using UnityEngine;
using MilanUtils;

[RequireComponent(typeof(Pathfinding), typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public float moveSpeed;
    public float maxJumpVel;

    Pathfinding pathfinder;
    Rigidbody2D rb;
    Transform player;

    Queue<Vector3> path;
    Vector2 curTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathfinder = GetComponent<Pathfinding>();
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.Find("Player").transform;

        path = new(pathfinder.FindPath(transform.position, player.position));
        curTarget = path.Dequeue();
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Vector2.Distance(transform.position, curTarget) <= 0.05f) curTarget = path.Dequeue();
        rb.linearVelocity = Angle2D.GetAngle<Vector3, Vector2>(transform.position, curTarget) * moveSpeed;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        foreach (var a in path) Gizmos.DrawCube(a, Vector3.one);
    }
}
