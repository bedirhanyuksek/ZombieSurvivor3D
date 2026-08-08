using System.Collections;
using UnityEngine;

namespace EtherealTerror
{
    public class PlayRandomSound : MonoBehaviour
    {
        public AudioClip[] sounds; // Array to store the sound clips
        private AudioSource audioSource; // The AudioSource component
        private int lastPlayedIndex = -1; // Index of the last played sound
        [SerializeField] float timer;

        void Start()
        {
            // Get the AudioSource component attached to the same GameObject
            audioSource = GetComponent<AudioSource>();

            StartCoroutine("TimerToPlaySound");
        }

        IEnumerator TimerToPlaySound()
        {
            yield return new WaitForSeconds(timer);
            PlaySound();
        }

        public void PlaySound()
        {
            if (sounds.Length == 0) return; // No sounds to play

            int newIndex;

            // Ensure the new index is different from the last played one
            do
            {
                newIndex = Random.Range(0, sounds.Length);
            }
            while (newIndex == lastPlayedIndex);

            // Play the selected sound
            audioSource.clip = sounds[newIndex];
            audioSource.Play();

            // Update the last played index
            lastPlayedIndex = newIndex;
        }
    }
}
