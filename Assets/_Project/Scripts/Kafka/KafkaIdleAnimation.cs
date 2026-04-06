using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Procedural idle animation for Kafka the corgi.
    /// Subtle breathing (body scale oscillation) + tail wag + ear twitch.
    /// Runs alongside KafkaFollowSimple.
    /// </summary>
    public class KafkaIdleAnimation : MonoBehaviour
    {
        [Header("Breathing")]
        [SerializeField] private float breathFrequency = 0.8f;
        [SerializeField] private float breathAmplitude = 0.008f;

        [Header("Tail Wag")]
        [SerializeField] private float tailWagFrequency = 3.5f;
        [SerializeField] private float tailWagAmplitude = 25f;

        [Header("Ear Twitch")]
        [SerializeField] private float earTwitchInterval = 4f;
        [SerializeField] private float earTwitchDuration = 0.3f;
        [SerializeField] private float earTwitchAngle = 8f;

        private Transform _body;
        private Transform _tail;
        private Transform _earL;
        private Transform _earR;

        private float _breathTime;
        private float _tailTime;
        private float _earTimer;
        private float _earTwitchTime;
        private bool _earTwitching;

        private Vector3 _bodyBaseScale;
        private Quaternion _tailBaseRot;
        private Quaternion _earLBaseRot;
        private Quaternion _earRBaseRot;

        private void Start()
        {
            _body = transform.Find("Body");
            _tail = transform.Find("Tail");
            _earL = transform.Find("EarL");
            _earR = transform.Find("EarR");

            if (_body != null) _bodyBaseScale = _body.localScale;
            if (_tail != null) _tailBaseRot = _tail.localRotation;
            if (_earL != null) _earLBaseRot = _earL.localRotation;
            if (_earR != null) _earRBaseRot = _earR.localRotation;

            _earTimer = Random.Range(2f, earTwitchInterval);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _breathTime += dt;
            _tailTime += dt;

            // Breathing: subtle Y-scale oscillation on body
            if (_body != null)
            {
                float breathOffset = Mathf.Sin(_breathTime * breathFrequency * Mathf.PI * 2f) * breathAmplitude;
                _body.localScale = _bodyBaseScale + new Vector3(0f, breathOffset, 0f);
            }

            // Tail wag: Z-axis rotation oscillation
            if (_tail != null)
            {
                float wagAngle = Mathf.Sin(_tailTime * tailWagFrequency * Mathf.PI * 2f) * tailWagAmplitude;
                _tail.localRotation = _tailBaseRot * Quaternion.Euler(0f, 0f, wagAngle);
            }

            // Ear twitch: occasional quick rotation
            _earTimer -= dt;
            if (_earTimer <= 0f && !_earTwitching)
            {
                _earTwitching = true;
                _earTwitchTime = 0f;
            }

            if (_earTwitching)
            {
                _earTwitchTime += dt;
                float t = _earTwitchTime / earTwitchDuration;
                if (t >= 1f)
                {
                    _earTwitching = false;
                    _earTimer = Random.Range(earTwitchInterval * 0.5f, earTwitchInterval * 1.5f);
                    if (_earL != null) _earL.localRotation = _earLBaseRot;
                    if (_earR != null) _earR.localRotation = _earRBaseRot;
                }
                else
                {
                    float angle = Mathf.Sin(t * Mathf.PI) * earTwitchAngle;
                    if (_earL != null)
                        _earL.localRotation = _earLBaseRot * Quaternion.Euler(angle, 0f, 0f);
                    if (_earR != null)
                        _earR.localRotation = _earRBaseRot * Quaternion.Euler(angle, 0f, 0f);
                }
            }
        }
    }
}
