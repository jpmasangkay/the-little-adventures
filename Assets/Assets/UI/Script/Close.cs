using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CartoonUI
{
    public class Close : MonoBehaviour
    {
        public GameObject targetGameObject;
        public void close()
        {
            if (targetGameObject != null)
            {
                targetGameObject.SetActive(false);
            }
        }
    }
}
