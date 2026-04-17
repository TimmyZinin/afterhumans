using UnityEngine;

namespace Afterhumans.CameraRigs
{
    /// <summary>
    /// Fallback third-person follow camera for the meadow sandbox when
    /// Cinemachine FreeLook gives trouble. Spring-arm: lerps to a target
    /// offset behind + above the follow transform, with optional mouse X
    /// orbit. Attach to the Main Camera, assign Target = Kafka root.
    /// </summary>
    public class KafkaFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Rig")]
        [SerializeField] private float distance = 3.5f;
        [SerializeField] private float height = 1.5f;
        [SerializeField] private float lookAtHeight = 0.4f;
        [SerializeField] private float followLerp = 8f;

        [Header("Orbit (mouse X only)")]
        [SerializeField] private bool allowOrbit = true;
        [SerializeField] private float orbitSpeed = 180f;

        private float _orbitYaw;

        private void LateUpdate()
        {
            if (target == null) return;

            if (allowOrbit && Input.GetMouseButton(1))
                _orbitYaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;

            Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y + _orbitYaw, 0f);
            Vector3 desiredPos = target.position + yaw * new Vector3(0f, height, -distance);

            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }
    }
}
