using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

public class MultiplayerGridManager : MonoBehaviour
{
    public static MultiplayerGridManager Instance { get; private set; }

    private Dictionary<Player, int> playerGraphMap = new Dictionary<Player, int>();
    private Dictionary<Player, ProceduralGridMover> playerMovers = new Dictionary<Player, ProceduralGridMover>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Chỉ chạy trên Server (Host) vì AI/Pathfinding chỉ tính toán trên server
        if (PurrNet.NetworkManager.main != null && !PurrNet.NetworkManager.main.isServer) return;

        // A* chưa khởi tạo xong thì chờ
        AstarPath astar = AstarPath.active;
        if (astar == null || astar.data.gridGraph == null) return;

        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player p in players)
        {
            if (p != null && p.isSpawned && !playerGraphMap.ContainsKey(p))
            {
                AssignGraphToPlayer(p);
            }
        }
        
        // Dọn dẹp player bị despawn/chết (hoặc thoát game)
        List<Player> keysToRemove = new List<Player>();
        foreach (var p in playerGraphMap.Keys)
        {
            if (p == null || !p.gameObject.activeInHierarchy || !p.isSpawned)
            {
                keysToRemove.Add(p);
            }
        }

        foreach (var p in keysToRemove)
        {
            ProceduralGridMover mover = playerMovers[p];
            if (mover != null)
            {
                mover.enabled = false; // Ngăn chặn Update() gọi target.position
                mover.target = null;
                Destroy(mover);
            }
            playerMovers.Remove(p);
            // Không nên xóa hẳn GridGraph vì có thể gây lỗi Crash A* (do luồng phụ đang chạy)
            // Chỉ cần gỡ ProceduralGridMover là đủ, Graph cũ sẽ bị bỏ hoang hoặc xài lại sau.
            playerGraphMap.Remove(p);
            Debug.Log($"[MultiplayerGridManager] Đã gỡ GridGraph của Player bị despawn/thoát.");
        }
    }

    private void AssignGraphToPlayer(Player p)
    {
        AstarPath astar = AstarPath.active;

        // Lấy lưới đầu tiên (tạo bởi InfinityMap) làm bản mẫu
        GridGraph templateGraph = astar.data.gridGraph;

        // Nếu là player đầu tiên (VD: Host) và templateGraph chưa có ai dùng, gán cho Player đó luôn.
        if (playerGraphMap.Count == 0)
        {
            playerGraphMap[p] = (int)templateGraph.graphIndex;
            CreateMoverForGraph(p, templateGraph);
            Debug.Log($"[MultiplayerGridManager] Gán GridGraph {templateGraph.graphIndex} (Template) cho Player {p.PlayerDisplayName}");
            return;
        }

        // Tạo lưới mới sao chép từ lưới gốc cho các player tiếp theo (Client)
        // Lưu ý: AddGraph chạy an toàn trên luồng chính của Unity.
        GridGraph newGraph = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
        if (newGraph != null)
        {
            newGraph.SetDimensions(templateGraph.width, templateGraph.depth, templateGraph.nodeSize);
            newGraph.center = p.transform.position;
            newGraph.is2D = templateGraph.is2D;
            newGraph.neighbours = templateGraph.neighbours;
            newGraph.cutCorners = templateGraph.cutCorners;
            newGraph.erodeIterations = templateGraph.erodeIterations;
            
            newGraph.collision.use2D = templateGraph.collision.use2D;
            newGraph.collision.heightCheck = templateGraph.collision.heightCheck;
            newGraph.collision.collisionCheck = templateGraph.collision.collisionCheck;
            newGraph.collision.type = templateGraph.collision.type;
            newGraph.collision.diameter = templateGraph.collision.diameter;
            newGraph.collision.mask = templateGraph.collision.mask;

            // Quét lưới mới
            astar.Scan(newGraph);

            playerGraphMap[p] = (int)newGraph.graphIndex;
            CreateMoverForGraph(p, newGraph);
            Debug.Log($"[MultiplayerGridManager] Tạo mới GridGraph {newGraph.graphIndex} cho Player {p.PlayerDisplayName}");
        }
    }

    private void CreateMoverForGraph(Player p, GridGraph graph)
    {
        ProceduralGridMover mover = AstarPath.active.gameObject.AddComponent<ProceduralGridMover>();
        mover.graph = graph;
        mover.target = p.transform;
        mover.updateDistance = 2f; // Cập nhật khi Player đi xa 2 nodes
        mover.enabled = true;
        
        playerMovers[p] = mover;
    }

    /// <summary>
    /// Trả về bitmask của GridGraph thuộc về Player tương ứng.
    /// Nếu không tìm thấy, trả về -1 (Tìm trên mọi lưới).
    /// </summary>
    public static int GetGraphMaskForPlayer(Player p)
    {
        if (Instance == null || p == null || !Instance.playerGraphMap.ContainsKey(p))
            return -1;
        
        int graphIndex = Instance.playerGraphMap[p];
        return 1 << graphIndex; 
    }
}
