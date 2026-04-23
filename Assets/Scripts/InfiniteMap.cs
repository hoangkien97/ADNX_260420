using System.Collections.Generic;
using UnityEngine;

public class InfiniteMap : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject[] tilePrefabs;

    [Header("Override Size (để 0 = tự động đo từ prefab)")]
    public float overrideWidth = 0f;
    public float overrideHeight = 0f;

    [Header("Mode")]
    public bool isTopDown3D = false;

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

        MeasureTileSize();   // ← đo kích thước thực trước tiên

        for (int i = 0; i < tilePrefabs.Length; i++)
            pools[i] = new Queue<GameObject>();

        lastAnchor = WorldToAnchor(player.position);
        Refresh();
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
        tile.SetActive(false);
        pools[idx].Enqueue(tile);
    }

    void EnsurePool(int idx) { if (!pools.ContainsKey(idx)) pools[idx] = new(); }

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