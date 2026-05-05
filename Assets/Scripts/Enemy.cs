    using UnityEngine;
using UnityEngine.UI;
using Pathfinding;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyDataSO enemyData;

    [SerializeField] private float enemyMoveSpeed;
    private float pathUpdateInterval = 0.3f;
    private float waypointReachDistance = 0.15f;

    protected Player player;
    [SerializeField] protected float maxHp ;
    protected float currentHp;
    [SerializeField] private Image hpBar;
    [SerializeField] protected float enterDamege;
    [SerializeField] protected float stayDamege;
    protected EnemySpawner spawner;

    private void ApplyDataSO()
    {
        if (enemyData == null) return;
        enemyMoveSpeed = enemyData.moveSpeed;
        maxHp          = enemyData.maxHp;
        enterDamege    = enemyData.enterDamage;
        stayDamege     = enemyData.stayDamage;
    }

    protected GameObject GetDropPrefab()   => enemyData != null ? enemyData.dropPrefab  : null;
    protected float      GetDropLifetime() => enemyData != null ? enemyData.dropLifetime : 7f;

    private SpriteRenderer spriteRenderer;
    private Seeker seeker;
    private Path currentPath;
    private int currentWaypoint;
    private float nextPathRequestTime;
    private float movementPlaneZ;

    private float baseMoveSpeed;
    private float baseMaxHp;
    private float baseEnterDamage;
    private float baseStayDamage;
    private bool statsInitialized = false;

    protected virtual void Awake()
    {
        ApplyDataSO();
        spriteRenderer = GetComponent<SpriteRenderer>();
        seeker = GetComponent<Seeker>();
        if (seeker == null)
        {
            seeker = gameObject.AddComponent<Seeker>();
        }
    }

    protected virtual void OnEnable()
    {
        ApplyDataSO();          
        statsInitialized = false; 
        player = FindAnyObjectByType<Player>();
        movementPlaneZ = player != null ? player.transform.position.z : 0f;
        transform.position = new Vector3(transform.position.x, transform.position.y, movementPlaneZ);
        ResetPathState();
        RequestPath();
    }

    protected virtual void OnDisable()
    {
        if (seeker != null && !seeker.IsDone())
        {
            seeker.CancelCurrentPathRequest();
        }

        ResetPathState();
    }

    public void Initialize(EnemySpawner spawner)
    {
        this.spawner = spawner;
    }

    public void ApplyStatMultiplier(float multiplier)
    {
        if (!statsInitialized)
        {
            baseMoveSpeed = enemyMoveSpeed;
            baseMaxHp = maxHp;
            baseEnterDamage = enterDamege;
            baseStayDamage = stayDamege;
            statsInitialized = true;
        }

        enemyMoveSpeed = baseMoveSpeed * multiplier;
        maxHp = baseMaxHp * multiplier;
        enterDamege = baseEnterDamage * multiplier;
        stayDamege = baseStayDamage * multiplier;

        currentHp = maxHp;
        UpdateHpBar();
    }

    protected virtual void Start()
    {
        player = FindAnyObjectByType<Player>();
        movementPlaneZ = player != null ? player.transform.position.z : 0f;
        transform.position = new Vector3(transform.position.x, transform.position.y, movementPlaneZ);
        currentHp = maxHp;
        UpdateHpBar();
    }

    protected virtual void Update()
    {
        UpdatePathRequest();
        MoveToPlayer();
        FlipEnemy();
    }

    protected void MoveToPlayer()
    {
        if (player == null)
        {
            return;
        }

        if (TryMoveAlongPath())
        {
            return;
        }
        if (AstarPath.active != null)
        {
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(transform.position, player.transform.position, enemyMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, movementPlaneZ);
    }

    private void UpdatePathRequest()
    {
        if (AstarPath.active == null)
        {
            return;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
            return;
        }

        if (Time.time < nextPathRequestTime)
        {
            return;
        }

        RequestPath();
    }

    private void RequestPath()
    {
        if (AstarPath.active == null || seeker == null || player == null)
        {
            return;
        }

        if (!seeker.IsDone())
        {
            return;
        }

        nextPathRequestTime = Time.time + pathUpdateInterval;
        seeker.StartPath(transform.position, player.transform.position, OnPathComplete);
    }

    private void OnPathComplete(Path path)
    {
        if (!isActiveAndEnabled || path == null || path.error || path.vectorPath == null || path.vectorPath.Count == 0)
        {
            currentPath = null;
            return;
        }

        currentPath = path;
        currentWaypoint = 0;
    }

    private bool TryMoveAlongPath()
    {
        if (currentPath == null || currentPath.vectorPath == null || currentPath.vectorPath.Count == 0)
        {
            return false;
        }

        while (currentWaypoint < currentPath.vectorPath.Count - 1 &&
               Vector2.Distance(transform.position, ToMovementPlanePoint(currentPath.vectorPath[currentWaypoint])) <= waypointReachDistance)
        {
            currentWaypoint++;
        }

        Vector3 targetPoint = ToMovementPlanePoint(currentPath.vectorPath[currentWaypoint]);
        if (float.IsNaN(targetPoint.x) || float.IsNaN(targetPoint.y) || float.IsInfinity(targetPoint.x) || float.IsInfinity(targetPoint.y))
        {
            return false;
        }
        Vector2 nextPosition = Vector2.MoveTowards(transform.position, targetPoint, enemyMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, movementPlaneZ);
        return true;
    }

    private void ResetPathState()
    {
        currentPath = null;
        currentWaypoint = 0;
        nextPathRequestTime = Time.time;
    }

    private Vector3 ToMovementPlanePoint(Vector3 pathPoint)
    {
        return new Vector3(pathPoint.x, pathPoint.y, movementPlaneZ);
    }

    protected void FlipEnemy()
    {
        if (player != null)
        {
            spriteRenderer.flipX = player.transform.position.x < transform.position.x;
        }

        if (hpBar != null)
        {
            hpBar.transform.rotation = Quaternion.identity;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateHpBar();
        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        GameManager.AddScore();

        if (spawner != null)
        {
            spawner.OnEnemyDied();
            spawner.ReturnEnemyToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHp / maxHp;
        }
    }
}
