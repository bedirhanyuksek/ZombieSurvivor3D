using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EtherealTerror
{
    public class doorScript : MonoBehaviour
    {
        [SerializeField] Animation anim;
        [SerializeField] AudioClip openSound;
        [SerializeField] string doorOpenAnimation;
        [SerializeField] string doorCloseAnimation;
        private bool doorOpen;
        private float cooldown;
        private AudioSource audioSource;

        private void Start()
        {
            doorOpen = false;
            audioSource = GetComponent<AudioSource>();
        }

        public void UseDoor()
        {
            if (cooldown > 0)
            {
                return;
            }

            cooldown = 1;

            doorOpen = !doorOpen;

            if (doorOpen)
            {
                anim.Play(doorOpenAnimation);
                audioSource.PlayOneShot(openSound);
            }
            else
            {
                anim.Play(doorCloseAnimation);
            }
        }

        private void FixedUpdate()
        {
            cooldown -= Time.deltaTime;
        }
    }
}