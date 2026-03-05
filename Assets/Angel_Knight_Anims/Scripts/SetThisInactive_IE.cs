using System.Collections;
using UnityEngine;

namespace LowPolyAngelKnight
{
    public class SetThisInactive_IE : MonoBehaviour
    {
        public GameObject thisOne;
        public float seconds;

        // Update is called once per frame
        void Update()
        {
            StartCoroutine(setThisInactive());
        }

        IEnumerator setThisInactive()
        {
            yield return new WaitForSeconds(seconds);
            thisOne.SetActive(false);
        }
    }
}
