using UnityEngine;

public sealed class ZGHitZone : MonoBehaviour
{
    [SerializeField] private bool headshot;

    public bool IsHeadshot => headshot;
}
