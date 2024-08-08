using UnityEngine;
using static System.MathF;

// Weapon component for characters(players and ai)
public class WeaponBehaviour : MonoBehaviour
{
    [SerializeField]private GameObject bulletPrefab;
    [SerializeField]private bool isPlayer;
    
    public bool hasSniper { get; set; } = false;
    public bool hasShotgun { get; set; } = false;
    
    // Matrices for rotating a normal vector into 4 new directions.
    // Used for creating the shotgun projectile spread.

    private static float inner = 0.0625f;
    private static float outer = 0.125f;
    
    private Matrix4x4 l = new (
        new Vector4(Cos(outer * PI), Mathf.Sin(outer * PI), 0, 0),
        new Vector4(-Mathf.Sin(outer * PI), Cos(outer * PI), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
    );
    
    private Matrix4x4 r = new (
        new Vector4(Cos(-outer * PI), Mathf.Sin(-outer * PI), 0, 0),
        new Vector4(-Mathf.Sin(-outer * PI), Cos(-outer * PI), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
    );
    
    private Matrix4x4 ml = new (
        new Vector4(Cos(inner * PI), Mathf.Sin(inner * PI), 0, 0),
        new Vector4(-Mathf.Sin(inner * PI), Cos(inner * PI), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
    );
    
    private Matrix4x4 mr = new (
        new Vector4(Cos(-inner * PI), Mathf.Sin(-inner * PI), 0, 0),
        new Vector4(-Mathf.Sin(-inner * PI), Cos(-inner * PI), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
    );
    
    public void FireSniper(Vector3 direction)
    {
        float bulletLife = 1.0f;
        float bulletSpeed = 30.0f;
        
        InstantiateBullet(direction, bulletLife, bulletSpeed);
    }
    
    public void FireShotgun(Vector3 direction)
    {
        float bulletLife = 0.4f;
        float bulletSpeed = 15.0f;
        
        Vector4 v4Dir = new Vector4(direction.x, direction.y, direction.z, 1);
        Vector3 lDir = l * v4Dir;
        Vector3 rDir = r * v4Dir;
        Vector3 mLDir = ml * v4Dir;
        Vector3 mRDir = mr * v4Dir;
        
        InstantiateBullet(direction, bulletLife, bulletSpeed);
        InstantiateBullet(lDir, bulletLife, bulletSpeed);
        InstantiateBullet(rDir, bulletLife, bulletSpeed);
        InstantiateBullet(mLDir, bulletLife, bulletSpeed);
        InstantiateBullet(mRDir, bulletLife, bulletSpeed);
    }

    private void InstantiateBullet(Vector3 direction, float bulletLife, float bulletSpeed)
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.GetComponent<Bullet>().playerOwned = isPlayer;
        bullet.transform.position = transform.position + direction;
        bullet.GetComponent<Rigidbody2D>().velocity = direction * bulletSpeed;
        Destroy(bullet, bulletLife);
    }
}
