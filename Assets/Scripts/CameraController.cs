using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Distance")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 10f;

    [Header("Rotation")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 70f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionPadding = 0.3f;

    private float rotX;
    private float rotY = 15f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // LateUpdate se izvršava nakon svih Update poziva - kamera prati karakter bez trzanja
    private void LateUpdate()
    {
        rotX += Input.GetAxis("Mouse X") * sensitivity;
        rotY -= Input.GetAxis("Mouse Y") * sensitivity;
        rotY  = Mathf.Clamp(rotY, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation    = Quaternion.Euler(rotY, rotX, 0f);
        Vector3    pivotPoint  = target.position + targetOffset;
        Vector3    desiredPos  = pivotPoint + rotation * (-Vector3.forward * distance);

        // Sprečavamo prolaz kamere kroz geometriju
        if (Physics.Linecast(pivotPoint, desiredPos, out RaycastHit hit, collisionMask))
            desiredPos = hit.point + hit.normal * collisionPadding;

        transform.position = desiredPos;
        transform.LookAt(pivotPoint);
    }

    // Javna metoda za otključavanje kursora (poziva PauseMenu)
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
