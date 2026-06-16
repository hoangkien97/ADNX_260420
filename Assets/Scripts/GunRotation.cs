using UnityEngine;
using PurrNet;

/// <summary>
/// Đảm nhiệm 1 việc duy nhất: Lấy Input chuột và xoay súng/lật súng.
/// </summary>
public class GunRotation : NetworkBehaviour
{
    private float rotateOffset = 180f;

    private void Update()
    {
        if (Time.timeScale == 0) return;
        // Chỉ cho phép Owner xoay súng
        Player parentPlayer = GetComponentInParent<Player>();
        if (parentPlayer != null && parentPlayer.isSpawned && !parentPlayer.isOwner) return;
        if (parentPlayer == null && isSpawned && !isOwner) return;

        RotateGun();
    }

    private void RotateGun()
    {
        if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0 ||
            Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
            return;

        Vector3 displacement = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + rotateOffset);
        transform.localScale = (angle < -90 || angle > 90)
            ? new Vector3(1, 1, 1)
            : new Vector3(1, -1, 1);
    }
}
