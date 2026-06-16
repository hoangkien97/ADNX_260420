using UnityEngine;
using Pathfinding;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Điều hướng A* Pathfinding và lật hình ảnh quái vật.
/// </summary>
[RequireComponent(typeof(Seeker))]
public class EnemyMovement : NetworkBehaviour
{
    private Enemy coordinator;
    private Seeker seeker;
    private SpriteRenderer spriteRenderer;

    private float pathUpdateInterval = 0.3f;
    private float waypointReachDistance = 0.15f;
    private float retargetInterval = 1f;
    private float nextRetargetTime = 0f;

    private Player targetPlayer;
    private Path currentPath;
    private int currentWaypoint;
    private float nextPathRequestTime;
    private float movementPlaneZ;

    private void Awake()
    {
        coordinator = GetComponent<Enemy>();
        seeker = GetComponent<Seeker>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        targetPlayer = FindNearestPlayer();
        if (targetPlayer != null)
        {
            movementPlaneZ = targetPlayer.transform.position.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, movementPlaneZ);
        }
        ResetPathState();
        RequestPath();
    }

    private void OnDisable()
    {
        if (seeker != null && !seeker.IsDone()) seeker.CancelCurrentPathRequest();
        ResetPathState();
    }

    private void Update()
    {
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 0;

        // Chỉ Server mới tính toán AI tìm đường
        if (!isSpawned || isServer)
        {
            UpdatePathRequest();
            MoveToPlayer();
            FlipEnemy();
        }
    }

    private void MoveToPlayer()
    {
        if (targetPlayer == null)
        {
            targetPlayer = FindNearestPlayer();
            return;
        }

        UpdateZDepth();
        if (TryMoveAlongPath()) return;

        float speed = coordinator != null ? coordinator.MoveSpeed : 2f;
        Vector2 nextPosition = Vector2.MoveTowards(
            transform.position, targetPlayer.transform.position, speed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, movementPlaneZ);
    }

    private void UpdateZDepth()
    {
        float targetZ = transform.position.y * 0.001f;
        if (Mathf.Abs(transform.position.z - targetZ) > 0.0001f)
            transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
    }

    private void UpdatePathRequest()
    {
        if (AstarPath.active == null) return;
        
        if (targetPlayer == null || targetPlayer.IsDead)
        {
            targetPlayer = FindNearestPlayer();
            nextRetargetTime = Time.time + retargetInterval;
        }
        else if (Time.time >= nextRetargetTime)
        {
            Player nearest = FindNearestPlayer();
            if (nearest != null && nearest != targetPlayer) targetPlayer = nearest;
            nextRetargetTime = Time.time + retargetInterval;
        }
        
        if (targetPlayer == null) return;
        if (Time.time < nextPathRequestTime) return;
        
        RequestPath();
    }

    private void RequestPath()
    {
        if (AstarPath.active == null || seeker == null || targetPlayer == null) return;
        if (!seeker.IsDone()) return;
        
        nextPathRequestTime = Time.time + pathUpdateInterval;
        int graphMask = MultiplayerGridManager.GetGraphMaskForPlayer(targetPlayer);
        
        if (graphMask != -1) 
            seeker.StartPath(transform.position, targetPlayer.transform.position, OnPathComplete, graphMask);
        else 
            seeker.StartPath(transform.position, targetPlayer.transform.position, OnPathComplete);
    }

    private void OnPathComplete(Path path)
    {
        if (!isActiveAndEnabled || path == null || path.error || path.vectorPath == null || path.vectorPath.Count == 0)
        {
            currentPath = null;
            return;
        }
        if (targetPlayer != null)
        {
            Vector3 pathEndPos = path.vectorPath[path.vectorPath.Count - 1];
            float distanceToRealTarget = Vector2.Distance(pathEndPos, targetPlayer.transform.position);
            if (distanceToRealTarget > 2f)
            {
                currentPath = null;
                return;
            }
        }
        currentPath = path;
        currentWaypoint = 0;
    }

    private bool TryMoveAlongPath()
    {
        if (currentPath == null || currentPath.vectorPath == null || currentPath.vectorPath.Count == 0) return false;
        
        while (currentWaypoint < currentPath.vectorPath.Count - 1 &&
               Vector2.Distance(transform.position, ToMovementPlanePoint(currentPath.vectorPath[currentWaypoint])) <= waypointReachDistance)
        {
            currentWaypoint++;
        }
        
        Vector3 targetPoint = ToMovementPlanePoint(currentPath.vectorPath[currentWaypoint]);
        if (float.IsNaN(targetPoint.x) || float.IsNaN(targetPoint.y) || float.IsInfinity(targetPoint.x) || float.IsInfinity(targetPoint.y)) return false;
        
        float speed = coordinator != null ? coordinator.MoveSpeed : 2f;
        Vector2 nextPosition = Vector2.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, movementPlaneZ);
        return true;
    }

    private void ResetPathState()
    {
        currentPath = null;
        currentWaypoint = 0;
        nextPathRequestTime = Time.time;
    }

    private Vector3 ToMovementPlanePoint(Vector3 pathPoint) => new Vector3(pathPoint.x, pathPoint.y, movementPlaneZ);

    private Player FindNearestPlayer()
    {
        Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
        Player nearest = null;
        float minDist = float.MaxValue;
        foreach (Player p in allPlayers)
        {
            if (p == null || p.IsDead || !p.gameObject.activeInHierarchy) continue;
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = p;
            }
        }
        return nearest;
    }

    private void FlipEnemy()
    {
        if (targetPlayer == null || targetPlayer.IsDead) targetPlayer = FindNearestPlayer();
        if (targetPlayer != null && spriteRenderer != null)
        {
            spriteRenderer.flipX = targetPlayer.transform.position.x < transform.position.x;
        }
    }
}
