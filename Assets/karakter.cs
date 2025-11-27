using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public class karakter : MonoBehaviour
{
    public NavMeshAgent karakterAgent; // karakterin NavMeshAgent componenti
    public float moveSpeed = 5.0f; // karakterin hareket hýzý
    public float DurmaMesafesi = 0.1f; // karakterin gidilmek istenilen konuma ne kadar yakýn olursa duracaðýný belirleyen deðiþken
    public bool hareket = false;// duþmanlarýn kovalama yaparken karakterin gideçeði noktaya doðru koþmasý için 
    public Vector3 hareketKonum;// karakterin gitmek istediði yer
    private Vector3 BakisKonumu = Vector3.zero;// karakterin Bakmak istediði yer
    [SerializeField] float donusHizi = 10f; // karakterin ne kadar hýzlý fareye doðru döneçeði
    private bool saldýrýCooldown = true; // karakterin saldýrýsýnýn hazýr olupp olmadýðý
    [SerializeField] float SaldiriBeklemeSuresi = 2f; // karakterin saldýrýlarý arasýnda ne kadar süre olacaðý
    public int can = 100;
    public Slider canbari;
    private void Awake()
    {
        karakterAgent.updateRotation = false;
    }
    void Update()
    {
        canbari.value = can;
        if (can <= 0)
        {
            Destroy(gameObject);
        }
        if (saldýrýCooldown == true)
        {
            RotasyonHesaplama();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) // eðer sol cliðe basýlmýþ ise
        {
            var cam = Camera.main;
            if (cam == null) { return; }
            Vector2 FareEkranPozisyonu = Mouse.current.position.ReadValue(); // farenin ekran üserindeki vectör 2 konumunu alýr
            Ray ray = cam.ScreenPointToRay(FareEkranPozisyonu);// kameradan oyun sahnesine ýþýn yani raycast atma
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 10000f)) // kameradan cýkn ýþýnýn bir objeye deyip deymediðini kontrol eder
            {
                karakterAgent.stoppingDistance = DurmaMesafesi; // agent componentindeki durma mesafesini istenilen durma mesafesine eþitle
                hareketKonum = hit.point; // hit ýþýnýn deydiði noktanýn konumunu alýr ve hareketKonum deðiþkenini o konuma eþitler
                karakterAgent.SetDestination(hareketKonum); // karakter oyuncunun istediði noktaya doðru hareket etmeye baþlar
                hareket = true;  // duþmanlarýnda algýlamasý için bool deðerini deðiþtir
            }
        }
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(hareketKonum.x, 0, hareketKonum.z)) <= DurmaMesafesi) // gidilecek yere istenilen kadar yaklaþýldýðýný kontrol et
        {
            hareket = false; // duþmanlarýnda algýlamasý için bool deðerini deðiþtir
            karakterAgent.ResetPath();// gidilecek yere yaklaþýnca hareketi kes
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)// sað fare tekerleðine basýlma çekildiðinde saldýrýyý yap
        {
            saldiriYap();
        }

    }

    private void RotasyonHesaplama()// karakterin fareye bakmasýnýn hesaplandýðý yer
    {
        var cam = Camera.main;
        if (cam == null) { return; }
        Vector2 FareEkranPozisyonu = Mouse.current.position.ReadValue(); // farenin ekran üserindeki vectör 2 konumunu alýr
        Ray ray = cam.ScreenPointToRay(FareEkranPozisyonu);// kameradan oyun sahnesine ýþýn yani raycast atma
        RaycastHit hit;
        {
            if (Physics.Raycast(ray, out hit, 10000f)) // kameradan cýkn ýþýnýn bir objeye deyip deymediðini kontrol eder
            {
                BakisKonumu = hit.point;// bakýþ konumunu ayarlar
            }
            else
            {
                BakisKonumu = Vector3.zero;// eðer fare bir objenin üzerinde deðil ise bakýþ yönü 0 0 0 olur
            }
        }
        Vector3 bakisYonu = BakisKonumu - transform.position; //bakýþ yönünü hesaplýyoruz
        bakisYonu.y = 0; // y eksenini hesapladan cýkarýyoruz
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(bakisYonu), Time.deltaTime * donusHizi);// karakteri y ekseninde yumuþak bir þekilde döndürüyoruz
    }


    public Animator karakterSaldiriAnimator;

    public void saldiriYap()// karakter saldýrý harekerine baþlar
    {
        if (saldýrýCooldown) // eðer saldýrý aktif ise saldýrý yap
        {

            StartCoroutine(saldiriBeklemeSuresi()); // saldýrýyý bekleme süresine sokar
            karakterSaldiriAnimator.SetTrigger("saldiri");

        }
    }

    public void hasarAl()
    {
        Debug.Log("karakter Hasar Aldý");
        can -= 20;
    }
    private IEnumerator saldiriBeklemeSuresi()
    {
        saldýrýCooldown = false;
        float x = Time.time;
        while (x + SaldiriBeklemeSuresi >= Time.time)
        {
            saldýrýCooldown = false;
            yield return null;
        }
        yield return null;
        saldýrýCooldown = true;
        // SaldiriBeklemeSuresi kadar süre beklendikten sonra saldýrýyý aktif eder
    }


}
