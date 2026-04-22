using UnityEngine;
using UnityEngine.UI;

public class PlayerCollition : MonoBehaviour
{
    [SerializeField] private Text txtCoin;
    [SerializeField] private AudioManager audioManager;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            GameManager.UpdateCoin();
            txtCoin.text = GameManager.CountCoin.ToString();
            Destroy(collision.gameObject);
            audioManager.PlayCoinSound();
        }
        if (collision.CompareTag("Heal"))
        {
            Player player = GetComponent<Player>();
            HealItem healItem = collision.GetComponent<HealItem>();
            if (healItem != null)
            {
                player.Heal(healItem.healValue);
            }
            Destroy(collision.gameObject);
            audioManager.PlayItemSound();
        }
    }
}
