using UnityEngine;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Đọc Input và Di chuyển Player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody2D rb;
    private PlayerStats stats;
    private PlayerHealth health;
    private GameManager gameManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;
        if (isSpawned && !isOwner) return;

        // Xử lý nút Pause (Esc)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
            
            if (health != null && health.IsDead && isSpawned && !isServer)
                gameManager?.TogglePauseLocal();
            else if (!isSpawned || isServer)
                gameManager?.TogglePause();
            else
                gameManager?.TogglePauseLocal();
        }

        if (Time.timeScale == 0f) return;

        Move();
    }

    private void Move()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        if (rb.bodyType == RigidbodyType2D.Static) return;

        float currentMoveSpeed = stats != null ? stats.MoveSpeed : 5f;
        rb.linearVelocity = input.normalized * currentMoveSpeed;

        // Báo cho Visuals cập nhật hình ảnh và animation
        PlayerVisuals visuals = GetComponent<PlayerVisuals>();
        if (visuals != null)
        {
            visuals.UpdateMovementAnimation(input);
        }
    }
}
