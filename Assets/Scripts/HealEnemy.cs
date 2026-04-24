using UnityEngine;

public class HealEnemy : Enemy
{
    [SerializeField] private GameObject healObject;
    //[SerializeField] private float healValue = 20f;
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
        if (healObject != null)
        {
            GameObject heal = Instantiate(healObject, transform.position, Quaternion.identity);
            Destroy(heal, 7f);
        }
        base.Die();

    }
}
