
using UnityEngine;

namespace EtherealTerror
{
    public class RecoilScript : MonoBehaviour
    {
        //Scripts
        private GunScript gunScript;

        //Main Camera
        [SerializeField] private Transform camRot;

        //Rotations
        private Vector3 currentRotation;
        private Vector3 targetRotation;

        //Recoil
        [SerializeField] private float recoilX;
        [SerializeField] private float recoilY;
        [SerializeField] private float recoilZ;

        //Settings
        [SerializeField] private float snappiness;
        [SerializeField] private float returnSpeed;


        private void Start()
        {
            gunScript = GetComponent<GunScript>();
        }

        void Update()
        {

            targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
            currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);
            camRot.transform.localRotation = Quaternion.Euler(currentRotation);
        }

        public void RecoilFire()
        {
            targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
        }
    }
}