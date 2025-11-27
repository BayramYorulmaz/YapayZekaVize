using UnityEngine;

public class sopaKarakter : MonoBehaviour
{
    public GameObject saldiriCollison;// saldýrý algýlama objesi

    public void SaldiriCollisonuAktif() // saldýrý algýlama objesini aktif eder 
    {
        saldiriCollison.SetActive(true);
    }
    public void SaldiriCollisonuPasif()// saldýrý algýlama objesini deaaktif eder 
    {
        saldiriCollison.SetActive(false);
    }

}
