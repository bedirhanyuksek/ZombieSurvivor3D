using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EtherealTerror
{
    public class BookScript : MonoBehaviour
    {
        private bool isOpen;
        private Animation animator;
        [SerializeField] string openAnimation;
        [SerializeField] string closeAnimation;

        private void Start()
        {
            animator = GetComponent<Animation>();
        }

        public void UseBook()
        {
            isOpen = !isOpen;

            if (isOpen)
            {
                animator.Play(openAnimation);
            }
            else
            {
                animator.Play(closeAnimation);
            }
        }
    }
}
