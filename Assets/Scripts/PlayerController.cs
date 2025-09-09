using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
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

    private Vector2 inputDirection;

    private bool shootPressed;

    private void UpdateInputs()
    {
        inputDirection = actionAsset["Move"].ReadValue<Vector2>(); // WASD

        shootPressed = actionAsset["Jump"].WasPressedThisFrame(); // Spacebar
    }
    
    #endregion

    private Rigidbody2D rb;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputs();

        UpdateMovement();
        
    }


    private void UpdateMovement()
    {
        if (inputDirection.x != 0)
        {
            print("Turning");
            rb.angularVelocity = inputDirection.x;
        }
        else if (inputDirection.y != 0)
        {
            print("Moving");
            rb.linearVelocity = new Vector2(0, inputDirection.y);
        }
    }
}
