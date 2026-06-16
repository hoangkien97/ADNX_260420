using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presenter cho Shop: Điều phối dữ liệu từ Player và ShopItemSO đẩy qua cho ShopView vẽ.
/// Bắt sự kiện bấm nút từ View và gửi lệnh gọi mua hàng tới Player.
/// </summary>
[RequireComponent(typeof(ShopView))]
public class ShopPresenter : MonoBehaviour
{
    private ShopView view;
    
    [Header("Data Source")]
    public ShopItemSO[] shopItemsSO;
    public GameObject[] shopPanelsGO;

    private Player player;
    private Gun gun;
    private static GameConfigSO GameConfig => EnemyDataManager.Instance?.gameConfig;
    private int currentCoin;

    private void Awake()
    {
        view = GetComponent<ShopView>();
        
        if (view.purchaseBtns == null || view.purchaseBtns.Length == 0)
        {
            Debug.LogWarning("[ShopPresenter] Chưa gán mảng purchaseBtns trong ShopView!");
            return;
        }

        // Tự động gán sự kiện OnClick cho các nút mua trong mảng
        for (int i = 0; i < view.purchaseBtns.Length; i++)
        {
            int index = i;
            if (view.purchaseBtns[i] != null)
            {
                // Xóa các event cũ trên Inspector để tránh dính logic cũ
                view.purchaseBtns[i].onClick.RemoveAllListeners();
                view.purchaseBtns[i].onClick.AddListener(() => PurchaseItem(index));
            }
            else
            {
                Debug.LogWarning($"[ShopPresenter] Nút thứ {i} trong mảng purchaseBtns bị null!");
            }
        }
    }

    private void OnEnable()
    {
        transform.SetAsLastSibling();
        
        if (shopPanelsGO != null)
        {
            foreach (var go in shopPanelsGO) if (go != null) go.SetActive(true);
        }

        currentCoin = GameManager.CountCoin;
        view.SetCoin(currentCoin);

        LoadDataToView();
        CheckPurchaseable();
    }

    private void Update()
    {
        if (player == null) FindLocalPlayer();

        if (player != null)
        {
            currentCoin = player.MyCoins;
            view.SetCoin(currentCoin);
            CheckPurchaseable();
            RefreshStatsView();
        }
    }

    private void FindLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (!p.isSpawned || p.isOwner) 
            {
                player = p;
                break;
            }
        }
    }

    private void LoadDataToView()
    {
        if (shopItemsSO == null) return;
        GameConfigSO cfg = GameConfig;
        if (cfg != null)
        {
            foreach (var so in shopItemsSO)
            {
                if (so == null) continue;
                switch (so.itemType)
                {
                    case ShopItemType.UpgradeSpeed:
                        so.baseCost = cfg.speedBaseCost;
                        so.effectValue = cfg.speedEffectValue;
                        break;
                    case ShopItemType.UpgradeDamage:
                        so.baseCost = cfg.damageBaseCost;
                        so.effectValue = cfg.damageEffectValue;
                        break;
                    case ShopItemType.UpgradeMaxHP:
                        so.baseCost = cfg.hpBaseCost;
                        so.effectValue = cfg.hpEffectValue;
                        break;
                }
            }
        }

        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            if (shopItemsSO[i] != null)
                view.SetItemInfo(i, shopItemsSO[i].title, shopItemsSO[i].description, shopItemsSO[i].baseCost);
        }
    }

    private void CheckPurchaseable()
    {
        if (shopItemsSO == null) return;
        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            if (shopItemsSO[i] != null)
                view.SetPurchaseInteractable(i, currentCoin >= shopItemsSO[i].baseCost);
        }
    }

    public void PurchaseItem(int index)
    {
        Debug.Log($"[ShopPresenter] Đã click nút mua thứ {index}");

        if (player == null)
        {
            Debug.LogError("[ShopPresenter] Lỗi: Không tìm thấy Player cục bộ!");
            return;
        }
        if (shopItemsSO == null || index >= shopItemsSO.Length)
        {
            Debug.LogError($"[ShopPresenter] Lỗi: Mảng shopItemsSO không hợp lệ hoặc OutOfBounds. Index: {index}");
            return;
        }

        ShopItemSO item = shopItemsSO[index];
        if (item == null)
        {
            Debug.LogError($"[ShopPresenter] Lỗi: item ở vị trí {index} bị NULL!");
            return;
        }

        Debug.Log($"[ShopPresenter] Kiểm tra giá tiền: Hiện có {currentCoin}, Yêu cầu {item.baseCost}");

        if (currentCoin >= item.baseCost)
        {
            Debug.Log("[ShopPresenter] Đủ tiền mua. Bắt đầu xử lý mua...");
            if (player.isSpawned)
            {
                Debug.Log("[ShopPresenter] Đang chơi Online. Gọi ServerRpc...");
                player.CmdBuyShopItem((int)item.itemType, item.baseCost, item.effectValue);
            }
            else
            {
                Debug.Log("[ShopPresenter] Đang chơi Offline. Trừ tiền trực tiếp...");
                GameManager.CountCoin -= item.baseCost;
                ApplyEffectOffline(item);
            }
        }
        else
        {
            Debug.LogWarning("[ShopPresenter] Không đủ tiền!");
        }
    }

    private void ApplyEffectOffline(ShopItemSO item)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || player == null) return;

        switch (item.itemType)
        {
            case ShopItemType.UpgradeSpeed: gm.UpgradeSpeed(item.effectValue); break;
            case ShopItemType.UpgradeDamage: gm.UpgradeDamage(item.effectValue); break;
            case ShopItemType.UpgradeMaxHP: gm.UpgradeMaxHP(item.effectValue); break;
        }
    }

    private void RefreshStatsView()
    {
        if (player == null) return;
        gun = player.GetComponentInChildren<Gun>();

        string hp = player.MaxHp.ToString("MaxHP : 0.##");
        string speed = player.MoveSpeed.ToString("Speed : 0.##");
        string damage = gun != null ? gun.CurrentDamage.ToString("Damage : 0.##") : "N/A";

        view.UpdateStats(hp, speed, damage);
    }
}
