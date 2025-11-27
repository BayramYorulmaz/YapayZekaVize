using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class UzunAI : MonoBehaviour
{
    // Yapay zekanýn davranýþ durumlarýný (State Machine) tanýmlýyoruz
    // Uzun karakteri 'Kacma' davranýþý sergilemiyor, daha agresif/doðrudan bir yapýsý var
    public enum State { Devriye, Kovalama, TahminiYereGitme, Saldirma }

    State state; // Mevcut durumu tutan deðiþken

    [Header("Ayarlar")]
    public Collider devriyeYeri; // Devriye atýlacak alanýn sýnýrlarý
    public GameObject karakter; // Hedef alýnan oyuncu karakteri
    public NavMeshAgent UzunAgent; // Hareket kontrolcüsü
    Vector3 GidilmekIstenilenYer; // Hedef koordinat

    [Header("Hareket ve Saldýrý Deðerleri")]
    public float DevriyeHizi = 2.5f; // Sakin dolaþma hýzý
    public float KovalamaHizi = 2.5f; // Oyuncuyu kovalarken kullanýlacak hýz (Ýleride hýzlandýrmak için)
    public float saldiriMesafesi = 4f; // Saldýrý animasyonuna geçiþ mesafesi

    [Header("Durum Deðerleri")]
    public int Can = 100; // Saðlýk puaný
    public Animator DusmanSaldiriAnimator; // Saldýrý animasyonlarýný yöneten bileþen
    public Slider canbari; // UI üzerindeki can göstergesi
    public GameObject UzunGidilmekIstenilenYerObje; // Debug: Hedef noktayý görselleþtiren obje

    // Coroutine'lerin üst üste binmesini engellemek için referans tutucular
    private Coroutine devriye = null;
    private Coroutine TahminiYereGitme = null;
    private Coroutine Saldirma = null;

    private void Awake()
    {
        state = State.Devriye; // Oyun baþlar baþlamaz devriye moduna geç
    }

    private void Start()
    {
        if (UzunAgent != null)
        {
            UzunAgent.stoppingDistance = 0.1f; // Hedefe ne kadar kala duracaðý
            UzunAgent.speed = DevriyeHizi; // Baþlangýç hýzý devriye hýzý olarak ayarlanýr
        }
    }

    private void FixedUpdate()
    {
        // Ölüm kontrolü
        if (Can <= 0)
        {
            Destroy(gameObject); // Can bittiyse objeyi yok et
        }

        // Can barýný anlýk güncelle
        canbari.value = Can;

        // Mesafeye göre Durum Geçiþ Kontrolü
        if (state == State.Kovalama)
        {
            // Eðer oyuncuya yeterince yaklaþýldýysa Saldýrý moduna geç
            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(karakter.transform.position.x, 0, karakter.transform.position.z)) <= saldiriMesafesi)
            {
                state = State.Saldirma;
            }
        }

        // Debug objesini hedefe taþý
        UzunGidilmekIstenilenYerObje.transform.position = GidilmekIstenilenYer;

        // Durum Makinesi (State Machine) Switch Bloðu
        switch (state)
        {
            case State.Devriye: // Etrafta rastgele dolaþma
                if (devriye == null)
                {
                    devriye = StartCoroutine(Devriye()); // Devriye rutini çalýþmýyorsa baþlat
                }
                GorusAlaniniTara(); // Düþman var mý diye etrafý tara
                break;

            case State.Kovalama: // Oyuncuyu doðrudan takip etme
                kovalama(); // NavMesh hedefini oyuncu yap
                GorusAlaniniTara(); // Oyuncuyu hala görüyor muyuz diye kontrol et
                break;

            case State.TahminiYereGitme: // Oyuncu gözden kaybolunca son görülen yere gitme
                if (TahminiYereGitme == null)
                {
                    TahminiYereGitme = StartCoroutine(TahmineGitmeRutin());
                }
                GorusAlaniniTara(); // Yolda giderken tekrar görürse yakalamak için tarama yap
                break;

            case State.Saldirma: // Saldýrý animasyonu ve bekleme
                if (Saldirma == null)
                {
                    Saldirma = StartCoroutine(SaldirmaRutin());
                }
                break;
        }
    }

    // Dýþarýdan hasar almak için kullanýlan fonksiyon
    public void hasarAl()
    {
        Debug.Log("Uzun Hasar Aldý");
        Can -= 35;
    }

    // Saldýrý Mantýðý Coroutine
    public IEnumerator SaldirmaRutin()
    {
        float T = Time.time;
        GidilmekIstenilenYer = transform.position; // Saldýrý anýnda olduðu yerde dursun
        DusmanSaldiriAnimator.SetTrigger("saldiri"); // Animasyonu tetikle

        // 3 saniye boyunca saldýrý yap
        while (T + 3 >= Time.time)
        {
            karaktereBak(); // Sürekli oyuncuya dön
            UzunAgent.SetDestination(GidilmekIstenilenYer);
            state = State.Saldirma; // Durumu koru
            yield return null;
        }

        // Saldýrý bitince tekrar kovalama moduna dön
        state = State.Kovalama;
        yield return null;
        Saldirma = null; // Coroutine'i temizle
        yield return null;
        Saldirma = null; // (Tekrar garantiye almak için)
    }

    // Devriye Mantýðý Coroutine
    public IEnumerator Devriye()
    {
        UzunAgent.updateRotation = true; // Dönüþ hareketini aç
        Bounds b = devriyeYeri.bounds; // Devriye sýnýrlarýný al
        Vector3 picket = transform.position;
        bool found = false;

        // NavMesh üzerinde rastgele geçerli bir nokta bulmak için 10 deneme yap
        for (int i = 0; i < 10; i++)
        {
            Vector3 rnd = new Vector3(Random.Range(b.min.x, b.max.x), b.center.y, Random.Range(b.min.z, b.max.z));
            if (UzunAgent != null)
            {
                // NavMesh üzerinde mi kontrol et (SamplePosition)
                if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    picket = hit.position;
                    found = true;
                    break;
                }
                else
                {
                    picket = rnd; // Bulamazsa rastgele noktayý al
                    found = true;
                    break;
                }
            }
        }

        // Hedef noktayý ayarla
        GidilmekIstenilenYer = found ? picket : b.center;
        GidilmekIstenilenYer.y = transform.position.y; // Yükseklik hatasýný önle

        if (UzunAgent != null && UzunAgent.enabled && UzunAgent.isOnNavMesh)
        {
            UzunAgent.SetDestination(GidilmekIstenilenYer);
        }

        // Hedefe gidene kadar bekle (Maksimum 10 saniye süre taný)
        float T = Time.time;
        bool bekle = false;
        while (T + 10 >= Time.time)
        {
            // Hedefe 0.2 birim yaklaþtý mý?
            if (Vector3.Distance(new Vector3(GidilmekIstenilenYer.x, 0, GidilmekIstenilenYer.z), new Vector3(transform.position.x, 0, transform.position.z)) <= 0.2f)
            {
                Debug.Log("Uzun Devriye noktasýna geldi");
                bekle = true;
                T = 0;
                break;
            }
            yield return null;
        }

        // Hedefe vardýysa 2 saniye bekle
        T = Time.time;
        if (bekle == true)
        {
            while (T + 2 >= Time.time)
            {
                yield return null;
            }
        }

        // Yeni bir devriye noktasý için rutini yeniden baþlat
        devriye = null;
        devriye = StartCoroutine(Devriye());
    }

    // Kovalama Fonksiyonu (Update içinde sürekli çaðrýlýr)
    public void kovalama()
    {
        karaktereBak(); // Oyuncuya dön
        GidilmekIstenilenYer = karakter.transform.position; // Hedef oyuncunun konumu
        GidilmekIstenilenYer.y = transform.position.y;
        UzunAgent.SetDestination(GidilmekIstenilenYer);
    }

    // Görüþ Alaný Tarama (Raycast ile)
    void GorusAlaniniTara()
    {
        int raySayisi = 20; // 20 adet ýþýn atýlacak
        float toplamAci = 90f; // Tarama açýsý
        float menzil = 15f; // Görme mesafesi
        float baslangicAcisi = -toplamAci / 2; // -45 dereceden baþla
        float aciAdimi = toplamAci / (raySayisi - 1);

        for (int i = 0; i < raySayisi; i++)
        {
            float gecerliAci = baslangicAcisi + (aciAdimi * i);
            Vector3 yon = Quaternion.Euler(0, gecerliAci, 0) * transform.forward; // Iþýn yönü
            Vector3 cikisNoktasi = transform.position;
            RaycastHit hit;

            // SphereCast ile hacimli bir ýþýn gönder
            if (Physics.SphereCast(cikisNoktasi, 0.5f, yon, out hit, menzil))
            {
                // Çarpan obje bizim karakterimiz mi?
                if (hit.collider.gameObject.GetComponent<karakter>())
                {
                    Debug.DrawRay(cikisNoktasi, yon * hit.distance, Color.black); // Görünce siyah çizgi
                    Debug.Log("Uzun oyuncuyu gördü");
                    state = State.Kovalama; // Gördüðü an Kovalama moduna geç
                    return; // Fonksiyondan çýk (Gördükten sonra diðer ray'leri atmaya gerek yok)
                }
                else
                {
                    Debug.DrawRay(cikisNoktasi, yon * hit.distance, Color.red); // Engel varsa kýrmýzý
                }
            }
            else
            {
                Debug.DrawRay(cikisNoktasi, yon * menzil, Color.green); // Boþluksa yeþil
            }
        }

        // Döngü bitti ve "return" olmadýysa, demek ki oyuncuyu göremiyoruz.
        // Eðer þu an kovalama durumundaysak ve oyuncuyu göremiyorsak, tahmini yere gitme moduna geç.
        if (state == State.Kovalama)
        {
            state = State.TahminiYereGitme;
        }
    }

    // Tahmini Yere Gitme (Kayýp durumunda arama)
    public IEnumerator TahmineGitmeRutin()
    {
        UzunAgent.updateRotation = true;

        // Son bilinen konuma gidene kadar bekle
        while (Vector3.Distance(new Vector3(GidilmekIstenilenYer.x, 0, GidilmekIstenilenYer.z), new Vector3(transform.position.x, 0, transform.position.z)) >= 0.2f)
        {
            UzunAgent.SetDestination(GidilmekIstenilenYer);
            yield return null;
        }

        // Oraya varýnca 1 saniye etrafa bakýn
        float T = Time.time;
        while (T + 1 >= Time.time)
        {
            yield return null;
            // Eðer bu sýrada tekrar görürsek (GorusAlaniniTara sayesinde state deðiþirse) rutini kýr
            if (state == State.Kovalama)
            {
                TahminiYereGitme = null;
                break;
            }
        }

        TahminiYereGitme = null;

        // Eski devriye rutinini temizle
        if (devriye != null)
        {
            StopCoroutine(devriye);
            devriye = null;
        }

        // Oyuncu bulunamazsa Devriye moduna geri dön
        state = State.Devriye;
    }

    // Yapay zekanýn yüzünü karaktere döndürme
    public void karaktereBak()
    {
        UzunAgent.updateRotation = false; // NavMesh'in otomatik dönüþünü kapat
        Vector3 bakis = karakter.gameObject.transform.position;
        bakis = bakis - transform.position; // Hedef yön vektörü
        bakis.y = transform.position.y; // Y eksenini sabitle
        // Yumuþak dönüþ (Interpolation)
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(bakis), Time.fixedDeltaTime * 2.4f);
    }

    // Editör ekranýnda görselleþtirme (Gizmos)
    private void OnDrawGizmos()
    {
        // Sadece kovalama durumundayken saldýrý menzilini sarý küre ile göster
        if (state == State.Kovalama)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, saldiriMesafesi);
        }
    }
}