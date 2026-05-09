using System.Collections;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton wrapper quản lý PurrNet NetworkManager.
/// Tồn tại DontDestroyOnLoad qua tất cả các scene.
/// Gọi StartHostAndLoad(scene) hoặc StartAsClient(ip) từ GameStartManager.
/// </summary>
public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap Instance { get; private set; }

    [SerializeField] private NetworkManager networkManager;

    public bool IsConnected => networkManager != null && (networkManager.isServer || networkManager.isClient);
    public bool IsServerRunning => networkManager != null && networkManager.isServer;
    public bool IsClientRunning => networkManager != null && networkManager.isClient;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (networkManager == null)
            networkManager = GetComponentInChildren<NetworkManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────── HOST ────────────────────────────────

    /// <summary>
    /// StartHost rồi chờ server sẵn sàng, sau đó load scene qua PurrNet.
    /// </summary>
    private bool isStarting = false;

    public void StartHostAndLoad(string sceneName)
    {
        if (networkManager == null)
        {
            Debug.LogError("[NetworkBootstrap] NetworkManager chưa được gán!");
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (isStarting)
        {
            Debug.LogWarning("[NetworkBootstrap] Đang trong quá trình khởi động mạng, vui lòng đợi...");
            return;
        }

        if (networkManager.isServer)
        {
            Debug.LogWarning("[NetworkBootstrap] Server đã chạy rồi. Load thẳng scene.");
            LoadSceneViaServer(sceneName);
            return;
        }

        isStarting = true;
        StartCoroutine(StartHostThenLoad(sceneName));
    }

    private IEnumerator StartHostThenLoad(string sceneName)
    {
        // Chờ socket thực sự được giải phóng (release) nếu vừa gọi Stop
        yield return new WaitWhile(() => networkManager.isServer || networkManager.isClient);

        // Đọc port thực tế từ Transport (Hỗ trợ cả UDP và Web/TCP)
        ushort serverPort = 5000;
        bool isTcp = false;
        
        if (networkManager.transport is UDPTransport udpForPort)
            serverPort = udpForPort.serverPort;
        else if (networkManager.transport is WebTransport webForPort)
        {
            serverPort = webForPort.serverPort;
            isTcp = true;
        }
        
        // Kiểm tra xem port có đang bị chiếm không để tránh văng log lỗi đỏ (Bind exception)
        bool portInUse = !IsPortAvailable(serverPort, isTcp);

        bool hostStarted = false;
        if (!portInUse)
        {
            try
            {
                Debug.Log($"[NetworkBootstrap] Port {serverPort} trống, đang khởi động HOST...");
                networkManager.StartHost();
                hostStarted = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NetworkBootstrap] Không thể khởi tạo Host. Chuyển sang Client. Chi tiết lỗi: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkBootstrap] Port {serverPort} đã bị chiếm. Tự động chuyển sang chế độ CLIENT.");
        }

        if (!hostStarted)
        {
            // Port bị chiếm -> Thử kết nối làm Client
            isStarting = false;
            StartAsClient("127.0.0.1");
            yield break;
        }

        // Chờ đến khi server thực sự sẵn sàng
        float timeout = 5f;
        float elapsed = 0f;
        while (!networkManager.isServer && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!networkManager.isServer)
        {
            Debug.LogError("[NetworkBootstrap] Server không khởi động được sau 5s. Thử làm Client...");
            isStarting = false;
            StartAsClient("127.0.0.1");
            yield break;
        }

        Debug.Log("[NetworkBootstrap] Server ready! Loading scene via PurrNet...");
        LoadSceneViaServer(sceneName);
        isStarting = false;
    }

    private void LoadSceneViaServer(string sceneName)
    {
        var serverSceneModule = networkManager.GetModule<PurrNet.Modules.ScenesModule>(true);
        if (serverSceneModule != null)
        {
            serverSceneModule.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            Debug.Log($"[NetworkBootstrap] PurrNet loading scene: {sceneName}");
        }
        else
        {
            Debug.LogWarning("[NetworkBootstrap] Không tìm thấy ScenesModule. Dùng SceneManager.");
            SceneManager.LoadScene(sceneName);
        }
    }

    // ─────────────────── CLIENT ──────────────────────────────

    /// <summary>
    /// StartClient kết nối vào IP của Host.
    /// </summary>
    public void StartAsClient(string hostIp)
    {
        if (networkManager == null)
        {
            Debug.LogError("[NetworkBootstrap] NetworkManager chưa được gán!");
            return;
        }

        // Thay vì check isClient, ta check clientState để bỏ qua nếu đang Connecting
        if (networkManager.clientState != ConnectionState.Disconnected)
        {
            Debug.LogWarning($"[NetworkBootstrap] Client đang ở trạng thái {networkManager.clientState}, bỏ qua lệnh StartClient.");
            return;
        }

        if (string.IsNullOrWhiteSpace(hostIp))
            hostIp = "127.0.0.1";

        if (networkManager.transport is UDPTransport udp)
        {
            udp.address = hostIp;
            Debug.Log($"[NetworkBootstrap] Connecting to HOST at {hostIp}...");
        }

        networkManager.StartClient();
    }

    // Giữ lại cho compat (offline host không cần load scene riêng)
    public void StartAsHost()
    {
        StartHostAndLoad("SampleScene");
    }

    // ─────────────────── DISCONNECT ──────────────────────────

    public void Disconnect()
    {
        if (networkManager == null) return;
        if (networkManager.isServer) networkManager.StopServer();
        if (networkManager.isClient) networkManager.StopClient();
        Debug.Log("[NetworkBootstrap] Disconnected.");
    }

    /// <summary>
    /// Disconnect sạch rồi khởi động lại host + load scene.
    /// Chạy trên NetworkBootstrap (DontDestroyOnLoad) nên coroutine không bị mất khi scene thay đổi.
    /// Dùng thay cho Disconnect() + StartHostAndLoad() gọi liền nhau (tránh NullRef trong PurrNet).
    /// </summary>
    public void RestartAsHost(string sceneName)
    {
        StartCoroutine(RestartHostCoroutine(sceneName));
    }

    private IEnumerator RestartHostCoroutine(string sceneName)
    {
        Debug.Log("[NetworkBootstrap] Đang bắt đầu quá trình Restart Game...");
        
        // 1. Dừng mọi kết nối hiện tại
        Disconnect();

        // 2. Chờ PurrNet đánh dấu isServer/isClient về false
        float waitStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - waitStart < 3f && (networkManager.isServer || networkManager.isClient))
        {
            yield return null;
        }

        // 3. Chờ UDP/TCP port thực sự được OS giải phóng (tối đa 2s)
        ushort serverPort = 5000;
        bool isTcp = false;
        
        if (networkManager?.transport is UDPTransport udpTransport)
            serverPort = udpTransport.serverPort;
        else if (networkManager?.transport is WebTransport webTransport)
        {
            serverPort = webTransport.serverPort;
            isTcp = true;
        }

        float portWaitStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - portWaitStart < 2f && !IsPortAvailable(serverPort, isTcp))
        {
            yield return null;
        }

        // 4. Reset flag và khởi động lại
        Debug.Log("[NetworkBootstrap] Đã dọn dẹp xong. Đang khởi động Host mới...");
        isStarting = false;
        StartHostAndLoad(sceneName);
    }

    // ─────────────────── EVENTS ──────────────────────────────

    public void SubscribeToConnectionEvents(
        System.Action<ConnectionState> onServerState,
        System.Action<ConnectionState> onClientState)
    {
        if (networkManager == null) return;
        if (onServerState != null) networkManager.onServerConnectionState += onServerState;
        if (onClientState != null) networkManager.onClientConnectionState += onClientState;
    }

    public void UnsubscribeFromConnectionEvents(
        System.Action<ConnectionState> onServerState,
        System.Action<ConnectionState> onClientState)
    {
        if (networkManager == null) return;
        if (onServerState != null) networkManager.onServerConnectionState -= onServerState;
        if (onClientState != null) networkManager.onClientConnectionState -= onClientState;
    }

    // Kiểm tra xem port UDP/TCP có đang được sử dụng không
    private bool IsPortAvailable(int port, bool isTcp = false)
    {
        bool isAvailable = true;
        
        if (isTcp)
        {
            System.Net.Sockets.TcpListener tcpListener = null;
            try
            {
                tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                tcpListener.Start();
            }
            catch (System.Net.Sockets.SocketException)
            {
                isAvailable = false;
            }
            finally
            {
                tcpListener?.Stop();
            }
        }
        else
        {
            System.Net.Sockets.UdpClient udpClient = null;
            try
            {
                udpClient = new System.Net.Sockets.UdpClient(port);
            }
            catch (System.Net.Sockets.SocketException)
            {
                isAvailable = false;
            }
            finally
            {
                udpClient?.Close();
            }
        }
        return isAvailable;
    }
}