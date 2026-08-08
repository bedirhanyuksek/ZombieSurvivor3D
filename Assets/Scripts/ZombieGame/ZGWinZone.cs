using UnityEngine;

public sealed class ZGWinZone : MonoBehaviour
{
    [SerializeField] private ZGGameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ZGPlayerController>() == null)
        {
            return;
        }

        gameManager.Win();
    }
}
