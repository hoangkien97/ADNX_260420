using UnityEngine;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Va chạm vật lý và gây sát thương cho người chơi.
/// </summary>
public class EnemyCombat : NetworkBehaviour
{
    private Enemy coordinator;

    private void Awake()
    {
        coordinator = GetComponent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Gây sát thương là việc của Server
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Player"))
        {
            Player p = collision.GetComponent<Player>();
            float dmg = coordinator != null ? coordinator.EnterDamage : 10f;
            if (p != null) p.TakeDamage(dmg);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Player"))
        {
            Player p = collision.GetComponent<Player>();
            float dmg = coordinator != null ? coordinator.StayDamage : 1f;
            if (p != null) p.TakeDamage(dmg);
        }
    }
}
