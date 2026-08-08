using UnityEngine;

namespace EtherealTerror
{
    public class WeaponSwitcher : MonoBehaviour
    {
        public GameObject[] weapons; // Array to hold all the weapons
        private int currentWeaponIndex = 0;
        private int nextWeaponIndex = 0;
        [SerializeField] float timeToWait = 1.0f; // Time to wait for unequip animation to finish
        private float timer = 0.0f;
        private bool isSwitching = false;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip UnEquipSound;

        void Start()
        {
            SelectWeapon(currentWeaponIndex); // Ensure only one weapon is active at the start
        }

        void Update()
        {
            // Decrease the timer
            if (timer > 0)
            {
                timer -= Time.deltaTime;

                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(UnEquipSound);
                }
                return; // Prevent further execution while waiting for unequip animation
            }
            else
            {
                OnUnequipAnimationEnd();
            }

            if (!isSwitching)
            {
                int previousWeaponIndex = currentWeaponIndex;

                // Scroll forward (up)
                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    nextWeaponIndex = currentWeaponIndex + 1;
                    if (nextWeaponIndex >= weapons.Length)
                    {
                        nextWeaponIndex = 0; // Loop back to the first weapon
                    }
                    StartSwitchingWeapon(previousWeaponIndex);
                }

                // Scroll backward (down)
                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    nextWeaponIndex = currentWeaponIndex - 1;
                    if (nextWeaponIndex < 0)
                    {
                        nextWeaponIndex = weapons.Length - 1; // Loop to the last weapon
                    }
                    StartSwitchingWeapon(previousWeaponIndex);
                }
            }
        }

        void StartSwitchingWeapon(int previousWeaponIndex)
        {
            if (previousWeaponIndex != nextWeaponIndex)
            {
                // Play unequip animation
                GetComponentInChildren<Animator>().SetTrigger("UnEquip");

                // Set the timer and mark that we're switching
                timer = timeToWait;
                isSwitching = true;
            }
        }

        // This function should be called when the unequip animation finishes
        public void OnUnequipAnimationEnd()
        {
            // Switch the weapon once the unequip animation is complete
            SelectWeapon(nextWeaponIndex);

            // Update current weapon index
            currentWeaponIndex = nextWeaponIndex;

            // Reset the switching flag
            isSwitching = false;
        }

        void SelectWeapon(int index)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].SetActive(i == index); // Activate the selected weapon, deactivate others
            }
        }
    }
}
