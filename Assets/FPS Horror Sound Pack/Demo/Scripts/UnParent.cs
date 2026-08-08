using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EtherealTerror
{
    public class UnParent : MonoBehaviour
    {
        void Start()
        {
            transform.parent = null;
        }
    }
}
