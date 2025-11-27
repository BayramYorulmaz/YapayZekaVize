using UnityEngine;

public class dusmanSopa : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<karakter>())
        {
            other.GetComponent<karakter>().hasarAl();
        }
    }
}
