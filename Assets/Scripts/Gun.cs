using Unity.VisualScripting;
using UnityEngine;

public class Gun : MonoBehaviour
{
    private float rotateOffset = 180f;
    [SerializeField] private Transform firePos;
    [SerializeField] private GameObject bulletPrefabs;
    [SerializeField] private float shotDelay = 0.5f;
    private float nextshot;
    [SerializeField] private int maxAmmo = 10;
    public int currentAmmo;
    void Start()
    {
        currentAmmo = maxAmmo;
    }

 
    void Update()
    {
        RotateGun();
        Shoot();
        ReLoad();
    }

    void RotateGun ()
    {
        if(Input.mousePosition.x <0 || Input.mousePosition.y < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
        {
            return;
        }
          
        Vector3 displacement = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;

        //Debug.Log($"[RotateGun] displacement: {displacement} | angle: {angle:F2}° | mousePos: {Input.mousePosition}");
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + rotateOffset));
        if ( angle < -90 || angle > 90 )
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, -1, 1);
        }
    }

    void Shoot()
    {
      if (Input.GetMouseButtonDown(0) && Time.time >= nextshot && currentAmmo > 0) {
            nextshot = Time.time + shotDelay;
            Instantiate(bulletPrefabs, firePos.position, firePos.rotation);
            currentAmmo--;
      }
    }

    void ReLoad()
    {
        if (Input.GetKeyDown(KeyCode.R))
            {
                currentAmmo = maxAmmo;
        }
    }
}
