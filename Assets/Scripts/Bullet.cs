using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private float bulletSpeed = 3;
    public float bulletLifeTime = 2;

    //private bool destroy;
    //private float destroyTimer;
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        var zRotation = transform.rotation.eulerAngles.z;
        var bulletDir =  new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
        //print(bulletDir.x);
        
        _rb.linearVelocity = bulletDir * bulletSpeed;

        //StartCoroutine(DestroyBullet());
    }

    public IEnumerator DestroyBullet()
    {
        yield return new WaitForSeconds(bulletLifeTime);
        
        if (this)
            Destroy(gameObject);
    }
    
    [ServerRpc]
    public void DestroyBulletServerRpc(float lifeTime)
    {
        
    }

    private void Update()
    {
        
        //destroyTimer
        
        if (this)
            Destroy(gameObject);

    }
    
}
