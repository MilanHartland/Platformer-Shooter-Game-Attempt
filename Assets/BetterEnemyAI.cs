using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MilanUtils;
using UnityEngine;

public class BetterEnemyAI : MonoBehaviour
{
    public float speed;
    public float maxJumpHeight;

    Rigidbody2D rb;

    public Dictionary<Vector3, List<Vector3>> pathGraph = new();
    List<Vector3> path = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GenerateMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) GetPath(transform.position, World.mousePos);
    }

    void Pathfind(Vector3 target) { StartCoroutine(PathfindCoroutine(target)); }
    IEnumerator PathfindCoroutine(Vector3 target)
    {
        Vector3 curTarget = target;
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            RaycastHit2D lineObstructed = Physics2D.Linecast(transform.position, target, ~(1 << gameObject.layer));
            if (lineObstructed)
            {

            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void JumpTo(Vector3 target)
    {
        //MAKE THE ENEMY CALCULATE THE VELOCITY OR HEIGHT FOR THE JUMP WHEN MOVING AT CONSTANT SPEED
        //ALTERNATIVELY CHEAT IT SOMEHOW (then the problem'd be how to cheat lol)

        Vector2 diff = target - transform.position;

        float g = -Physics2D.gravity.y;
        float fullTime = Mathf.Abs(diff.x) / speed;
        Vector2 vel = new(Mathf.Sign(diff.x) * speed, (diff.y + 0.5f * g * fullTime * fullTime) / fullTime);

        rb.linearVelocity = vel;
    }

    Vector3 GetClosestNode()
    {
        float min = Mathf.Infinity; Vector3 closest = Vector3.positiveInfinity;
        foreach (Vector3 vec in pathGraph.Keys) { if (Vector2.Distance(transform.position, vec) <= min) { closest = vec; } }
        return closest;
    }

    void GenerateMap()
    {
        List<Vector3> allNodes = new();
        foreach (Transform child in GameObject.Find("Path Node Parent").transform)
        { child.position = Vector3Int.RoundToInt(child.position); allNodes.Add(child.position); }

        pathGraph = new();
        foreach (Vector3 a in allNodes)
        {
            pathGraph.Add(a, new());
            foreach (Vector3 b in allNodes)
            {
                if (a == b) continue;
                RaycastHit2D hit = Physics2D.Linecast(a, b, ~(1 << gameObject.layer));
                if (!hit) { pathGraph[a].Add(b); }
            }
        }
    }

    class PathNode
    {
        public Vector3 pos;
        public float dist;
        public PathNode host;
    }

    void GetPath(Vector3 start, Vector3 end)
    {
        GenerateMap();

        float lowestEndDist = Mathf.Infinity; float lowestStartDist = Mathf.Infinity; Vector3 newEnd = end; Vector3 newStart = start;
        foreach (Vector3 node in pathGraph.Keys)
        {
            float startDist = Vector2.Distance(node, start); if (startDist < lowestStartDist) { lowestStartDist = startDist; newStart = node; }
            float endDist = Vector2.Distance(node, end); if (endDist < lowestEndDist) { lowestEndDist = endDist; newEnd = node; }
        }
        start = newStart; end = newEnd;

        Dictionary<Vector3, PathNode> pathNodes = new();
        List<PathNode> unvisited = new() { new() { pos = start } };

        foreach (Vector3 v in pathGraph.Keys) { pathNodes.Add(v, new() { pos = v, dist = Mathf.Infinity, host = null }); }

        PathNode cur = null;
        for (int i = 0; i < 1000; i++)
        {
            unvisited.Sort((x, y) => x.dist.CompareTo(y.dist));
            if (unvisited[0].dist == Mathf.Infinity) { path = new(); return; }
            cur = unvisited[0];
            unvisited.Remove(cur);

            print($"{cur.pos} {end}");
            if (cur.pos == end) { path = new(); RetracePath(cur); break; }

            foreach (Vector3 connection in pathGraph[cur.pos])
            {
                PathNode con = pathNodes[connection];
                float newDist = cur.dist + Vector2.Distance(cur.pos, con.pos);
                PathNode newNode = new() { pos = connection, dist = newDist, host = cur };

                try { unvisited.First(x => x.pos == connection); } catch { unvisited.Add(newNode);}

                if (con.dist > newDist) con = newNode;
            }
        }

        path.Reverse();
        return;

        void RetracePath(PathNode cur)
        {
            path.Add(cur.pos);
            if (cur.host != null) RetracePath(cur.host);
        }
    }

    void OnDrawGizmos()
    {
        // foreach (var obj in pathGraph)
        // {
        //     foreach (var listObj in obj.Value)
        //     {
        //         Gizmos.DrawLine(obj.Key, listObj);
        //     }
        // }

        foreach(var obj in pathGraph){ Gizmos.DrawCube(obj.Key, Vector3.one * 0.5f); }
        
        // for (int i = 0; i < path.Count; i++)
        // {
        //     if (i < path.Count - 1) Gizmos.DrawLine(path[i], path[i + 1]);
        // }
    }
}
