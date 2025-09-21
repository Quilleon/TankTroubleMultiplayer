using System;
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

    private void UpdateInputs()
    {
        inputDirection.Value = actionAsset["Move"].ReadValue<Vector2>(); // WASD
        Debug.Log(OwnerClientId + ": " + inputDirection.Value.x);

        shootPressed = actionAsset["Jump"].WasPressedThisFrame(); // Spacebar
    }
    
    #endregion

    
    
    
    #region Variables

    private Rigidbody2D rb;

    private float tankSpeed = 3, tankRotation = -60;

    #endregion

    
    
    #region Network Variables

    private NetworkVariable<int> randomNum = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #endregion
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(OwnerClientId + "; " + randomNum.Value);
        
        if (!IsOwner) return;

        if (shootPressed)
        {
            randomNum.Value = Random.Range(0, 100);
        }
        
        UpdateInputs();

        UpdateMovement();
    }


    private void UpdateMovement()
    {
        var zRotation = transform.eulerAngles.z;
        
        if (inputDirection.Value.x != 0)
        {
            int turningDir = inputDirection.Value.x > 0 ? 1 : -1;
            
            //print("Turning");
            var deltaRotation = zRotation + turningDir * tankRotation * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, deltaRotation);
        }
        else if (inputDirection.Value.y != 0)
        {
            //print("Moving");
            //rb.linearVelocity = new Vector2(0, inputDirection.y * tankSpeed);
            
            var moveVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * zRotation), Mathf.Sin(Mathf.Deg2Rad * zRotation));
            
            moveVector *= inputDirection.Value.y;
            
            rb.linearVelocity = moveVector.normalized * tankSpeed;
        }

        if (inputDirection.Value.x != 0 || inputDirection.Value.y == 0)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
