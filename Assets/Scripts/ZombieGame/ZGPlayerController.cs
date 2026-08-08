using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ZGHealth))]
public sealed class ZGPlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float minPitch = -45f;
    [SerializeField] private float maxPitch = 62f;
    [SerializeField] private Transform playerModel;
    [SerializeField] private Transform thirdPersonWeapon;
    [SerializeField] private Transform firstPersonWeapon;
    [SerializeField] private Vector3 thirdPersonCameraLocalPosition = new Vector3(1.15f, 0.55f, -4.25f);
    [SerializeField] private Vector3 firstPersonCameraLocalPosition = new Vector3(0f, 0.28f, 0.08f);

    private CharacterController controller;
    private ZGHealth health;
    private float verticalVelocity;
    private float pitch;
    private bool controlsLocked;
    private bool firstPerson;
    private Camera playerCamera;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<ZGHealth>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    private void OnEnable()
    {
        health.Died += LockControls;
    }

    private void OnDisable()
    {
        health.Died -= LockControls;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ApplyCameraMode();
    }

    private void Update()
    {
        if (controlsLocked)
        {
            return;
        }

        if (ZGInput.EscapePressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Look();
        if (ZGInput.ToggleCameraPressed)
        {
            ToggleCameraMode();
        }
        Move();
    }

    private void Look()
    {
        var look = ZGInput.LookDelta * lookSensitivity * ZGSettings.AimSensitivity;
        transform.Rotate(Vector3.up, look.x);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        if (cameraPivot != null)
        {
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }

    private void Move()
    {
        var input = ZGInput.Move;
        var move = transform.right * input.x + transform.forward * input.y;
        var speed = ZGInput.RunHeld ? runSpeed : walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (controller.isGrounded && ZGInput.JumpPressed)
        {
            verticalVelocity = jumpForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
        var velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void LockControls()
    {
        controlsLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ToggleCameraMode()
    {
        firstPerson = !firstPerson;
        ApplyCameraMode();
    }

    private void ApplyCameraMode()
    {
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = firstPerson ? firstPersonCameraLocalPosition : thirdPersonCameraLocalPosition;
            playerCamera.transform.localEulerAngles = Vector3.zero;
        }

        if (playerModel != null)
        {
            playerModel.gameObject.SetActive(!firstPerson);
        }
        if (thirdPersonWeapon != null)
        {
            thirdPersonWeapon.gameObject.SetActive(!firstPerson);
        }
        if (firstPersonWeapon != null)
        {
            firstPersonWeapon.gameObject.SetActive(firstPerson);
        }
    }
}
