using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMotor : MonoBehaviour
{
    private NavMeshAgent agent;
    public NavMeshAgent Agent => agent;
    public bool IsPathBlocked { get; private set; }
    public Building BlockedBy { get; private set; }

    // How many consecutive PathComplete reads before we trust the path is open
    private int pathClearFrames = 0;
    private const int PATH_CLEAR_FRAMES_REQUIRED = 3;
    public bool IsConfirmedClear { get; private set; }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    // Returns true if the path just became confirmed clear (wall destroyed)
    public bool UpdatePath(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(destination, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            pathClearFrames++;
            IsConfirmedClear = pathClearFrames >= PATH_CLEAR_FRAMES_REQUIRED;

            if (IsConfirmedClear)
            {
                IsPathBlocked = false;
                BlockedBy = null;
                agent.SetPath(path);
            }
            return IsConfirmedClear;
        }
        else
        {
            pathClearFrames = 0;
            IsConfirmedClear = false;
            IsPathBlocked = true;
            IdentifyWall(path, destination);
            agent.SetPath(path); // Move as far as possible
            return false;
        }
    }

    private void IdentifyWall(NavMeshPath path, Vector3 targetPos)
    {
        // Walk path segments first
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector2 from = path.corners[i];
            Vector2 to   = path.corners[i + 1];
            Vector2 dir  = (to - from).normalized;
            float dist   = Vector2.Distance(from, to);

            RaycastHit2D[] hits = Physics2D.RaycastAll(from, dir, dist);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider.TryGetComponent<Building>(out Building b) &&
                    b.StructureType == StructureType.Wall)
                {
                    BlockedBy = b;
                    return;
                }
            }
        }

        // Fallback: check near the end of the partial path
        if (path.corners.Length > 0)
        {
            Vector3 pathEnd = path.corners[path.corners.Length - 1];
            Collider2D[] nearby = Physics2D.OverlapCircleAll(pathEnd, 2f);

            Building closest = null;
            float minDist = Mathf.Infinity;

            foreach (var col in nearby)
            {
                if (!col.TryGetComponent<Building>(out Building b)) continue;
                if (b.StructureType != StructureType.Wall) continue;
                float d = Vector3.Distance(pathEnd, b.transform.position);
                if (d < minDist) { minDist = d; closest = b; }
            }
            BlockedBy = closest;
        }
    }

    public void MoveToward(Vector3 destination)
    {
        if (!agent.isStopped)
        {
            if (Vector3.Distance(agent.destination, destination) > 0.2f)
                agent.SetDestination(destination);
        }
    }

    public void ModifySpeed(float multiplier) => agent.speed *= multiplier;
    public void Stop()   => agent.isStopped = true;
    public void Resume() => agent.isStopped = false;
}