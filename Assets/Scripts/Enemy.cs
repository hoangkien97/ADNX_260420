using UnityEngine;
using UnityEngine.UI;
using Pathfinding;
using PurrNet;

/// <summary>
/// Enemy với PurrNet multiplayer support.
/// - A* Pathfinding chỉ chạy trên Server (Host)
/// - currentHp sync qua SyncVar → clients update HP bar
/// - TakeDamage chỉ xử lý trên Server
/// - Die → Server despawn + RpcSpawnLoot broadcast hiệu ứng
/// - Tìm player gần nhất trong số 4 players
/// </summary>
public class Enemy : NetworkBehaviour
{
    [SerializeField] private EnemyDataSO enemyData;

    private float enemyMoveSpeed;
    private float pathUpdateInterval = 0.3f;
    private float waypointReachDistance = 0.15f;

    protected Player targetPlayer;
    protected float maxHp;
    [SerializeField] private Image hpBar;
    protected float enterDamege;
    protected float stayDamege;
    protected EnemySpawner spawner;

    // SyncVar HP: Server ghi, tất cả clients đọc
    [SerializeField] private SyncVar<float> currentHp = new SyncVar<float>(100f, ownerAuth: false);

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

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        currentHp.onChanged += OnHpChanged;

        if (asServer)
        {
            currentHp.value = maxHp;
        }

