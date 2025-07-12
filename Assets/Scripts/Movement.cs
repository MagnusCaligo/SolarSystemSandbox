using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public GameObject playerHead;

    public float movementSpeed = 1f;
    public float controllerRotateSpeed = 1f;
    public float mouseRotateSpeed = .05f;
    public float maxJetpackStength = 1f;

    private PlayerInput input = null;
    private Vector2 moveVector = Vector2.zero;
    private Vector2 lookVector = Vector2.zero;
    private float currentJetpackValue = 0f;
    private Rigidbody rb = null;

    private void Awake()
    {
        input = new PlayerInput();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.Log("Rigid body is null!");
    }

    
    private void OnEnable()
    {
        input.Enable();
        input.Player.Movement.performed += onMovementPerformed;
        input.Player.Movement.canceled += onMovementCancelled;

        input.Player.MouseCameraControl.performed += onMouseMovement;
        input.Player.MouseCameraControl.canceled += onLookRotationCancelled;

        input.Player.JoystickCameraControl.performed += onControllerMovement;
        input.Player.JoystickCameraControl.canceled += onLookRotationCancelled;

        input.Player.JetpackUp.performed += onJetpackUp;
        input.Player.JetpackUp.canceled += onJetpackCancelled;

        input.Player.JetpackDown.performed += onJetpackDown;
        input.Player.JetpackDown.canceled += onJetpackCancelled;
    }

    /*
    private void OnDisable()
    {
        input.Disable();
        input.Player.Movement.performed -= onMovementPerformed;
        input.Player.Movement.canceled -= onMovementCancelled;
    }
    */

    private void onMouseMovement(InputAction.CallbackContext value)
    {
        lookVector = mouseRotateSpeed * value.ReadValue<Vector2>();
    }

    private void onControllerMovement(InputAction.CallbackContext value)
    {
        lookVector = controllerRotateSpeed * value.ReadValue<Vector2>();
    }

    private void onLookRotationCancelled(InputAction.CallbackContext value)
    {
        lookVector = Vector2.zero;
    }

    private void onMovementPerformed(InputAction.CallbackContext value)
    {
        moveVector = value.ReadValue<Vector2>();
    }

    private void onMovementCancelled(InputAction.CallbackContext value)
    {
        moveVector = Vector2.zero;
    }

    private void onJetpackUp(InputAction.CallbackContext value)
    {
        currentJetpackValue = value.ReadValue<float>() * maxJetpackStength;
    }
    private void onJetpackDown(InputAction.CallbackContext value)
    {
        currentJetpackValue = value.ReadValue<float>() * -maxJetpackStength;
    }

    private void onJetpackCancelled(InputAction.CallbackContext value)
    {
        currentJetpackValue = 0f;
    }

    private void FixedUpdate()
    {
        Vector3 forwardMovement = (transform.forward * moveVector.y) * movementSpeed;
        Vector3 rightMovement = (transform.right * moveVector.x) * movementSpeed;
        rb.linearVelocity += forwardMovement + rightMovement;

        playerHead.transform.rotation *= Quaternion.Euler(new Vector3(-lookVector.y, 0, 0));
        transform.rotation *= Quaternion.Euler(new Vector3(0, lookVector.x, 0));

        rb.linearVelocity += transform.up * currentJetpackValue;
    }
}
