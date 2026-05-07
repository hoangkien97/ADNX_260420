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

        if (collision.CompareTag("Coin"))
        {
            GameManager.UpdateCoin();
            if (txtCoin != null)
                txtCoin.text = GameManager.CountCoin.ToString();
            Destroy(collision.gameObject);
            audioManager?.PlayCoinSound();
        }

        if (collision.CompareTag("Heal"))
        {
            Player player = GetComponent<Player>();
            HealItem healItem = collision.GetComponent<HealItem>();
            if (player != null && healItem != null)
            {
                // Gửi lên server để server xử lý Heal (vì TakeDamage/Heal là server-side)
                if (isSpawned)
                    CmdHeal(healItem.healValue);
                else
                    player.Heal(healItem.healValue);
            }
            Destroy(collision.gameObject);
            audioManager?.PlayItemSound();
        }
    }

    /// <summary>
    /// Owner gửi lên Server để server cộng HP.
    /// </summary>
    [ServerRpc(requireOwnership: true)]
    private void CmdHeal(float amount)
    {
        Player player = GetComponent<Player>();
        if (player != null)
            player.Heal(amount);
    }
}
