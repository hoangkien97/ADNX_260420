using UnityEngine;
using UnityEngine.UI;

public class PlayerCollition : MonoBehaviour
{
    [SerializeField] private Text txtCoin;


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            GameManager.UpdateCoin();
            txtCoin.text = GameManager.CountCoin.ToString();
            Destroy(collision.gameObject);
        }
        if (collision.CompareTag("Heal"))
        {
            Player player = GetComponent<Player>();
            player.Heal(20f);
            Destroy(collision.gameObject);
        }
    }
}