        UpdateHpBar();
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentHp.onChanged -= OnHpChanged;
    }

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

    protected virtual void Awake()
    {
        ApplyDataSO();
        spriteRenderer = GetComponent<SpriteRenderer>();
        seeker = GetComponent<Seeker>();
        if (seeker == null)
            seeker = gameObject.AddComponent<Seeker>();
    }

    protected virtual void OnEnable()
    {
        ApplyDataSO();
        statsInitialized = false;

        // Tìm player gần nhất
        targetPlayer = FindNearestPlayer();
        if (targetPlayer != null)
        {
            movementPlaneZ = targetPlayer.transform.position.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, movementPlaneZ);
        }

        ResetPathState();
        RequestPath();
    }

    protected virtual void OnDisable()
    {
        if (seeker != null && !seeker.IsDone())
            seeker.CancelCurrentPathRequest();
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
            baseMoveSpeed   = enemyMoveSpeed;
            baseMaxHp       = maxHp;
            baseEnterDamage = enterDamege;
            baseStayDamage  = stayDamege;
            statsInitialized = true;
        }

        enemyMoveSpeed = baseMoveSpeed   * multiplier;
        maxHp          = baseMaxHp       * multiplier;
        enterDamege    = baseEnterDamage * multiplier;
        stayDamege     = baseStayDamage  * multiplier;

        // Set HP qua currentHp (nếu đã spawned, chỉ server ghi)
        if (isSpawned)
        {
            if (isServer) currentHp.value = maxHp;
        }
        else
        {
            currentHp.value = maxHp;
        }

        UpdateHpBar();
    }

    protected virtual void Start()
    {
        targetPlayer = FindNearestPlayer();
        if (targetPlayer != null)
        {
            movementPlaneZ = targetPlayer.transform.position.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, movementPlaneZ);
        }

        if (!isSpawned)
        {
            // Offline: khởi tạo bình thường
            currentHp.value = maxHp;
        }

        UpdateHpBar();
    }

    protected virtual void Update()
    {
        // AI chỉ server chạy
        if (!isSpawned || isServer)
        {
            UpdatePathRequest();
            MoveToPlayer();
        }

        // ALL clients đều flip sprite
        FlipEnemy();
    }

    // ─────────────────── PATHFINDING ─────────────────────────

    protected void MoveToPlayer()
    {
        if (targetPlayer == null)
        {
            targetPlayer = FindNearestPlayer();
            return;
        }

        if (TryMoveAlongPath()) return;

        // Fallback: Nếu A* chưa có đường đi (đang tính toán), di chuyển thẳng về phía Player
        Vector2 nextPosition = Vector2.MoveTowards(
            transform.position, targetPlayer.transform.position, enemyMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, movementPlaneZ);
    }

    private void UpdatePathRequest()
    {
        if (AstarPath.active == null) return;

        // Cập nhật target player gần nhất theo thời gian
        if (targetPlayer == null || targetPlayer.IsDead)
            targetPlayer = FindNearestPlayer();

        if (targetPlayer == null) return;
        if (Time.time < nextPathRequestTime) return;

        RequestPath();
    }

    private void RequestPath()
    {
        if (AstarPath.active == null || seeker == null || targetPlayer == null) return;
        if (!seeker.IsDone()) return;

        nextPathRequestTime = Time.time + pathUpdateInterval;
        seeker.StartPath(transform.position, targetPlayer.transform.position, OnPathComplete);
    }

    private void OnPathComplete(Path path)
    {
        if (!isActiveAndEnabled || path == null || path.error ||
            path.vectorPath == null || path.vectorPath.Count == 0)
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
            return false;

        while (currentWaypoint < currentPath.vectorPath.Count - 1 &&
               Vector2.Distance(transform.position,
                   ToMovementPlanePoint(currentPath.vectorPath[currentWaypoint])) <= waypointReachDistance)
        {
            currentWaypoint++;
        }

        Vector3 targetPoint = ToMovementPlanePoint(currentPath.vectorPath[currentWaypoint]);
        if (float.IsNaN(targetPoint.x) || float.IsNaN(targetPoint.y) ||
            float.IsInfinity(targetPoint.x) || float.IsInfinity(targetPoint.y))
            return false;

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

    private Vector3 ToMovementPlanePoint(Vector3 pathPoint) =>
        new Vector3(pathPoint.x, pathPoint.y, movementPlaneZ);

    // ─────────────────── FIND NEAREST PLAYER ─────────────────

    /// <summary>
    /// Tìm Player còn sống gần nhất trong số tất cả players (tối đa 4).
    /// </summary>
    protected Player FindNearestPlayer()
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

    protected void FlipEnemy()
    {
        if (targetPlayer == null || targetPlayer.IsDead)
            targetPlayer = FindNearestPlayer();

        if (targetPlayer != null)
            spriteRenderer.flipX =
                targetPlayer.transform.position.x < transform.position.x;

        if (hpBar != null)
            hpBar.transform.rotation = Quaternion.identity;
    }

    // ─────────────────── DAMAGE & DEATH ──────────────────────

    public virtual void TakeDamage(float damage)
    {
        // Chỉ Server xử lý
        if (isSpawned && !isServer) return;

        float newHp = Mathf.Clamp(currentHp.value - damage, 0f, maxHp);
        currentHp.value = newHp;

        if (newHp <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        GameManager.AddScore();
        if (isServer || !isSpawned)
            SpawnLoot(transform.position);

        if (spawner != null)
        {
            spawner.OnEnemyDied();
        }
        
        if (isSpawned)
            Despawn();
        else
            Destroy(gameObject);
    }

    /// Sinh vật phẩm trên Server và đồng bộ qua mạng cho mọi người.
    /// Sinh vật phẩm trên Server và đồng bộ qua mạng cho mọi người.
    private void SpawnLoot(Vector3 position)
    {
        GameObject prefab = GetDropPrefab();
        if (prefab == null) return;

        if (isSpawned)
        {
            // Network mode: Spawn qua PurrNet
            GameObject dropItem = UnityProxy.InstantiateDirectly(prefab);
            dropItem.transform.position = position;

            if (dropItem.TryGetComponent<PurrNet.NetworkIdentity>(out var netId))
                netId.Spawn(prefab, networkManager);

            // Cấy bộ đếm giờ vào Item, truyền thời gian lấy từ JSON
            ItemDespawner despawner = dropItem.AddComponent<ItemDespawner>();
            despawner.StartDespawn(GetDropLifetime());
        }
        else
        {
            // Offline mode
            GameObject dropItem = Instantiate(prefab, position, Quaternion.identity);
            Destroy(dropItem, GetDropLifetime());
        }
    }


    private System.Collections.IEnumerator DespawnItemAfterDelay(GameObject item, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (item != null)
        {
            if (item.TryGetComponent<NetworkIdentity>(out var netId) && netId.isSpawned)
                netId.Despawn();
            else
                Destroy(item);
        }
    }

    // ─────────────────── HP BAR ──────────────────────────────

    private void OnHpChanged(float newHp)
    {
        UpdateHpBar();
    }

    protected void UpdateHpBar()
    {
        if (hpBar != null && maxHp > 0f)
            hpBar.fillAmount = currentHp.value / maxHp;
    }

    // ─────────────────── COLLISION ───────────────────────────

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ Server xử lý damage
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Player"))
        {
            Player p = collision.GetComponent<Player>();
            if (p != null) p.TakeDamage(enterDamege);
        }
    }

    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Player"))
        {
            Player p = collision.GetComponent<Player>();
            if (p != null) p.TakeDamage(stayDamege);
        }
    }
}
