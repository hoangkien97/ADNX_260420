using UnityEngine;
using UnityEngine.UI;
using PurrNet;

/// <summary>
/// Xử lý va chạm nhặt item của Player.
/// - Chỉ Owner mới nhặt (coin/heal riêng từng người)
/// - Coin và Heal update local trước (responsive), Server xác nhận
/// </summary>
public class PlayerCollition : NetworkBehaviour
{
    [SerializeField] private Text txtCoin;
    [SerializeField] private AudioManager audioManager;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ owner của player này mới nhặt item
        if (isSpawned && !isOwner) return;

        if (collision.CompareTag("Coin") || collision.CompareTag("Heal"))
        {
            bool isHeal = collision.CompareTag("Heal");
            float healValue = 0f;
            if (isHeal)
            {
                HealItem healItem = collision.GetComponent<HealItem>();
                if (healItem != null) healValue = healItem.healValue;
            }

            if (isSpawned)
            {
                // Gọi Server để Server xử lý despawn chung cho tất cả
                if (collision.TryGetComponent<PurrNet.NetworkIdentity>(out var netId))
                {
                    CmdPickupItem(netId, isHeal, healValue);
                    
                    // Ẩn tạm thời ở Client nội bộ để có cảm giác nhặt được ngay lập tức (Client-Side Prediction)
                    collision.gameObject.SetActive(false);
                }
            }
            else
            {
                // Offline Mode
                ProcessPickupLocal(isHeal, healValue);
                Destroy(collision.gameObject);
            }
        }
    }

    private void ProcessPickupLocal(bool isHeal, float healValue)
    {
        if (isHeal)
        {
            Player player = GetComponent<Player>();
            if (player != null) player.Heal(healValue);
            audioManager?.PlayItemSound();
        }
        else
        {
            GameManager.UpdateCoin();
            if (txtCoin != null)
                txtCoin.text = GameManager.CountCoin.ToString();
            audioManager?.PlayCoinSound();
        }
    }

    /// <summary>
    /// Gửi yêu cầu nhặt đồ lên Server. Server sẽ là người phán xử ai nhặt được (để tránh 2 người nhặt cùng lúc).
    /// </summary>
    [ServerRpc(requireOwnership: true)]
    private void CmdPickupItem(PurrNet.NetworkIdentity itemNetId, bool isHeal, float healValue)
    {
        // 1. Server kiểm tra xem item này còn tồn tại không (chưa bị người khác nhặt mất)
        if (itemNetId == null || !itemNetId.isSpawned) return;

        // 2. Nếu hợp lệ, Server gọi Despawn để xóa item khỏi tất cả các máy
        itemNetId.Despawn();

        // 3. Gửi kết quả TRỰC TIẾP đến đúng player đã nhặt (TargetRpc thay vì broadcast)
        if (owner.HasValue)
            RpcGrantPickup(owner.Value, isHeal, healValue);
    }

    /// <summary>
    /// TargetRpc: Server gửi trực tiếp đến 1 client duy nhất (owner của nhân vật này).
    /// Tiết kiệm băng thông hơn ObserversRpc vì không broadcast cho tất cả.
    /// </summary>
    [TargetRpc]
    private void RpcGrantPickup(PlayerID target, bool isHeal, float healValue)
    {
        // Không cần if (!isOwner) vì TargetRpc đã gửi đúng người rồi
        ProcessPickupLocal(isHeal, healValue);
    }
}
