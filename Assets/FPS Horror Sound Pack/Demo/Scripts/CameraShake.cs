using UnityEngine;
using System.Collections;

namespace EtherealTerror
{
    public class CameraShake : MonoBehaviour
    {
        // Duration and magnitude of the shake effect
        public float shakeDuration = 0.5f;
        public float shakeMagnitude = 0.3f;

        // Smooths the transition back to the original position
        public float dampingSpeed = 1.0f;

        // The original position of the camera
        private Vector3 initialPosition;

        // Reference to the main camera's transform
        private Transform cameraTransform;

        void Start()
        {
            // Find the Main Camera in the scene and get its transform
            cameraTransform = Camera.main.transform;

            // Store the camera's initial position
            initialPosition = cameraTransform.localPosition;
        }

        // Method to trigger the camera shake
        public void TriggerShake(float duration = 0, float magnitude = 0)
        {
            // If no custom values are passed, use the default values
            if (duration == 0) duration = shakeDuration;
            if (magnitude == 0) magnitude = shakeMagnitude;

            StartCoroutine(Shake(duration, magnitude));
        }

        private IEnumerator Shake(float duration, float magnitude)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                // Create random offsets for x and y positions
                float offsetX = Random.Range(-1f, 1f) * magnitude;
                float offsetY = Random.Range(-1f, 1f) * magnitude;

                // Apply the offsets to the camera's position
                cameraTransform.localPosition = new Vector3(initialPosition.x + offsetX, initialPosition.y + offsetY, initialPosition.z);

                // Increment the elapsed time
                elapsedTime += Time.deltaTime;

                yield return null; // Wait for the next frame
            }

            // Gradually move the camera back to the original position
            while (Vector3.Distance(cameraTransform.localPosition, initialPosition) > 0.01f)
            {
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, initialPosition, Time.deltaTime * dampingSpeed);
                yield return null;
            }

            // Ensure the camera returns exactly to its original position
            cameraTransform.localPosition = initialPosition;
        }
    }
}
