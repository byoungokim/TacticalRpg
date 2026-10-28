using System.Collections;
using Tactics.Core;
using UnityEngine;

namespace Tactics.View
{
    // Orbiting tactics camera: Q/E rotates in 90° steps, WASD pans, wheel zooms.
    public class CameraRig : MonoBehaviour
    {
        const float Pitch = 40f;
        const float MinDistance = 8f;
        const float MaxDistance = 26f;

        public Camera Camera { get; private set; }

        Vector3 _pivot, _pivotTarget;
        float _yaw = 45f, _yawTarget = 45f;
        float _distance = 18f, _distanceTarget = 18f;
        bool _cutIn;

        public void Init(Vector3 center)
        {
            Camera = gameObject.AddComponent<Camera>();
            Camera.fieldOfView = 30f;
            Camera.depth = 10f; // render over any default camera already in the scene
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
            _pivot = _pivotTarget = center;
            Apply();
        }

        public void FocusOn(Vector3 point) => _pivotTarget = point;

        void LateUpdate()
        {
            if (!_cutIn)
            {
                if (InputShim.RotateLeftDown) _yawTarget += 90f;
                if (InputShim.RotateRightDown) _yawTarget -= 90f;
                _distanceTarget = Mathf.Clamp(_distanceTarget - InputShim.Scroll * 1.5f, MinDistance, MaxDistance);

                var pan = InputShim.Pan;
                if (pan != Vector2.zero)
                {
                    var yawRot = Quaternion.Euler(0f, _yaw, 0f);
                    _pivotTarget += (yawRot * Vector3.right * pan.x + yawRot * Vector3.forward * pan.y) * 8f * Time.deltaTime;
                }
            }

            float k = 1f - Mathf.Exp(-8f * Time.deltaTime);
            _pivot = Vector3.Lerp(_pivot, _pivotTarget, k);
            _yaw = Mathf.LerpAngle(_yaw, _yawTarget, k);
            _distance = Mathf.Lerp(_distance, _distanceTarget, k);
            Apply();
        }

        void Apply()
        {
            var rot = Quaternion.Euler(Pitch, _yaw, 0f);
            transform.SetPositionAndRotation(_pivot - rot * Vector3.forward * _distance, rot);
        }

        // Quick close-up on the attacker for special skills, then back to the previous view.
        public IEnumerator CutIn(Vector3 from, Vector3 to)
        {
            var savedPivot = _pivotTarget;
            var savedDistance = _distanceTarget;
            _cutIn = true;
            _pivotTarget = Vector3.Lerp(from, to, 0.35f) + Vector3.up * 0.6f;
            _distanceTarget = 6f;
            yield return new WaitForSeconds(0.7f);
            _pivotTarget = savedPivot;
            _distanceTarget = savedDistance;
            yield return new WaitForSeconds(0.3f);
            _cutIn = false;
        }
    }
}
