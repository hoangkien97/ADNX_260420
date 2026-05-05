using UnityEngine;

public class HealEnemy : Enemy
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (player != null)
                player.TakeDamage(enterDamege);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (player != null)
                player.TakeDamage(stayDamege);
        }
    }

    protected override void Die()
    {
        GameObject prefab = GetDropPrefab();
        if (prefab != null)
        {
            GameObject heal = Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(heal, GetDropLifetime());
        }
        base.Die();
    }
}
