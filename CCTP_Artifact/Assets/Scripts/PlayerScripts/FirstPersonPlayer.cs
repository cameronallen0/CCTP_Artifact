using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayer : MonoBehaviour
{
    private PlayerControls inputActions;

    private CharacterController controller;

    [SerializeField] private Camera cam;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float crouchSpeed = 5f;
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float runSpeed = 15f;
    [SerializeField] public float lookSensitivity = 30f;

    public LayerMask groundLayer;

    private float xRotation = 0f;

    // Movement Variables
    private Vector3 velocity;
    public float gravity = -19.62f;
    private bool grounded;
    private bool isRunning;

    //Rocket Variables
    private bool isLaunching = false;
    private Vector3 launchDirection;
    public float rocketHeight = 10f;

    //Jump Variables
    [SerializeField] private float jumpHeight = 3.0f;
    private bool isJumping;

    // Zoom Variables
    public float zoomFOV = 35.0f;
    public float zoomSpeed = 9f;
    private float targetFOV;
    private float baseFOV;

    // Crouch Variables
    private float initHeight;
    [SerializeField] private float crouchHeight;
    private bool isCrouching;

    private void Awake()
    {
        inputActions = new PlayerControls();
    }
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        initHeight = controller.height;
        Cursor.lockState = CursorLockMode.Locked;
        SetBaseFOV(cam.fieldOfView);
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void Update()
    {
        DoMovement();
        DoLooking();
        DoZoom();
        DoCrouch();
        DoJump();
        DoRun();
        DoFire();
    }

    private void DoLooking()
    {
        Vector2 looking = GetPlayerLook();
        float lookX = looking.x * lookSensitivity * Time.deltaTime;
        float lookY = looking.y * lookSensitivity * Time.deltaTime;

        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        transform.Rotate(Vector3.up * lookX);
    }

    private void DoMovement()
    {
        grounded = controller.isGrounded;
        if (grounded && velocity.y < 0)
        {
            velocity.y = -2f;
            isJumping = false;
            isRunning = false;
        }
        if(isCrouching && grounded)
        {
            movementSpeed = crouchSpeed;
        }
        else
        {
            movementSpeed = walkSpeed;
        }
        
        Vector2 movement = GetPlayerMovement();
        Vector3 move = transform.right * movement.x + transform.forward * movement.y;
        controller.Move(move * movementSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void DoRun()
    {
        if(isCrouching && grounded)
        {
            isRunning = false;
        }
        else
        {
            if (inputActions.FPSController.Run.ReadValue<float>() > 0)
            {
                isRunning = !isRunning;
                movementSpeed = runSpeed;
            }
            else
            {
                movementSpeed = walkSpeed;
            }
        }
    }

    private void DoZoom()
    {
        if (inputActions.FPSController.Zoom.ReadValue<float>() > 0)
        {
            targetFOV = zoomFOV;
        }
        else
        {
            targetFOV = baseFOV;
        }
        UpdateZoom();
    }

    private void DoCrouch()
    {
        if (inputActions.FPSController.Crouch.ReadValue<float>() > 0)
        {
            isCrouching = true;
            controller.height = crouchHeight;
            movementSpeed = crouchSpeed;
        }
        else
        {
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), 2.0f, -1))
            {
                controller.height = crouchHeight;
                movementSpeed = crouchSpeed;
            }
            else
            {
                controller.height = initHeight;
                movementSpeed = walkSpeed;
                isCrouching = false;

            }
        }
    }

    private void DoFire()
    {
        if (grounded)
        {
            if (inputActions.FPSController.Fire.triggered)
            {
                Vector2 clickPosition = GetMousePositionInWorld();

                RaycastHit hit;
                if (Physics.Raycast(cam.ScreenPointToRay(clickPosition), out hit, Mathf.Infinity, groundLayer))
                {
                    Debug.Log("SHOOT");

                    // Calculate the launch direction opposite to the raycast direction, considering character rotation
                    Vector3 raycastDirection = (hit.point - transform.position).normalized;

                    // Consider only the y-axis rotation to ensure backward launch
                    float characterRotationY = transform.eulerAngles.y;
                    launchDirection = Quaternion.Euler(0f, characterRotationY, 0f) * -Vector3.forward;

                    // Launch in the opposite direction
                    velocity.y = Mathf.Sqrt(rocketHeight * -2f * gravity);
                    isLaunching = true;
                }
            }
        }

        if (isLaunching)
        {
            // Check if the player has come to a stop
            if (velocity.magnitude < 0.1f)
            {
                isLaunching = false;
            }

            // Apply the launch direction
            controller.Move(launchDirection * 10f * Time.deltaTime);
        }
    }


    private Vector2 GetMousePositionInWorld()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            return hit.point;
        }

        return Vector2.zero;
    }

    private void DoJump()
    {
        if(grounded)
        {
            if (inputActions.FPSController.Jump.triggered)
            {
                isJumping = !isJumping;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }

    private void UpdateZoom()
    {
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }

    public void SetBaseFOV(float fov)
    {
        baseFOV = fov;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public Vector2 GetPlayerMovement()
    {
        return inputActions.FPSController.Move.ReadValue<Vector2>();
    }

    public Vector2 GetPlayerLook()
    {
        return inputActions.FPSController.Look.ReadValue<Vector2>();
    }
}
