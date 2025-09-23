using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Rigidbody2D _rb;
    private AudioSource _audio;
    [SerializeField] private float bulletSpeed = 3;
    public float bulletLifeTime = 2;
    
    [SerializeField] private AudioClip bulletBounceSfx;
    private Vector2 prevLinearVelocity;

    //private bool destroy;
    //private float destroyTimer;
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _audio = GetComponent<AudioSource>();
        
        var zRotation = transform.rotation.eulerAngles.z;
        var bulletDir =  new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
        //print(bulletDir.x);
        
        _rb.linearVelocity = bulletDir * bulletSpeed;

        if (!IsOwner) return;
        StartCoroutine(DestroyBullet());
    }

    private IEnumerator DestroyBullet()
    {
        yield return new WaitForSeconds(bulletLifeTime);
        
        if (this)
            DestroyBulletServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void DestroyBulletServerRpc()
    {
        //if (!IsOwner) return;
        
        //NetworkManager.Singleton.SpawnManager.SpawnedObjects[0].Despawn();
        GetComponent<NetworkObject>().Despawn();
    }

    
    private void Update()
    {
        if (prevLinearVelocity != _rb.linearVelocity)
        {
            _audio.PlayOneShot(bulletBounceSfx);
        }
        
        prevLinearVelocity = _rb.linearVelocity;
    }
}
