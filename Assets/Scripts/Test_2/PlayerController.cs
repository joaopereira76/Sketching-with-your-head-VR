using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    
    private float speed = 0.04f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Navigation Mode")]
    [SerializeField] private float deadzone = 0.4f;
    public Transform root;
    public Transform cam;

    private Rigidbody rb;
    private float verticalRotation = 0f;
    private bool navigationMode = false;

    public SoundPlayer sound;


    [Header("Manual Z-Axis Movement")]
    [SerializeField] private float zMoveSpeed = 2f;


    private bool undoRedoMode = false;
    [SerializeField] private float undoRedoDeadzone = 3f;
    [SerializeField] private float undoRedoCooldown = 1.5f;
    private float undoRedoTimer = 0f;

    
    void Start()
    {
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void HandleZMovement(bool forward)
    {
        if (forward)
        {
            root.position += transform.forward * zMoveSpeed;
        }
        else
        {
            root.position -= transform.forward * zMoveSpeed;
        }
    }

    public void LookBasedXYMovementWithDeadzone()
    {
        Vector3 lookDirection = cam.forward;
        float x = lookDirection.x;
        float y = lookDirection.y;

        Vector3 move = new Vector3(x, y, 0f);


        if (Mathf.Abs(x) > deadzone || Mathf.Abs(y) > deadzone)
        {
            root.transform.position += move.normalized * speed;
        }
        
    }

    public void LookBasedUndoRedoWithDeadzone()
    {
        Vector3 lookDirection = cam.forward;
        float y = lookDirection.y;

        undoRedoTimer += Time.fixedDeltaTime; 

        if (undoRedoTimer >= undoRedoCooldown)
        {
            if (y > undoRedoDeadzone)
            {
                Debug.Log("Redo triggered by looking UP!");
                SingleMeshGrid grid = FindObjectOfType<SingleMeshGrid>();
                if (grid != null)
                {
                    sound.playErase();
                    grid.Redo();
                }
                undoRedoTimer = 0f;
            }
            else if (y < -undoRedoDeadzone)
            {
                Debug.Log("Undo triggered by looking DOWN!");
                SingleMeshGrid grid = FindObjectOfType<SingleMeshGrid>();
                if (grid != null)
                {
                    sound.playErase();
                    grid.Undo();
                }
                undoRedoTimer = 0f;
            }
        }
    }

 
}
