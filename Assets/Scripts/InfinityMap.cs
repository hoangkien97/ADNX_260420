using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class InfinityMap : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject[] tilePrefabs;

    [Header("Override Size (để 0 = tự động đo từ prefab)")]
    public float overrideWidth = 0f;
    public float overrideHeight = 0f;

    [Header("Mode")]
    public bool isTopDown3D = false;

    [Header("A* Pathfinding")]
    [SerializeField] private bool autoSetupPathfinding = true;
    [SerializeField] private float graphWorldSize = 80f;
    [SerializeField] private float graphNodeSize = 0.5f;
    [SerializeField] private float graphFollowDistanceInNodes = 2f;
    [SerializeField] private int graphPaddingInNodes = 0;
    [SerializeField] private int rockLayer = 6;
    [SerializeField] private LayerMask obstacleLayers = 1 << 6;

    // Kích thước thực đo được từ prefab
    private float tileW;
    private float tileH;

    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Dictionary<int, Queue<GameObject>> pools = new();
    private Vector2Int lastAnchor = new(int.MaxValue, int.MaxValue);

    private static readonly Vector2Int[] OFFSETS =
    {
        new(0,0), new(1,0), new(0,1), new(1,1)
    };

    // =======================================================
    void Start()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        { Debug.LogError("[InfiniteMap] Thiếu tilePrefabs!"); return; }
        if (player == null)
        { Debug.LogError("[InfiniteMap] Thiếu player reference!"); return; }

        MeasureTileSize();   // ← đo kích thước thực trước tiên

        for (int i = 0; i < tilePrefabs.Length; i++)
            pools[i] = new Queue<GameObject>();

        lastAnchor = WorldToAnchor(player.position);
        Refresh();

        if (autoSetupPathfinding)
        {
            StartCoroutine(SetupPathfindingAfterMapReady());
        }
    }

    void Update()
    {
        Vector2Int anchor = WorldToAnchor(player.position);
        if (anchor == lastAnchor) return;
        lastAnchor = anchor;
        Refresh();
    }

    // =======================================================
    // ĐO KÍCH THƯỚC THỰC CỦA PREFAB
    // Spawn tạm tile đầu tiên → đọc bounds → hủy ngay
    // =======================================================
    void MeasureTileSize()
    {
        // Nếu user đã nhập thủ công thì dùng luôn
        if (overrideWidth > 0 && overrideHeight > 0)
        {
            tileW = overrideWidth;
            tileH = overrideHeight;
            Debug.Log($"[InfiniteMap] Size thủ công: W={tileW} H={tileH}");
            return;
        }

        // Spawn tạm để đo
        GameObject temp = Instantiate(tilePrefabs[0]);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        Bounds bounds = GetBounds(temp);
        DestroyImmediate(temp);

        if (bounds.size == Vector3.zero)
        {
            tileW = overrideWidth > 0 ? overrideWidth : 10f;
            tileH = overrideHeight > 0 ? overrideHeight : 10f;
            Debug.LogWarning($"[InfiniteMap] Không đo được bounds! Dùng fallback W={tileW} H={tileH}");
            return;
        }

        tileW = isTopDown3D ? bounds.size.x : bounds.size.x;
        tileH = isTopDown3D ? bounds.size.z : bounds.size.y;

        // Nếu 1 chiều vẫn = 0, dùng chiều còn lại (tile vuông)
        if (tileW <= 0) tileW = tileH;
        if (tileH <= 0) tileH = tileW;

        Debug.Log($"[InfiniteMap] Size tự động: W={tileW} H={tileH} (bounds={bounds.size})");
    }

    // Tìm bounds từ Renderer2D / Renderer3D / Collider2D / Collider
    Bounds GetBounds(GameObject go)
    {
        // Ưu tiên Renderer (chính xác nhất)
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Fallback: Collider 3D
        var colliders = go.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds b = colliders[0].bounds;
            foreach (var c in colliders) b.Encapsulate(c.bounds);
            return b;
        }

        // Fallback: Collider 2D
        var col2Ds = go.GetComponentsInChildren<Collider2D>();
        if (col2Ds.Length > 0)
        {
            Bounds b = col2Ds[0].bounds;
            foreach (var c in col2Ds) b.Encapsulate(c.bounds);
            return b;
        }

        return new Bounds(Vector3.zero, Vector3.zero);
    }

    // =======================================================
    // REFRESH: sync 4 tile active với vị trí player
    // =======================================================
    void Refresh()
    {
        var needed = GetSurrounding4(lastAnchor);

        // Thu hồi tile không dùng
        var toRemove = new List<Vector2Int>();
        foreach (var kv in activeTiles)
            if (!needed.Contains(kv.Key))
            { ReturnToPool(kv.Key, kv.Value); toRemove.Add(kv.Key); }
        foreach (var c in toRemove) activeTiles.Remove(c);

        // Spawn tile mới
        foreach (var cell in needed)
            if (!activeTiles.ContainsKey(cell))
                SpawnTile(cell);
    }

    HashSet<Vector2Int> GetSurrounding4(Vector2Int anchor)
    {
        var set = new HashSet<Vector2Int>();
        foreach (var o in OFFSETS) set.Add(anchor + o);
        return set;
    }

    // =======================================================
    // SPAWN & POOL
    // =======================================================
    void SpawnTile(Vector2Int cell)
    {
        int idx = GetPrefabIndex(cell);
        var tile = GetFromPool(idx);
        tile.transform.position = CellToWorld(cell);
        tile.SetActive(true);
        NormalizeRockLayer(tile);
        UpdatePathfindingForRockBounds(tile);
        activeTiles[cell] = tile;

        //Debug.Log($"[Spawn] cell={cell} → world={CellToWorld(cell)} prefab={idx}");
    }

    GameObject GetFromPool(int idx)
    {
        EnsurePool(idx);
        return pools[idx].Count > 0 ? pools[idx].Dequeue() : Instantiate(tilePrefabs[idx]);
    }

    void ReturnToPool(Vector2Int cell, GameObject tile)
    {
        int idx = GetPrefabIndex(cell);
        EnsurePool(idx);
        bool hasBounds = TryGetRockBounds(tile, out Bounds rockBounds);
        tile.SetActive(false);
        if (hasBounds && autoSetupPathfinding && AstarPath.active != null)
        {
            Physics2D.SyncTransforms();
            AstarPath.active.UpdateGraphs(rockBounds);
        }
        pools[idx].Enqueue(tile);
    }

    void EnsurePool(int idx) { if (!pools.ContainsKey(idx)) pools[idx] = new(); }

    IEnumerator SetupPathfindingAfterMapReady()
    {
        // Chờ 1 frame để TilemapCollider2D/CompositeCollider2D cập nhật shape sau khi clone map.
        yield return null;
        Physics2D.SyncTransforms();
        SetupPathfinding();
    }

    void SetupPathfinding()
    {
        AstarPath astar = AstarPath.active;
        if (astar == null)
        {
            GameObject astarObject = new GameObject("A* Pathfinding");
            astar = astarObject.AddComponent<AstarPath>();
        }

        if (astar == null)
        {
            Debug.LogWarning("[InfiniteMap] Không thể khởi tạo AstarPath.");
            return;
        }

        GridGraph graph = astar.data.gridGraph;
        if (graph == null)
        {
            graph = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
        }

        if (graph == null)
        {
            Debug.LogWarning("[InfiniteMap] Không thể tạo GridGraph.");
            return;
        }

        float nodeSize = Mathf.Max(0.1f, graphNodeSize);
        int widthNodes;
        int depthNodes;
        Vector3 graphCenter;

        if (TryGetActiveMapBounds(out Bounds mapBounds))
        {
            float padding = Mathf.Max(0, graphPaddingInNodes) * nodeSize;
            mapBounds.Expand(new Vector3(padding * 2f, padding * 2f, 0f));
            widthNodes = Mathf.Max(1, Mathf.CeilToInt(mapBounds.size.x / nodeSize));
            depthNodes = Mathf.Max(1, Mathf.CeilToInt(mapBounds.size.y / nodeSize));
            graphCenter = mapBounds.center;
        }
        else
        {
            int graphSizeInNodes = Mathf.Max(20, Mathf.RoundToInt(graphWorldSize / nodeSize));
            widthNodes = graphSizeInNodes;
            depthNodes = graphSizeInNodes;
            graphCenter = player.position;
        }

        graphCenter.z = 0f;

        graph.SetDimensions(widthNodes, depthNodes, nodeSize);
        graph.center = graphCenter;
        graph.is2D = true;
        graph.neighbours = NumNeighbours.Eight;
        graph.cutCorners = false;
        graph.erodeIterations = 0;
        graph.collision.use2D = true;
        graph.collision.heightCheck = false;
        graph.collision.collisionCheck = true;
        graph.collision.type = ColliderType.Sphere;
        graph.collision.diameter = 1f;
        graph.collision.mask = ResolveObstacleLayerMask();

        Debug.Log($"[InfiniteMap] A* graph configured: size={graph.width}x{graph.depth}, nodeSize={graph.nodeSize}, center={graph.center}, mask={graph.collision.mask.value}");

        Physics2D.SyncTransforms();
        astar.Scan();

        ProceduralGridMover mover = astar.GetComponent<ProceduralGridMover>();
        if (mover == null)
        {
            mover = astar.gameObject.AddComponent<ProceduralGridMover>();
        }

        mover.graph = graph;
        mover.target = player;
        mover.updateDistance = Mathf.Max(1f, graphFollowDistanceInNodes);
        mover.enabled = mover.target != null;
        if (mover.enabled)
        {
            mover.UpdateGraph();
        }
    }

    LayerMask ResolveObstacleLayerMask()
    {
        if (obstacleLayers.value != 0)
        {
            return obstacleLayers;
        }

        int detectedRockLayer = FindRockLayerFromActiveTiles();
        if (detectedRockLayer < 0)
        {
            detectedRockLayer = FindRockLayerFromPrefabs();
        }
        int finalLayer = detectedRockLayer >= 0 ? detectedRockLayer : rockLayer;
        int clampedRockLayer = Mathf.Clamp(finalLayer, 0, 31);
        return 1 << clampedRockLayer;
    }

    int FindRockLayerFromActiveTiles()
    {
        foreach (var kv in activeTiles)
        {
            GameObject tile = kv.Value;
            if (tile == null)
            {
                continue;
            }

            Transform[] transforms = tile.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current = transforms[i];
                if (current.CompareTag("Rock"))
                {
                    return current.gameObject.layer;
                }
            }
        }

        return -1;
    }

    int FindRockLayerFromPrefabs()
    {
        if (tilePrefabs == null)
        {
            return -1;
        }

        for (int i = 0; i < tilePrefabs.Length; i++)
        {
            GameObject tilePrefab = tilePrefabs[i];
            if (tilePrefab == null)
            {
                continue;
            }

            Transform[] transforms = tilePrefab.GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                Transform current = transforms[j];
                if (current.CompareTag("Rock"))
                {
                    return current.gameObject.layer;
                }
            }
        }

        return -1;
    }

    void NormalizeRockLayer(GameObject tile)
    {
        if (tile == null)
        {
            return;
        }

        int clampedRockLayer = Mathf.Clamp(rockLayer, 0, 31);
        Transform[] transforms = tile.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current.CompareTag("Rock"))
            {
                current.gameObject.layer = clampedRockLayer;
                CompositeCollider2D compositeCollider = current.GetComponent<CompositeCollider2D>();
                if (compositeCollider != null && compositeCollider.geometryType != CompositeCollider2D.GeometryType.Polygons)
                {
                    compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
                }
            }
        }
    }

    void UpdatePathfindingForRockBounds(GameObject tile)
    {
        if (!autoSetupPathfinding || AstarPath.active == null || tile == null)
        {
            return;
        }

        if (TryGetRockBounds(tile, out Bounds bounds))
        {
            Physics2D.SyncTransforms();
            AstarPath.active.UpdateGraphs(bounds);
        }
    }

    bool TryGetRockBounds(GameObject tile, out Bounds bounds)
    {
        bounds = default;
        Collider2D[] colliders = tile.GetComponentsInChildren<Collider2D>(true);
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (!collider.enabled || !collider.CompareTag("Rock"))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    bool TryGetActiveMapBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        if (tileW > 0f && tileH > 0f)
        {
            float minX = 0f;
            float minY = 0f;
            float maxX = 0f;
            float maxY = 0f;

            foreach (var kv in activeTiles)
            {
                GameObject tile = kv.Value;
                if (tile == null || !tile.activeInHierarchy)
                {
                    continue;
                }

                Vector2Int cell = kv.Key;
                float tileMinX = cell.x * tileW;
                float tileMaxX = tileMinX + tileW;
                float tileMinY = cell.y * tileH;
                float tileMaxY = tileMinY + tileH;

                if (!hasBounds)
                {
                    minX = tileMinX;
                    minY = tileMinY;
                    maxX = tileMaxX;
                    maxY = tileMaxY;
                    hasBounds = true;
                }
                else
                {
                    minX = Mathf.Min(minX, tileMinX);
                    minY = Mathf.Min(minY, tileMinY);
                    maxX = Mathf.Max(maxX, tileMaxX);
                    maxY = Mathf.Max(maxY, tileMaxY);
                }
            }

            if (hasBounds)
            {
                Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
                Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
                bounds = new Bounds(center, size);
                return true;
            }
        }

        return TryGetActiveMapContentBounds(out bounds);
    }

    bool TryGetActiveMapContentBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (var kv in activeTiles)
        {
            GameObject tile = kv.Value;
            if (tile == null || !tile.activeInHierarchy)
            {
                continue;
            }

            Bounds tileBounds = GetBounds(tile);
            if (tileBounds.size == Vector3.zero)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = tileBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(tileBounds);
            }
        }

        return hasBounds;
    }

    // =======================================================
    // COORDINATES
    // =======================================================

    // Player luôn nằm giữa 4 tile nhờ offset -0.5 tile
    Vector2Int WorldToAnchor(Vector3 pos)
    {
        float px = pos.x;
        float py = isTopDown3D ? pos.z : pos.y;
        return new Vector2Int(
            Mathf.FloorToInt((px - tileW * 0.5f) / tileW),
            Mathf.FloorToInt((py - tileH * 0.5f) / tileH)
        );
    }

    // Tile đặt tại TÂM ô: (cell + 0.5) × tileSize
    // → tile[0,0] center=(W/2, H/2), spans [0,W]×[0,H]
    // → tile[1,0] center=(3W/2,H/2), spans [W,2W]×[0,H]  ← không chồng
    Vector3 CellToWorld(Vector2Int cell)
    {
        float wx = (cell.x + 0.5f) * tileW;
        float wy = (cell.y + 0.5f) * tileH;
        return isTopDown3D ? new Vector3(wx, 0f, wy) : new Vector3(wx, wy, 0f);
    }

    int GetPrefabIndex(Vector2Int cell)
    {
        int n = tilePrefabs.Length;
        int h = cell.x * 73856093 ^ cell.y * 19349663;
        return ((h % n) + n) % n;
    }

    // =======================================================
    // GIZMOS
    // =======================================================
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        float w = tileW > 0 ? tileW : (overrideWidth > 0 ? overrideWidth : 10f);
        float h = tileH > 0 ? tileH : (overrideHeight > 0 ? overrideHeight : 10f);

        Vector2Int anchor = Application.isPlaying
            ? lastAnchor
            : new Vector2Int(
                Mathf.FloorToInt((player.position.x - w * 0.5f) / w),
                Mathf.FloorToInt(((isTopDown3D ? player.position.z : player.position.y) - h * 0.5f) / h));

        foreach (var o in OFFSETS)
        {
            var cell = anchor + o;
            float cx = (cell.x + 0.5f) * w;
            float cy = (cell.y + 0.5f) * h;
            Vector3 center = isTopDown3D ? new Vector3(cx, 0, cy) : new Vector3(cx, cy, 0);
            Vector3 size = isTopDown3D ? new Vector3(w, 0.05f, h) : new Vector3(w, h, 0.05f);

            Gizmos.color = o == Vector2Int.zero
                ? new Color(1f, 1f, 0f, 0.5f)
                : new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(center, size);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(player.position, w * 0.05f);
    }
#endif
}
