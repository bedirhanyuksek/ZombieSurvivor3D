using UnityEngine;

namespace EtherealTerror
{
    public class Destroy : MonoBehaviour
    {
        [SerializeField] private float timeToDestroy;

        private void Awake()
        {
            Destroy(gameObject, timeToDestroy);
        }
    }
}
