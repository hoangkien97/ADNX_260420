using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] private float enemyMoveSpeed = 2f;
    protected Player player;
    [SerializeField] protected float maxHp = 100;
    protected float currentHp;
    [SerializeField] private Image hpBar;
    [SerializeField] protected float enterDamege = 10f;
    [SerializeField] protected float stayDamege = 1f;
    protected EnemySpawner spawner;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(EnemySpawner spawner)
    {
        this.spawner = spawner;
    }

    protected virtual void Start()
    {
        player = FindAnyObjectByType<Player>();
        currentHp = maxHp;
        UpdateHpBar();
    }

    protected virtual void Update()
    {
        MoveToPlayer();
        FlipEnemy();

    }
    protected void MoveToPlayer()
    {

        if (player != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, enemyMoveSpeed * Time.deltaTime);
        }
    }
    protected void FlipEnemy()
    {
        if (player != null)
        {
            spriteRenderer.flipX = player.transform.position.x < transform.position.x;
        }
        if (hpBar != null)
        {
            hpBar.transform.rotation = Quaternion.identity;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateHpBar();
        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    protected void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHp / maxHp;
        }
    }

    private void DebugEnemyCount()
    {
        // Đếm enemy đang active
        Enemy[] activeEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        Dictionary<string, int> countByType = new Dictionary<string, int>();
        foreach (Enemy enemy in activeEnemies)
        {
            string type = enemy.GetType().Name;
            if (!countByType.ContainsKey(type))
                countByType[type] = 0;
            countByType[type]++;
        }

        string log = $"Tổng enemy: {activeEnemies.Length}\n";
        foreach (var kvp in countByType)
            log += $"  {kvp.Key}: {kvp.Value}\n";

        Debug.Log(log);
    }
}
