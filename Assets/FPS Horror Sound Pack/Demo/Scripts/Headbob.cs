using UnityEngine;

namespace EtherealTerror
{
    public class HeadBob : MonoBehaviour
    {
        public float bobSpeed = 14f; // Speed of the head bob
        public float bobAmountWalking = 0.05f; // Amount of movement in the head bob when walking
        public float bobAmountSprinting = 0.07f; // Amount of movement in the head bob when sprinting

        private float defaultYPos = 0f;
        private float timer = 0f;
        private float translateChange;

        void Start()
        {
            defaultYPos = transform.localPosition.y;
        }

        void Update()
        {
            float waveslice = 0f;
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0)
            {
                timer = 0f;
            }
            else
            {
                waveslice = Mathf.Sin(timer);
                timer += bobSpeed * Time.deltaTime;
                if (timer > Mathf.PI * 2)
                {
                    timer -= Mathf.PI * 2;
                }
            }

            if (waveslice != 0)
            {
                if (FirstPersonController.sprinting)
                {
                    translateChange = waveslice * bobAmountSprinting;
                }
                else
                {
                    translateChange = waveslice * bobAmountWalking;
                }
                float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
                totalAxes = Mathf.Clamp(totalAxes, 0f, 1f);
                translateChange = totalAxes * translateChange;
                transform.localPosition = new Vector3(transform.localPosition.x, defaultYPos + translateChange, transform.localPosition.z);
            }
            else
            {
                transform.localPosition = new Vector3(transform.localPosition.x, defaultYPos, transform.localPosition.z);
            }
        }
    }
}
