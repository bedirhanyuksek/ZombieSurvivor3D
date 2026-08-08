using UnityEngine;

namespace EtherealTerror
{
    public class MeleeScript : MonoBehaviour
    {
        [SerializeField] float attackRange = 5f; // Range of the melee attack
        [SerializeField] float attackRate = 1f; // Attack cooldown
        [SerializeField] GameObject impactEffect;        // Impact effect when hitting something
        [SerializeField] GameObject impactEffectFlesh;   // Impact effect when hitting flesh
        [SerializeField] AudioClip[] attackSounds;        // Array of sounds for shooting
        private AudioSource audioSource;
        private Animator animator;
        private int attackIndex;
        private float attackTimer;

        private void Start()
        {
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            attackIndex = 1;
        }

        void Update()
        {
            animator.SetBool("Walk", FootstepController.walking);
            animator.SetBool("Run", FirstPersonController.sprinting);
            animator.SetBool("Idle", !FootstepController.walking);

            attackTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && attackTimer < 0) // Trigger attack with left mouse button
            {
                Attack();
            }
        }

        void Attack()
        {
            attackTimer = attackRate;

            if (attackIndex < 3)
            {
                attackIndex += 1;
            }
            else
            {
                attackIndex = 1;
            }

            animator.SetTrigger("Attack " + attackIndex);

            // Play a random attack sound
            if (attackSounds != null && attackSounds.Length > 0)
            {
                AudioClip randomShootSound = attackSounds[Random.Range(0, attackSounds.Length)];
                audioSource.PlayOneShot(randomShootSound);
            }

            // Perform the raycast
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, attackRange))
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
    }
}
