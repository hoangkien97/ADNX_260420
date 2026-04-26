using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float timeDestroy = 1f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private GameObject bloodPrefab;

    public float BaseDamage => damage;

    void Start()
    {
        damage += GameManager.BonusDamage;
        Destroy(gameObject, timeDestroy);
    }


    void Update()
    {
        MoveBullet();
    }

    void MoveBullet()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                GameObject blood = Instantiate(bloodPrefab, transform.position, Quaternion.identity);
                Destroy(blood, 1f);
            }
            Destroy(gameObject);
        }
        if (collision.CompareTag("Rock"))
        {
            Destroy(gameObject);
        }

    }
}
