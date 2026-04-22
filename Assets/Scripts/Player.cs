using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    [SerializeField] private float maxHp = 100f;
    private float currentHp;
    [SerializeField] private Image hpBar;
    [SerializeField] private GameManager gameManager;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        gameManager = FindAnyObjectByType<GameManager>();
    }
    void Start()
    {
        currentHp = maxHp;
        UpdateHpBar();
    }


    void Update()
    {
        MovePlayer();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.TogglePause();
        }
    }
    void MovePlayer()
    {
        Vector2 playerInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        rb.linearVelocity = playerInput.normalized * speed;
        if (playerInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (playerInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        if (playerInput != Vector2.zero)
        {
            animator.SetBool("isRun", true);
        }
        else
        {
            animator.SetBool("isRun", false);
        }
    }
    public void TakeDamage(float damege)
    {
        currentHp -= damege;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateHpBar();
        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        SceneManager.LoadScene("GameOver");
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHp / maxHp;
        }
    }

    public void Heal(float healAmount)
    {
        currentHp += healAmount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateHpBar();
    }

}
