using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EtherealTerror
{
    public class LampScript : MonoBehaviour
    {
        bool on;
        [SerializeField] GameObject lightObject;

        [SerializeField] AudioClip onSound;
        [SerializeField] AudioClip offSound;
        [SerializeField] AudioSource audioSource;

        public void UseLamp()
        {
            on = !on;
            lightObject.SetActive(on);

            if (on)
            {
                audioSource.PlayOneShot(onSound);
            }
            else
            {
                audioSource.PlayOneShot(offSound);
            }
        }
    }
}
