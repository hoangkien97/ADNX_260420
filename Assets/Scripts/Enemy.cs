using System;
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

    private float baseMoveSpeed;
    private float baseMaxHp;
    private float baseEnterDamage;
    private float baseStayDamage;
    private bool statsInitialized = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(EnemySpawner spawner)
    {
        this.spawner = spawner;
    }

    public void ApplyStatMultiplier(float multiplier)
    {
        if (!statsInitialized)
        {
            baseMoveSpeed = enemyMoveSpeed;
            baseMaxHp = maxHp;
            baseEnterDamage = enterDamege;
            baseStayDamage = stayDamege;
            statsInitialized = true;
        }

        enemyMoveSpeed = baseMoveSpeed * multiplier;
        maxHp = baseMaxHp * multiplier;
        enterDamege = baseEnterDamage * multiplier;
        stayDamege = baseStayDamage * multiplier;

        currentHp = maxHp;
        UpdateHpBar();
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
        GameManager.AddScore();

        if (spawner != null)
        {
            spawner.OnEnemyDied();
            spawner.ReturnEnemyToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHp / maxHp;
        }
    }

 
}
