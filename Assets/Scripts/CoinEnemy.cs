using System;
using UnityEngine;

public class CoinEnemy : Enemy
{
    [SerializeField] private GameObject coinObject;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (player != null)
            {
                player.TakeDamage(enterDamege);
            }
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (player != null)
            {
                player.TakeDamage(stayDamege);
            }
        }
    }

    protected override void Die()
    {
        if (coinObject != null)
        {
            GameObject coin = Instantiate(coinObject, transform.position, Quaternion.identity);
            Destroy(coin, 7f);
        }
        base.Die();

    }

}
