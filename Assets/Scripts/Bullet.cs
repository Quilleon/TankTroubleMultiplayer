using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public PlayerController OwnerController;
    
    private Rigidbody2D _rb;
    private AudioSource _audio;
    private SpriteRenderer _sprite;
    [SerializeField] private float bulletSpeed = 3;
    public float bulletLifeTime = 2;
    
    [SerializeField] private AudioClip bulletBounceSfx;
    private Vector2 prevLinearVelocity;

    public NetworkVariable<Color32> bulletColor = new (Color.white);
    
    public override void OnNetworkSpawn()
    {
        bulletColor.OnValueChanged += (value, newValue) => { _sprite = GetComponentInChildren<SpriteRenderer>(); _sprite.color = newValue; };
    }

    //private AnticipatedNetworkTransform anticipatedTransform;

    //private bool destroy;
    //private float destroyTimer;
    
    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _audio = GetComponent<AudioSource>();
        //_sprite = GetComponentInChildren<SpriteRenderer>();
        
        var zRotation = transform.rotation.eulerAngles.z;
        var bulletDir =  new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
        //print(bulletDir.x);
        
        // Move in shoot direction
        _rb.linearVelocity = bulletDir * bulletSpeed;
        
        // Reset visual rotation
        transform.GetChild(0).rotation = Quaternion.identity;
        
        if (!IsOwner) return;
        StartCoroutine(DestroyBullet());
    }

    private IEnumerator DestroyBullet()
    {
        yield return new WaitForSeconds(bulletLifeTime);

        if (this)
        {
            DestroyBulletServerRpc();
        }
            
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void DestroyBulletServerRpc()
    {
        //if (!IsOwner) return;
        //if (OwnerController) OwnerController.bulletCount--; // Have to use ammo it this ever starts working
        //else Debug.LogError("OwnerController is not set!");
        
        //NetworkManager.Singleton.SpawnManager.SpawnedObjects[0].Despawn();
        GetComponent<NetworkObject>().Despawn();
    }

    
    private void Update()
    {
        //if (OwnerController) _sprite.color = OwnerController.playerColor;
        
        
        if (prevLinearVelocity != _rb.linearVelocity)
        {
            _audio.PlayOneShot(bulletBounceSfx);
        }
        
        prevLinearVelocity = _rb.linearVelocity;
    }

    
}
