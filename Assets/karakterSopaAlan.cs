using UnityEngine;

public class karakterSopaAlan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<huysuzAI>())
        {
            other.GetComponent<huysuzAI>().hasarAl();
        }

        if (other.GetComponent<UzunAI>())
        {
            other.GetComponent<UzunAI>().hasarAl();
        }
    }
}
