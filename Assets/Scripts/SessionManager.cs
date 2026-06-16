using UnityEngine;
using PurrNet;
using UnityEngine.SceneManagement;

/// <summary>
/// Trách nhiệm duy nhất: Điều phối luồng Game Session (Tạm dừng, Về Menu, Mở Shop mạng lưới).
/// </summary>
public class SessionManager : NetworkBehaviour
{
    private GameManager coordinator;

    [SerializeField] public SyncVar<bool> syncPaused = new SyncVar<bool>(false, ownerAuth: false);
    [SerializeField] public SyncVar<bool> syncShopOpen = new SyncVar<bool>(false, ownerAuth: false);
    
    public bool isPaused = false;
    public bool isShopOpen = false;

    private void Awake()
    {
        coordinator = GetComponent<GameManager>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        syncPaused.onChanged += ApplyPause;
        syncShopOpen.onChanged += ApplyShop;
        
        ApplyPause(syncPaused.value);
        ApplyShop(syncShopOpen.value);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        syncPaused.onChanged -= ApplyPause;
        syncShopOpen.onChanged -= ApplyShop;
    }

    public void TogglePause()
    {
        if (isSpawned && !isServer) return;
        if (!isPaused && Time.timeScale == 0f) return;
        bool newPaused = !isPaused;

        if (isSpawned)
        {
            syncPaused.value = newPaused;
            RpcSetPause(newPaused);
        }
        else ApplyPause(newPaused);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcSetPause(bool paused) => ApplyPause(paused);

    private void ApplyPause(bool paused)
    {
        isPaused = paused;
        if (coordinator != null && coordinator.UI != null)
            coordinator.UI.SetPausePanelActive(paused);
        RefreshGlobalTimeScale();
    }

    public void OpenShopForAll()
    {
        if (!isServer) return;
        syncShopOpen.value = true;
        RpcOpenShop();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcOpenShop() => ApplyShop(true);

    public void CloseShopForAll()
    {
        if (!isServer) return;
        syncShopOpen.value = false;
        RpcCloseShop();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcCloseShop() => ApplyShop(false);

    private void ApplyShop(bool open)
    {
        isShopOpen = open;
        if (coordinator != null && coordinator.UI != null)
            coordinator.UI.SetShopPanelActive(open);
        RefreshGlobalTimeScale();
    }

    private void RefreshGlobalTimeScale()
    {
        Time.timeScale = (isPaused || isShopOpen) ? 0f : 1f;
    }

    public void GoMainMenu()
    {
        if (isSpawned && isServer)
        {
            RpcForceQuitToMenu();
            StartCoroutine(HostQuitCoroutine());
        }
        else ExecuteQuitLocal();
    }

    private System.Collections.IEnumerator HostQuitCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        ExecuteQuitLocal();
    }

    [ObserversRpc(runLocally: false)]
    private void RpcForceQuitToMenu() => ExecuteQuitLocal();

    private void ExecuteQuitLocal()
    {
        if (coordinator != null) coordinator.ResetRunStateLocal();
        Time.timeScale = 1f;
        
        NetworkBootstrap bootstrap = NetworkBootstrap.Instance ?? FindAnyObjectByType<NetworkBootstrap>();
        if (bootstrap != null) bootstrap.DisconnectAndLoad("GameStart");
        else SceneManager.LoadScene("GameStart");
    }
}
