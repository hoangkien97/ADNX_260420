using UnityEngine;

/// <summary>
/// Trọng tài (Coordinator) cho hệ thống Shop theo chuẩn MVP.
/// Đã tách logic UI (ShopView) và logic Data (ShopPresenter).
/// Script này đóng vai trò giữ hàm ContinueGame() để Inspector gọi tới không bị lỗi.
/// </summary>
[RequireComponent(typeof(ShopView))]
[RequireComponent(typeof(ShopPresenter))]
public class ShopManager : MonoBehaviour
{
    public void ContinueGame()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.isServer) return;
            GameManager.Instance.CloseShopForAll();
        }
        else
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }
    }
}
