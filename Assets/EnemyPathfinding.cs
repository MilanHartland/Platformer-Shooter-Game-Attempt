using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MilanUtils;
using static MilanUtils.Trajectory;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPathfinding : MonoBehaviour
{
    public float speed;
    public float maxJumpHeight;

    Rigidbody2D rb;

    public static Dictionary<Vector3, List<Vector3>> pathGraph = new();
    [HideInInspector] public List<Vector3> path { get; private set; } = new();
    Vector3 curTarget = Vector3.zero;

    public Tilemap map;
    public Bounds floorBounds;
    bool grounded => Physics2D.OverlapBox(transform.position + floorBounds.center, floorBounds.size, 0f, mask);

    Pathfinding.ConnectionRequirements graphConnectionRequirements;

    LayerMask mask;

    Vector3 pfCenterRelative => Vector3.down * (1f / transform.lossyScale.y);
    Vector3 pfCenter => transform.position + pfCenterRelative;

    [HideInInspector]public bool isPathfinding;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GenerateMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) StartPathfindCoroutine(World.mousePos);
        // if (Input.GetKeyDown(KeyCode.X)) rb.linearVelocity = GetJumpToConstantSpeed(pfCenter, World.mousePos2D, speed);
        // if (Input.GetKeyDown(KeyCode.C)) pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
    }

    public void StopPathfinding(){rb.linearVelocityX = 0f; path = new(); StopAllCoroutines(); isPathfinding = false;}

    public void Pathfind(Vector3 target)
    {
        //If no path, or there is no unobstructed line in LinecastMultiple (in this case a linecast from this position and 1 higher towards the path), reset path
        if (path.Count == 0)
        {
            path = Pathfinding.Dijkstra(pfCenter, target, pathGraph);
            return;
        }

        List<Vector3> newPath = Pathfinding.Dijkstra(path[0], target, pathGraph);
        if(Vector2.Distance(pfCenter, path[0]) <= .1f) path.RemoveAt(0);
        if(Vector2.Distance(pfCenter, newPath[0]) <= .1f) newPath.RemoveAt(0);
        
        Vector3 firstCur = path[0];
        path = newPath;
        path.Insert(0, firstCur);

        StopAllCoroutines();
        StartPathfindCoroutine(target, false);
    }

    void StartPathfindCoroutine(Vector3 target, bool getPath = true) { StartCoroutine(PathfindCoroutine(target, getPath)); }
    IEnumerator PathfindCoroutine(Vector3 target, bool getPath = true)
    {
        isPathfinding = true;

        //Sets the bool to true and gets the path
        if (getPath) path = Pathfinding.Dijkstra(pfCenter, target, pathGraph);

        if (path.Count < 1) { Debug.Log("NO PATH"); rb.linearVelocityX = 0f; yield break; }
        curTarget = path[0]; path.RemoveAt(0);

        while (Vector2.Distance(pfCenter, target) > 0.05f)
        {
            if(MenuManager.IsPaused) yield return null;

            if (Vector2.Distance(curTarget, pfCenter) < 0.1f)
            {
                transform.position = curTarget - pfCenterRelative;
                if (path.Count > 0)
                {
                    curTarget = path[0];
                    path.RemoveAt(0);
                }
                else { rb.linearVelocityX = 0f; yield break; }
            }

            Vector2 diff = curTarget - pfCenter;

            if (diff.y > 0.1f)
            {
                //If on the ground and there is a y difference, jump
                if (grounded)
                    rb.linearVelocity = GetJumpToConstantSpeed(pfCenter, curTarget, speed);

                if (Physics2D.Linecast(pfCenter, curTarget, mask))
                {
                    //If the x distance is between .6-1, stop moving horizontally
                    if (Mathf.Abs(diff.x) > .6f && Mathf.Abs(diff.x) < 1f)
                        rb.linearVelocityX = 0f;
                    else if(Mathf.Abs(diff.x) < 1f)
                    {
                        Vector3 closest = Lists.GetClosest(pathGraph[curTarget], pfCenter);
                        curTarget = closest;
                        // rb.linearVelocityX = Mathf.Sign(closest.x - pfCenter.x) * speed;
                    }
                }
            }
            else
            {
                Vector2 jumpSpeed = GetJumpToConstantSpeed(pfCenter, curTarget, speed);
                //If grounded and no raycast next position downward (so when a gap) and there is a significant enough x difference
                if (grounded && !Physics2D.Raycast(pfCenter + new Vector3(Mathf.Sign(diff.x) * .8f, 0f), Vector2.down, 1f, mask) && Mathf.Abs(diff.x) > 0.45f && jumpSpeed.y > 0)
                {
                    rb.linearVelocity = jumpSpeed;
                }
                //If next frame would not go over target, move
                else if (Mathf.Abs(diff.x) > speed * Time.fixedDeltaTime)
                {
                    rb.linearVelocityX = Mathf.Sign(diff.x) * speed;
                }
                //If next frame would go over target, teleport to target and reset x
                else if (Mathf.Abs(diff.x) < speed * Time.fixedDeltaTime)
                {
                    rb.linearVelocityX = 0f;
                    transform.position = new(curTarget.x, transform.position.y);
                }
            }

            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * Mathf.Sign(rb.linearVelocityX), transform.localScale.y, transform.localScale.z);

            yield return new WaitForFixedUpdate();
        }

        StopPathfinding();
    }

    //ChatGPT code
    float GetJumpVelocity(Vector3 start, Vector3 target)
    {
        Vector2 diff = target - start;
        float dx = diff.x;
        float dy = diff.y + .1f;

        float g = -Physics2D.gravity.y;               // positive gravity magnitude
        float tHoriz = Mathf.Abs(dx) / speed;         // minimum time so horizontal speed won't exceed `speed`

        // time where vertical initial velocity is minimized while still landing descending
        float tStationary = 0f;
        if (dy > 0f) tStationary = Mathf.Sqrt(2f * dy / g);

        // smallest feasible flight time that satisfies both horizontal limit and descending condition
        float t = Mathf.Max(tHoriz, tStationary);

        // compute required initial vertical velocity for that flight time
        float v0 = (dy + 0.5f * g * t * t) / t;
        return v0;
    }
    
    float GetJumpHeight(Vector3 start, Vector3 target)
    {
        float vel = GetJumpVelocity(start, target);
        return (0.5f * vel * vel) / -Physics2D.gravity.y;
    }

    [InspectorButton("Generate Map")]
    void GenerateMap()
    {
        mask = 1 << LayerMask.NameToLayer("Map");

        graphConnectionRequirements = (start, end) =>
        {
            Vector3 trueStart = start;
            start -= pfCenterRelative;

            bool clearLine = !Physics2D.Linecast(start, end, mask);
            Vector2 diff = end - start;
            Vector2 trueDiff = end - trueStart;
            bool corrY = diff.y <= maxJumpHeight;

            bool startIsAbove = trueDiff.y < 0;
            bool isSame = Mathf.Abs(trueDiff.y) < 0.05f;
            bool canWalk = false;
            bool fallRayUnobstructed = false;
            bool canJump = false;

            //Gets the raycast list for a jump
            var hitList = GetJumpRayList();
            bool jumpUnobstructed = !hitList.Any((hit) => { return hit; });

            if (startIsAbove)
            {
                //Gets the linecast 1 unit to the left/right of the start. If either is unobstructed, that means a fall is possible (disregarding x distance)
                bool lineLeft = !Physics2D.Linecast(trueStart + Vector3.left * 0.5f, end, mask);
                bool lineRight = !Physics2D.Linecast(trueStart + Vector3.right * 0.5f, end, mask);

                fallRayUnobstructed = lineLeft || lineRight;
            }
            
            if (isSame)
            {
                //Checks if there is a gap by doing a raycastline downward and returning if on any there is no hit
                List<RaycastHit2D> raycastLine = World.RaycastLine(trueStart, new(Mathf.Sign(diff.x), 0f), Mathf.RoundToInt(Mathf.Abs(diff.x) - 1), Vector2.down, mask);
                bool hasGap = raycastLine.Any((hit) => { return !hit; });

                //If there is a gap, set canWalk to false, check if jump is allowed (GetJumpRayList and HasCondition), then set canJump. If no gap, then canWalk is true
                if (hasGap)
                {
                    canWalk = false;
                    corrY = GetJumpHeight(trueStart, end) <= maxJumpHeight;

                    canJump = jumpUnobstructed && clearLine;
                }
                else canWalk = clearLine && !Physics2D.Linecast(trueStart, end, mask) && !Physics2D.Raycast(start, new Vector2(diff.x, 0f), Mathf.Abs(diff.x), mask);
            }
            
            if(!startIsAbove && !isSame)
            {
                //Gets the height of the jump that would happen between start/end and check if maxJumpHeight allows it
                corrY = GetJumpHeight(trueStart, end) <= maxJumpHeight;

                //Gets the linecast .75 units to the left/right of the end. If either is unobstructed, that means a jump is possible (disregarding x distance)
                bool lineLeft = !Physics2D.Linecast(end + Vector3.left * .75f, start, mask);
                bool lineRight = !Physics2D.Linecast(end + Vector3.right * .75f, start, mask);
                bool otherLine = lineLeft || lineRight;
                
                //Checks if the x is different. If not, then you would hit the tile above
                bool differentX = start.x != end.x;

                canJump = jumpUnobstructed && differentX && (clearLine || otherLine);
            }
            
            //Checks if the x is good enough for falling, which is when the x difference (-.2 to account for tile size) is under the fall time * speed
            bool corrFallX = Mathf.Abs(diff.x) - 0.2f < GetTimeToFall(Mathf.Abs(diff.y)) * speed;
            //Checks if the fall would be unobstructed
            bool corrFallLines = !GetFallRayList().Any(x => {return x;});

            //Checks if the x is good enough for jumping, which is when the x difference is below the max jump time (time to fall from jump height * 2 because up-down, multiplied by speed) * speed
            float maxJumpTime = 2f * GetTimeToFall(GetJumpHeight(trueStart, end)) * speed;
            bool corrJumpX = Mathf.Abs(diff.x) < maxJumpTime * speed;
            
            //Checks if can fall according to raycasts and max fall distance
            bool canFall = fallRayUnobstructed && (corrFallX || (canJump && corrJumpX)) && corrFallLines;

            //Checks if the x is good enough in general, which is if can fall, can jump (with correct x for jump), or can walk
            bool corrX = canFall || (canJump && corrJumpX) || canWalk;
            
            if(end == new Vector3(13, 3) && trueStart == new Vector3(21, 5)) print(corrX);
            return (canWalk || canJump || fallRayUnobstructed) && corrX && corrY;

            List<RaycastHit2D> GetJumpRayList()
            {
                //Gets the raycast variables
                Vector2 rayStart = start + new Vector3(0f, transform.lossyScale.y * .5f);
                Vector2 moveDir = new(Mathf.Sign(diff.x), 0f);
                int moveCount = Mathf.RoundToInt(Mathf.Abs(diff.x) - 1);

                //For each move to be made, add 2 things: a raycast from (start + move i steps) up to the height of the jump, and a linecast from that height to next height
                List<RaycastHit2D> hitList = new();
                for (int i = 0; i <= moveCount; i++)
                {
                    float heightChange = GetHeightChangeAfterTime(GetJumpVelocity(start, end), (float)i / speed);
                    float heightChangeNext = GetHeightChangeAfterTime(GetJumpVelocity(start, end), (float)(i + 1) / speed);

                    hitList.Add(Physics2D.Raycast(rayStart + moveDir * i, Vector2.up, heightChange, mask));
                    hitList.Add(Physics2D.Linecast(rayStart + moveDir * i + Vector2.up * heightChange, rayStart + moveDir * i + Vector2.up * heightChangeNext, mask));
                }
                
                return hitList;
            }

            List<RaycastHit2D> GetFallRayList()
            {
                //Gets the raycast variables
                Vector2 rayStart = start + new Vector3(0f, transform.lossyScale.y * .5f);
                Vector2 moveDir = new(Mathf.Sign(diff.x), 0f);
                int moveCount = Mathf.RoundToInt(Mathf.Abs(diff.x) - 1);

                //For each move to be made, add 2 things: a raycast from (start + move i steps) up to the height of the jump, and a linecast from that height to next height
                List<RaycastHit2D> hitList = new();
                for (int i = 0; i <= moveCount; i++)
                {
                    float heightChange = GetHeightChangeAfterTime(0f, (float)i / speed);
                    float heightChangeNext = GetHeightChangeAfterTime(0f, (float)(i + 1) / speed);

                    hitList.Add(Physics2D.Raycast(rayStart + moveDir * i, Vector2.up, heightChange, mask));
                    hitList.Add(Physics2D.Linecast(rayStart + moveDir * i + Vector2.up * heightChange, rayStart + moveDir * i + Vector2.up * heightChangeNext, mask));
                }

                return hitList;
            }
        };
        pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
    }

    [ContextMenu("Log Path")]
    void LogPath()
    {
        string log = string.Empty;
        for(int i = 0; i < path.Count; i++)
        {
            log += path[i];
        }
        Debug.Log(log);
    }
    
    public bool drawGraphGizmos;
    public bool drawPathGizmos;
    #pragma warning disable
    void OnDrawGizmos()
    {
        if(drawGraphGizmos)
        {
            Gizmos.color = Color.white;
            foreach (var obj in pathGraph.Keys)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawCube(obj, Vector3.one * 0.5f);
                
                foreach (var b in pathGraph[obj])
                {
                    bool twoWay = pathGraph[b].Contains(obj);
                    if (twoWay) Gizmos.color = Color.white;
                    else if (obj.y > b.y) Gizmos.color = new Color(.7f, 1f, .7f, 1f);
                    else if (obj.y < b.y) Gizmos.color = new Color(.7f, .7f, 1f, 1f);

                    Gizmos.DrawLine(obj, b);
                }
            }
        }

        if(drawPathGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(curTarget, Vector3.one);

            Gizmos.color = Color.blue;
            if (path.Count < 1) return;
            Gizmos.DrawLine(pfCenter, curTarget); Gizmos.DrawLine(curTarget, path[0]);
            if (path.Count < 2) return;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
                Gizmos.DrawWireCube(path[i], 0.25f * Vector3.one);
            }
            Gizmos.DrawWireCube(path[^1], 0.25f * Vector3.one);
        }
    }
}