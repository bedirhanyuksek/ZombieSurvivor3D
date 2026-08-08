using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EtherealTerror
{
    [System.Serializable]
    public class events
    {
        public string eventName;
        public AudioClip eventSound;
        [HideInInspector]
        public bool isPlayed = false;
    }

    public class AnimationEventSound : MonoBehaviour
    {

        public float soundVolume = 1f;
        public List<events> SoundEvents = new List<events>();

        [SerializeField] AudioSource Audio;

        public void EventPlaySound(string SoundEvent)
        {
            for (int i = 0; i < SoundEvents.Count; i++)
            {
                if (SoundEvents[i].eventName == SoundEvent)
                {
                    if (SoundEvents[i].eventSound)
                    {
                        if (!Audio)
                        {
                            AudioSource.PlayClipAtPoint(SoundEvents[i].eventSound, transform.position, soundVolume);
                        }
                        else
                        {
                            Audio.clip = SoundEvents[i].eventSound;
                            Audio.volume = soundVolume;
                            Audio.Play();
                        }
                    }
                }
            }
        }
    }
}
