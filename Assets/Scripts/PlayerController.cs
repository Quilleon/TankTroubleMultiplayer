using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Random = UnityEngine.Random;

public class PlayerController : NetworkBehaviour
{
    #region Inputs
    
    private PlayerInput input;
    
    private InputActionAsset actionAsset;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();

        //ActivateTank(false);
        if (IsOwner)
        {
            Debug.Log(OwnerClientId + ": ");
            ActivateTankServerRpc(false);
        }
    }

    private void OnEnable()
    {
        if (!actionAsset)
            actionAsset = input.actions;
            
        actionAsset.Enable();
    }

    private void OnDisable()
    {
        actionAsset.Disable();
    }

    private NetworkVariable<Vector2> inputDirection = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool shootPressed;

    private bool characterControl = false;

    private void UpdateInputs()
    {
        inputDirection.Value = actionAsset["Move"].ReadValue<Vector2>(); // WASD
        //Debug.Log(OwnerClientId + ": " + inputDirection.Value.x);

        shootPressed = actionAsset["Jump"].WasPressedThisFrame(); // Spacebar
    }
    
    #endregion

    
    
    #region Variables

    private Rigidbody2D rb;

    private AudioSource audioSource;

    [SerializeField] private float tankSpeed = 2, tankRotation = 100;
    
    [SerializeField] private float respawnTime = 3;
    
    [SerializeField] private GameObject bulletPrefab;
    
    private GameObject bulletObject;
    
    private bool canShoot = true, willShoot;
    //[Range(0, 5)] 
    private int ammo, maxAmmo = 5;

    private float shootingCooldown = .2f, willShootBufferTimerTime = .5f, reloadTime = 8;

    
    private NetworkVariable<bool> firedBullet = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //private NetworkTransform bulletTransform;

    [SerializeField] private Color[] playerColors;
    public Color playerColor;
    
    [SerializeField] private AudioClip[] songs;
    
    [SerializeField] private AudioClip[] sfx;
    
    private int spawnPointUsed = -1;

    private GameObject deathScreenParent;


    #endregion

    
    
    #region Network Variables

    /*
    private struct MyCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message;
        
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }*/

    
    
    //private NetworkVariable<int> randomNum = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    //private NetworkVariable<MyCustomData> customData = new NetworkVariable<MyCustomData>(new MyCustomData() { _int = 54, _bool = true }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Set true on spawn
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //private bool isDead = false;
    
    private struct BooleanArray : INetworkSerializable
    {
        public bool[] bools;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref bools);
        }
    }
    
    //private NetworkVariable<bool[]> spawnPointsOccupied = new NetworkVariable<bool[]>(new bool[5], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<BooleanArray> spawnPointsOccupied = new NetworkVariable<BooleanArray>( 
        new BooleanArray() {bools = new bool[5]}, //{false, false, false, false, false}
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
   
    public override void OnNetworkSpawn()
    {
        // If the network variable is changed from last update, do {}
        
        //randomNum.OnValueChanged += (int previousValue, int newValue) => { Debug.Log(OwnerClientId + "; randomNum: " + randomNum.Value); };
        //customData.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) => { Debug.Log(OwnerClientId + "; _int: " + newValue._int + ", _bool: " + newValue._bool); };
        
        // If isDead becomes true, SpawnPlayer()
        isDead.OnValueChanged += (value, newValue) => { if (newValue)
        {
            if (!IsOwner) return; 
            Debug.Log(OwnerClientId+ ": Spawned"); StartCoroutine(PlayerDeath()); 
        } };
        
        /*
        firedBullet.OnValueChanged += (value, newValue) => { if (newValue) 
        { 
            Debug.Log(OwnerClientId+ " shot a bullet");
            
            //SpawnBulletServerRpc();
        } };
        */
    }
    
    #endregion
    
    
    
    
    void Start()
    {
        deathScreenParent = GameObject.Find("DeathScreenParent");
        
        // Assign color based on clientID
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        int colorID = (int)OwnerClientId;
        while (colorID >= playerColors.Length)
            colorID -= playerColors.Length;
        
        //print(OwnerClientId +  " joined and changed colour");
        spriteRenderer.color = playerColors[colorID];
        playerColor = spriteRenderer.color;
        
        audioSource = GetComponent<AudioSource>();
        
        // Checks if it is the owner that executes the script
        if (!IsOwner) return;
        
        // Get rigidbody
        rb = GetComponent<Rigidbody2D>();

        // Play music
        StartCoroutine(PlaySongWithIntro(songs[0], songs[1]));
        
        // To spawn the player
        //isDead.Value = true;
        PlayerSpawn();
    }

    private IEnumerator PlaySongWithIntro(AudioClip intro, AudioClip loop)
    {
        audioSource.PlayOneShot(intro);
        
        yield return new WaitForSeconds(intro.length + .1f); 
        
        audioSource.loop = true;
        audioSource.clip = loop;
        audioSource.Play();
    }

    
    void Update()
    {
        //bulletObject.GetComponentInChildren<SpriteRenderer>().color = playerColor;
        //if (bulletObject) bulletObject.GetComponent<Bullet>().OwnerController = this;
        
        //Debug.Log(OwnerClientId + "; " + randomNum.Value);

        // This can be avoided if the player only has one life and the map resets when respawning
        ammo = Mathf.Clamp(ammo, 0, 5);
        
        if (!IsOwner) return;
        
        
        UpdateInputs();

        UpdateActions();
    }


    private void UpdateActions()
    {
        // If no characterControl, disable actions
        if (!characterControl)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        
        #region Movement

       
        var zRotation = transform.eulerAngles.z;
        
        // Turn with AD
        if (inputDirection.Value.x != 0)
        {
            int turningDir = inputDirection.Value.x > 0 ? 1 : -1;
            
            //print("Turning");
            var deltaRotation = zRotation + turningDir * tankRotation * -1 * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, deltaRotation);
        }
        //else // Move with WS
        //if (inputDirection.Value.y != 0)
        {
            //print("Moving");
            //rb.linearVelocity = new Vector2(0, inputDirection.y * tankSpeed);
            
            var moveVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
            
            moveVector *= inputDirection.Value.y;
            
            rb.linearVelocity = moveVector.normalized * tankSpeed;
        }

        // If not pressing anything, set velocity to zero
        if (inputDirection.Value.y == 0) // || inputDirection.Value.x != 0 
        {
            rb.linearVelocity = Vector2.zero;
            
        }
        
        
        #endregion
        
        
        if (shootPressed && !canShoot)
        {
            willShoot = true;

            willShootBufferTimerIEnumerator = WillShootBufferTimer();
            StartCoroutine(willShootBufferTimerIEnumerator);
        }
        else if ((shootPressed || willShoot) && canShoot && ammo > 0) // Ammo and cooldown
        {
            //randomNum.Value = Random.Range(0, 100);
            //customData.Value = new MyCustomData() { _int = 47, _bool = false };
            //TestServerRpc();

            //NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(bulletPrefab, OwnerClientId, true, false, false, transform.position, transform.rotation);
            
            //bulletObject = Instantiate(bulletPrefab, transform.position, transform.rotation);
            //bulletObject.GetComponent<NetworkObject>().Spawn(true);
            
            /*
            var muzzlePos = transform.GetChild(2).position;
            bulletObject = Instantiate(bulletPrefab, muzzlePos, transform.rotation);
            
            bulletObject.GetComponentInChildren<SpriteRenderer>().color = playerColor;
            */

            //if (bulletCount < maxBulletCount)
            {
                //bulletCount++;
                //firedBullet.Value = true;
                //bulletTransform = 
                
                //SpawnBulletServerRpc();
                
                //bulletObject.GetComponent<Bullet>().OwnerController = this;
            }
            
            
            willShoot = false;
            if (willShootBufferTimerIEnumerator != null)
            {
                StopCoroutine(willShootBufferTimerIEnumerator);
                willShootBufferTimerIEnumerator = null;
            }

            StartCoroutine(ShootingCooldown());
            StartCoroutine(ReloadBullet());
            
            SpawnBulletServerRpc(transform.GetChild(2).position);
        }
    }

    private IEnumerator willShootBufferTimerIEnumerator;
    private IEnumerator WillShootBufferTimer()
    {
        yield return new WaitForSeconds(willShootBufferTimerTime);
        willShoot = false;
    }

    private IEnumerator ShootingCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootingCooldown);
        canShoot = true;
    }

    private IEnumerator ReloadBullet()
    {
        ammo--;
        yield return new WaitForSeconds(reloadTime);
        ammo++;
    }
    //[ServerRpc] private void TestServerRpc() { Debug.Log(OwnerClientId + " sends a function over the server."); }

    
    
    [ServerRpc]
    private void SpawnBulletServerRpc(Vector3 spawnPos)
    {
        // Add this to the bullet when spawning instead
        //audioSource.PlayOneShot(sfx[0]); // Bullet shot

        //if (!IsServer) return;
        
        // Spawns the bullet and asks the server to do the same

        if ((spawnPos - transform.position).magnitude > 1)
        {
            Debug.LogError("CHEATER!!!! Or you may be lagging a whole bunch, anyways, no bullet for you.");
            return;
        }
        
        var muzzlePos = spawnPos;
        
        bulletObject = Instantiate(bulletPrefab, muzzlePos, transform.rotation);
        bulletObject.GetComponent<NetworkObject>().Spawn(true);
        
        bulletObject.GetComponent<Bullet>().bulletColor.Value = playerColor;

        //if (IsServer) BulletOwnerRpc();
        
        //bulletObject.GetComponent<Bullet>().OwnerController = this;
        
        //bulletObject.GetComponentInChildren<SpriteRenderer>().color = playerColor;
        
        // Start destroy timer on the bullet object (calling it here, means I don't have to implement network into the bullet script)
        //StartCoroutine(bulletObject.GetComponent<Bullet>().DestroyBullet());
    }
    
    //[ServerRpc(RequireOwnership = false)]

    private IEnumerator PlayerDeath()
    {
        // Disable Tank
        ActivateTankServerRpc(false);
        
        //TODO: Play explosion animation
        audioSource.PlayOneShot(sfx[1]);
        
        // Client rpc calling the deathScreen
        ActivateDeathScreenClientRpc(true);
        
        yield return new WaitForSeconds(respawnTime);
        
        ActivateDeathScreenClientRpc(false);
        
        PlayerSpawn();
    }

    [ClientRpc]
    private void ActivateDeathScreenClientRpc(bool active)
    {
        //GameObject.Find("DeathScreen").SetActive(active);
        deathScreenParent.transform.GetChild(0).gameObject.SetActive(active);
    }
    
    private void PlayerSpawn()
    {
        //Debug.Log(OwnerClientId + " spawned");

        if (spawnPointUsed >= 0)
        {
            // Reset previously used spawnPoint
            spawnPointsOccupied.Value.bools[spawnPointUsed] = false;
        }
        
        // 
        isDead.Value = false;
        
        // Gets a random spawnPoint
        var spawnPointParent = GameObject.Find("SpawnPoints");

        // 
        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < spawnPointParent.transform.childCount; i++)
            spawnPoints.Add(spawnPointParent.transform.GetChild(i));
        
        // 
        for (int i = 0; i < spawnPoints.Count; i++)
            if (!spawnPoints[i].IsChildOf(spawnPointParent.transform))
                spawnPoints.RemoveAt(i);
            
        
        // Find an empty spawn point and spawn there
        for (int i = 0; i < spawnPointParent.transform.childCount; i++)
        {
            var spawnPointID = Random.Range(0, spawnPointParent.transform.childCount);
            var spawnPoint = spawnPoints[spawnPointID];
            
            if (spawnPointsOccupied.Value.bools[spawnPointID])
            {
                spawnPoints.Remove(spawnPoint);
            }
            else
            {
                //print("Chose spawn point " + spawnPointID);
                
                // Claim spawn point
                spawnPointsOccupied.Value.bools[spawnPointID] = true;
                spawnPointUsed = spawnPointID;

                foreach (var variable in spawnPointsOccupied.Value.bools)
                {
                    print(variable);
                }
                
                // Move to spawnPoint
                transform.position = spawnPoints[spawnPointID].position;
                break;
            }
        }
        
        // Gives control to the player and activates the tank
        //characterControl = true;
        //ammo = maxAmmo;
        ActivateTankServerRpc(true);
    }

    [ServerRpc]
    private void ActivateTankServerRpc(bool active)
    {
        characterControl = active;
        
        ammo = maxAmmo;
        
        //Debug.Log(OwnerClientId + ": tried to visualize the tank");
        
        // De/Activate children
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(active);
        }
    }
    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner)
            return;
        
        if (other.gameObject.CompareTag("Bullet"))
        {
            Debug.Log(OwnerClientId + " died!");
            isDead.Value = true;
            //IsDeadServerRpc(true);
            
            //other.gameObject.GetComponent<NetworkObject>().Despawn();
            other.gameObject.GetComponentInParent<Bullet>().DestroyBulletServerRpc();
        }
    }
}
