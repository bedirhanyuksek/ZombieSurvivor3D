using UnityEngine;
using System.Collections.Generic;

namespace EtherealTerror
{

    [System.Serializable]
    public class SurfaceType
    {
        public string surfaceTag;                  // Tag of the surface (e.g., "Wood", "Metal", "Grass")
        public AudioClip[] footstepSounds;         // Array of footstep sounds for this surface
    }

    public class FootstepController : MonoBehaviour
    {
        public AudioSource audioSource;             // AudioSource component to play the footstep sounds
        public List<SurfaceType> surfaceTypes;      // List of surface types and their corresponding sounds
        public float stepIntervalWalk = 0.4f;           // Time interval between steps when walking
        public float stepIntervalSprint = 0.7f;           // Time interval between steps when sprinting

        private CharacterController controller;     // Reference to the CharacterController component
        private float stepTimer = 0f;               // Timer to track time between steps
        private int lastPlayedIndex = -1;           // Index of the last played footstep sound

        public LayerMask groundLayer;               // Layer mask for ground detection

        public static bool walking;                 // Bool to check if player is walking

        void Start()
        {
            controller = GetComponent<CharacterController>();

            // If no AudioSource is assigned, use the one attached to the same GameObject
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        void Update()
        {
            if (controller.velocity.magnitude > 0.01f) // If player is walking
            {
                walking = true;
                stepTimer += Time.deltaTime;

                if (FirstPersonController.sprinting)
                {
                    if (stepTimer >= stepIntervalSprint)
                    {
                        PlayFootstepSound();
                        stepTimer = 0f;
                    }
                }
                else
                {
                    if (stepTimer >= stepIntervalWalk)
                    {
                        PlayFootstepSound();
                        stepTimer = 0f;
                    }
                }
            }
            else
            {
                walking = false;
            }
        }

        void PlayFootstepSound()
        {
            AudioClip[] selectedFootstepSounds = GetFootstepSoundsForCurrentSurface();

            if (selectedFootstepSounds != null && selectedFootstepSounds.Length > 0)
            {
                int newIndex;

                // Ensure the new sound is different from the last one played
                do
                {
                    newIndex = Random.Range(0, selectedFootstepSounds.Length);
                }
                while (newIndex == lastPlayedIndex);

                // Play the selected footstep sound
                audioSource.PlayOneShot(selectedFootstepSounds[newIndex]);

                // Store the index of the played sound
                lastPlayedIndex = newIndex;
            }
        }

        AudioClip[] GetFootstepSoundsForCurrentSurface()
        {
            RaycastHit hit;

            // Cast a ray downward from the character to detect the surface
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundLayer))
            {
                string surfaceTag = hit.collider.tag;

                // Find the SurfaceType that matches the tag
                foreach (SurfaceType surfaceType in surfaceTypes)
                {
                    if (surfaceType.surfaceTag == surfaceTag)
                    {
                        return surfaceType.footstepSounds;
                    }
                }
            }

            // Return null if no matching surface type is found
            return null;
        }
    }
}