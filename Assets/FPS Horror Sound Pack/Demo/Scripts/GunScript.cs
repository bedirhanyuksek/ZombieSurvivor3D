using UnityEngine;
using System.Collections;

namespace EtherealTerror
{
    public class GunScript : MonoBehaviour
    {
        [SerializeField] float fireRate = 0.1f;          // Time between shots
        [SerializeField] float reloadTime = 2.0f;        // Time to reload
        [SerializeField] AudioClip[] shootSounds;        // Array of sounds for shooting
        [SerializeField] ParticleSystem muzzleFlash;         // Muzzle flash effect
        [SerializeField] GameObject impactEffect;        // Impact effect when hitting something
        [SerializeField] GameObject impactEffectFlesh;   // Impact effect when hitting flesh
        [SerializeField] GameObject bulletCasingSound; // The object that plays the sound of the bullet casing hitting the ground
        [SerializeField] Transform bulletCasingSpawn; // The transform that specifies where the bulletCasing object should be instantiated

        private bool isReloading = false;      // Is the gun currently reloading
        private float nextTimeToFire = 0f;     // Time until the next shot can be fired
        private Animator armAnimator;             // Reference to the Animator component
        [SerializeField] Animator gunAnimator;             // Reference to the Animator component
        private AudioSource audioSource;       // Reference to the AudioSource component

        private RecoilScript recoil_Script;     // Recoil reference

        private CameraShake camera_Shake; // Camera shake reference

        void Start()
        {
            armAnimator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            recoil_Script = GetComponent<RecoilScript>();
            camera_Shake = GetComponent<CameraShake>();
        }

        void Update()
        {
            if (isReloading)
                return;

            // Animation
            armAnimator.SetBool("Walk", FootstepController.walking);
            gunAnimator.SetBool("Walk", FootstepController.walking);

            armAnimator.SetBool("Idle", !FootstepController.walking);
            gunAnimator.SetBool("Idle", !FootstepController.walking);

            armAnimator.SetBool("Run", FirstPersonController.sprinting);
            gunAnimator.SetBool("Run", FirstPersonController.sprinting);

            // Check if shooting
            if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + fireRate;
                Shoot();
            }

            // Check for reload
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(Reload());
            }
        }

        void Shoot()
        {
            recoil_Script.RecoilFire();

            camera_Shake.TriggerShake();

            // Play shoot animation
            armAnimator.SetTrigger("Shoot");
            gunAnimator.SetTrigger("Shoot");

            // Instantiate bulletCasingSound object
            Instantiate(bulletCasingSound, bulletCasingSpawn.position, bulletCasingSpawn.rotation);

            // Play muzzle flash

            muzzleFlash.Play();


            // Play a random shoot sound
            if (shootSounds != null && shootSounds.Length > 0)
            {
                AudioClip randomShootSound = shootSounds[Random.Range(0, shootSounds.Length)];
                audioSource.PlayOneShot(randomShootSound);
            }

            // Perform the raycast
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                // Instantiate impact effect at the point of impact
                if (impactEffect != null)
                {
                    if (hit.transform.tag == "Flesh")
                    {
                        var bulletholeFlesh = Instantiate(impactEffectFlesh, hit.point, Quaternion.LookRotation(hit.normal));
                        bulletholeFlesh.transform.parent = hit.transform;
                    }
                    else
                    {
                        var bullethole = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        bullethole.transform.parent = hit.transform;
                    }
                }
            }
        }

        IEnumerator Reload()
        {
            isReloading = true;

            // Play reload animation
            armAnimator.SetTrigger("Reload");
            gunAnimator.SetTrigger("Reload");

            // Wait for the reload time to finish
            yield return new WaitForSeconds(reloadTime);

            isReloading = false;
        }
    }
}
