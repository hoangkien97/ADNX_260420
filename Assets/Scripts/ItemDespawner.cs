using UnityEngine;


public class ItemDespawner : MonoBehaviour
{
    public void StartDespawn(float delay)
    {
        Invoke(nameof(DoDespawn), delay);
    }

    private void DoDespawn()
    {
        if (TryGetComponent<PurrNet.NetworkIdentity>(out var netId) && netId.isSpawned)
            netId.Despawn();
        else
            Destroy(gameObject);
    }
}
