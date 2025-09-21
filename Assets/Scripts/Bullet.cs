using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private float bulletSpeed = 3;
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        var zRotation = transform.rotation.eulerAngles.z;
        var bulletDir =  new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
        print(bulletDir.x);
        
        _rb.linearVelocity = bulletDir * bulletSpeed;
    }
    
}
