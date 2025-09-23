using System;
using System.Collections.Generic;
using TMPro.Examples;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    [SerializeField] private float tankSpeed = 2, tankRotation = -60;
    
    [SerializeField] private GameObject bulletPrefab;
    
    private GameObject bulletObject;

    #endregion

    
    
    #region Network Variables

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
    }

    private struct BooleanArray : INetworkSerializable
    {
        public bool[] bools;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref bools);
        }
    }
    
    private NetworkVariable<int> randomNum = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private NetworkVariable<MyCustomData> customData = new NetworkVariable<MyCustomData>(new MyCustomData() { _int = 54, _bool = true }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Set true on spawn
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //private bool isDead = false;
    
    //private NetworkVariable<bool[]> spawnPointsOccupied = new NetworkVariable<bool[]>(new bool[5], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<BooleanArray> spawnPointsOccupied = new NetworkVariable<BooleanArray>( new BooleanArray() {bools = new bool[5]}, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
   
    public override void OnNetworkSpawn()
    {
        // If the network variable is changed from last update, do {}
        
        //randomNum.OnValueChanged += (int previousValue, int newValue) => { Debug.Log(OwnerClientId + "; randomNum: " + randomNum.Value); };
        //customData.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) => { Debug.Log(OwnerClientId + "; _int: " + newValue._int + ", _bool: " + newValue._bool); };
        
        // If isDead becomes true, SpawnPlayer()
        isDead.OnValueChanged += (value, newValue) => { if (newValue) { Debug.Log(OwnerClientId+ ": Spawned"); SpawnPlayer(); } };
    }
    
    #endregion
    
    
    
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // To spawn the player
        if (IsOwner)
        {
            isDead.Value = true;
            //SpawnPlayerServerRpc();
        }
    }

    
    void Update()
    {
        //Debug.Log(OwnerClientId + "; " + randomNum.Value);
        
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
            var deltaRotation = zRotation + turningDir * tankRotation * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, deltaRotation);
        }
        else // Move with WS
        if (inputDirection.Value.y != 0)
        {
            //print("Moving");
            //rb.linearVelocity = new Vector2(0, inputDirection.y * tankSpeed);
            
            var moveVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
            
            moveVector *= inputDirection.Value.y;
            
            rb.linearVelocity = moveVector.normalized * tankSpeed;
        }

        // If not pressing anything, set velocity to zero
        if (inputDirection.Value.x != 0 || inputDirection.Value.y == 0)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        
        #endregion
        
        
        if (shootPressed)
        {
            //randomNum.Value = Random.Range(0, 100);
            //customData.Value = new MyCustomData() { _int = 47, _bool = false };
            //TestServerRpc();

            //NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(bulletPrefab, OwnerClientId, true, false, false, transform.position, transform.rotation);
            
            //bulletObject = Instantiate(bulletPrefab, transform.position, transform.rotation);
            //bulletObject.GetComponent<NetworkObject>().Spawn(true);
            
            SpawnBulletServerRpc();
        }
    }

    [ServerRpc]
    private void TestServerRpc()
    {
        Debug.Log(OwnerClientId + " sends a function over the server.");
    }

    private int spawnPointUsed = -1;
    
    [ServerRpc]
    private void SpawnBulletServerRpc()
    {
        // Spawns the bullet and asks the server to do the same
        var muzzlePos = transform.GetChild(2).position;
        bulletObject = Instantiate(bulletPrefab, muzzlePos, transform.rotation);
        bulletObject.GetComponent<NetworkObject>().Spawn(true);
        
        // Start destroy timer on the bullet object (calling it here, means I don't have to implement network into the bullet script)
        StartCoroutine(bulletObject.GetComponent<Bullet>().DestroyBullet());
    }
    
    //[ServerRpc(RequireOwnership = false)]
    private void SpawnPlayer()
    {
        Debug.Log(OwnerClientId + ": activated spawn");

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
                print("Chose spawn point " + spawnPointID);
                
                // Claim spawn point
                spawnPointsOccupied.Value.bools[spawnPointID] = true;
                spawnPointUsed = spawnPointID;
                
                // Move to spawnPoint
                transform.position = spawnPoints[spawnPointID].position;
                break;
            }
        }
        
        characterControl = true;
        
        ActivateTankServerRpc(true);

        // Activates tank and gives control to the player
        //ActivateTank(true);
    }

    [ServerRpc]
    private void ActivateTankServerRpc(bool active)
    {
        Debug.Log(OwnerClientId + ": tried to visualize the tank");
        
        // De/Activate children
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(active);
        }
    }

    //[ServerRpc] private void IsDeadServerRpc(bool dead) { isDead.Value = dead; }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!IsOwner)
            return;
        
        if (other.gameObject.CompareTag("Bullet"))
        {
            Debug.Log(OwnerClientId + " died!");
            isDead.Value = true;
            //IsDeadServerRpc(true);
            
            //other.gameObject.GetComponent<NetworkObject>().Despawn();
            other.gameObject.GetComponent<Bullet>().DestroyBulletServerRpc(0);
        }
    }
}
