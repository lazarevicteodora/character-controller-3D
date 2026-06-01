using UnityEngine;

// Idle animacija za objekte za sakupljanje - bob gore-dole i rotacija
// Svi parametri su podesivi kroz Inspector za lako prilagođavanje
public class CollectibleAnimator : MonoBehaviour
{
    [Header("Bob (gore-dole translacija)")]
    [SerializeField] private bool enableBob = true;
    [SerializeField] private float bobAmplitude = 0.25f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Rotacija")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (enableBob)
        {
            // Sin funkcija daje gladak gore-dole pokret - amplituda kontroliše visinu, speed frekvenciju
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = startPos + new Vector3(0f, yOffset, 0f);
        }

        if (enableRotation)
        {
            transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}
