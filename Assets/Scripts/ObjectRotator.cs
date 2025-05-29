using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("Target")]
    public Transform targetObject;

    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0f, 50f, 0f);

    [Header("Wobble Settings")]
    [Range(0f, 1f)]
    public float wobblinessX = 0f;

    [Range(0f, 1f)]
    public float wobblinessY = 0f;

    [Range(0f, 1f)]
    public float wobblinessZ = 0f;

    [Range(0.1f, 10f)]
    public float wobbleFrequency = 1f;

    private Vector3 baseSpeed;

    void Start()
    {
        if (targetObject == null)
            targetObject = transform;

        baseSpeed = rotationSpeed;
    }

    void Update()
    {
        if (targetObject == null) return;

        Vector3 currentSpeed = baseSpeed;

        float wobble = Mathf.Sin(Time.time * wobbleFrequency * Mathf.PI * 2f);

        if (wobblinessX > 0f)
            currentSpeed.x = baseSpeed.x + (wobble * wobblinessX * baseSpeed.x);

        if (wobblinessY > 0f)
            currentSpeed.y = baseSpeed.y + (wobble * wobblinessY * baseSpeed.y);

        if (wobblinessZ > 0f)
            currentSpeed.z = baseSpeed.z + (wobble * wobblinessZ * baseSpeed.z);

        targetObject.Rotate(currentSpeed * Time.deltaTime);
    }
}