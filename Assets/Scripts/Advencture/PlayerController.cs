using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions controls;
    private Vector2 moveInput;
    private Camera mainCam;

    public float moveSpeed = 5f;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        mainCam = Camera.main;

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Update()
    {
        MovePlayer();
        SyncCameraRotationY();
    }

    private void MovePlayer()
    {
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }

    private void SyncCameraRotationY()
    {
        if (mainCam != null)
        {
            Vector3 camEuler = mainCam.transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
        }
    }
}