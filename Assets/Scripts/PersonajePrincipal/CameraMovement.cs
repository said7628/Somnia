using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;

    private float fixedZ;

    void Start()
    {
        fixedZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y,
            fixedZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}