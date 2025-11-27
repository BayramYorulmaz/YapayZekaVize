using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class huysuzAI : MonoBehaviour
{
    // Yapay zekanýn sahip olabileceði davranýþ durumlarý (Finite State Machine)
    public enum State { Devriye, OnunuKesme, Kovalama, Kacma, TahminiYereGitme, Saldirma }

    State state; // Mevcut durumu tutan deðiþken

    [Header("Ayarlar")]
    public Collider devriyeYeri; // Yapay zekanýn devriye atabileceði sýnýrlarý belirleyen collider
    public GameObject karakter; // Takip edilecek veya saldýrýlacak hedef karakter
    public NavMeshAgent HuysuzAgent; // Hareket kontrolünü saðlayan NavMeshAgent bileþeni
    Vector3 GidilmekIstenilenYer; // NavMeshAgent'ýn hedef olarak belirleyeceði koordinat

    [Header("Hareket Deðerleri")]
    public float Hýz = 2.5f; // Yapay zekanýn hareket hýzý
    public float kacmaMesafesi = 4f; // Karakter bu mesafeye girerse yapay zeka kaçmaya baþlar
    public float kacmaMesafesi2 = 4f; // Kaçarken hedeften ne kadar uzaða gideceði
    public float saldiriMesafesi = 4f; // Bu mesafeye gelindiðinde saldýrý durumuna geçilir

    [Header("Durum Deðerleri")]
    public int Can = 100; // Yapay zekanýn saðlýk puaný
    public Animator DusmanSaldiriAnimator; // Saldýrý animasyonlarýný kontrol eden animatör
    public Slider canbari; // UI üzerinde caný gösteren slider
    public GameObject HuysuzGidilmekIstenilenYerObje; // Debug amaçlý hedef noktayý gösteren obje

    // Coroutine referanslarýný tutarak ayný rutinin üst üste binmesini engelliyoruz
    private Coroutine devriye = null;
    private Coroutine Kovalama = null;
    private Coroutine TahminiYereGitme = null;
    private Coroutine Saldirma = null;

    private void Awake()
    {
        state = State.Devriye; // Oyun baþlar baþlamaz varsayýlan olarak devriye durumuna geç
    }

    private void Start()
    {
        // Agent bileþeni varsa baþlangýç ayarlarýný yap
        if (HuysuzAgent != null)
        {
            HuysuzAgent.stoppingDistance = 0.1f; // Hedefe ne kadar kala duracaðýný ayarla
            HuysuzAgent.speed = Hýz; // Hareket hýzýný atanan deðiþkene eþitle
        }
    }

    private void FixedUpdate()
    {
        // Ölüm kontrolü
        if (Can <= 0)
        {
            Destroy(gameObject); // Can 0 veya altýndaysa objeyi yok et
        }

        // Can barýný güncelle
        canbari.value = Can;

        // Can kritik seviyenin (30) altýndaysa öncelikli durum Kaçma'dýr
        if (Can < 30)
        {
            state = State.Kacma;
        }
        else
        {
            // Eðer karakterin önünü kesmeye çalýþýyorsak ve yeterince yaklaþtýysak saldýrýya geç
            if (state == State.OnunuKesme)
            {
                // Mevcut konum ile karakter arasýndaki mesafeyi ölç (Y ekseni hariç)
                if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(karakter.transform.position.x, 0, karakter.transform.position.z)) <= saldiriMesafesi)
                {
                    state = State.Saldirma;
                }
            }
        }

        // Debug objesini gidilecek yere taþý (Görselleþtirme için)
        HuysuzGidilmekIstenilenYerObje.transform.position = GidilmekIstenilenYer;

        // Durum Makinesi (State Machine) Mantýðý
        switch (state)
        {
            case State.Devriye: // Rastgele noktalarda dolaþma durumu
                if (devriye == null)
                {
                    devriye = StartCoroutine(Devriye()); // Devriye rutini çalýþmýyorsa baþlat
                }
                GorusAlaniniTara(); // Devriye atarken etrafý gözle
                break;

            case State.OnunuKesme: // Karakteri gördüðü ve ona doðru yöneldiði durum
                // Eðer devriye rutini hala çalýþýyorsa durdur
                if (devriye != null)
                {
                    StopCoroutine(devriye);
                    devriye = null;
                }
                OnunuKesmeRutin(); // Karakterin konumuna git
                GorusAlaniniTara(); // Görüþ alanýný kontrol etmeye devam et
                break;

            case State.Kovalama: // Karakter görüþten çýktýktan sonra son görülen yere gitme/kovalama
                if (Kovalama == null)
                {
                    Kovalama = StartCoroutine(kovalamaRutin());
                }
                GorusAlaniniTara();
                break;

            case State.Kacma: // Can azaldýðýnda karakterden uzaklaþma
                KacmaRutin();
                break;

            case State.TahminiYereGitme: // Karakter tamamen kaybolduðunda son bilinen noktaya gitme
                if (TahminiYereGitme == null)
                {
                    TahminiYereGitme = StartCoroutine(TahmineGitmeRutin());
                }
                GorusAlaniniTara();
                break;

            case State.Saldirma: // Saldýrý mesafesine girildiðinde saldýrý yapma
                if (Saldirma == null)
                {
                    Saldirma = StartCoroutine(SaldirmaRutin());
                }
                break;
        }
    }

    // Dýþarýdan veya baþka scriptten hasar vermek için çaðrýlan fonksiyon
    public void hasarAl()
    {
        Debug.Log("Huysuz Hasar Aldý");
        Can -= 40; // Caný 40 birim azalt
    }

    // Saldýrý Mantýðý Coroutine
    public IEnumerator SaldirmaRutin()
    {
        float T = Time.time;
        GidilmekIstenilenYer = transform.position; // Saldýrýrken olduðu yerde durmasý için hedefi kendi konumu yap
        DusmanSaldiriAnimator.SetTrigger("saldiri"); // Animasyonu tetikle

        // 3 saniye boyunca saldýrý durumunda kal
        while (T + 3 >= Time.time)
        {
            karaktereBak(); // Sürekli karaktere dön
            HuysuzAgent.SetDestination(GidilmekIstenilenYer); // Hareket etme
            state = State.Saldirma; // Durumu saldýrý olarak zorla
            yield return null; // Bir sonraki kareye bekle
        }

        state = State.OnunuKesme; // Saldýrý bitince tekrar takip moduna geç
        yield return null;
        Saldirma = null; // Coroutine'i boþa çýkar
    }

    // Devriye Mantýðý Coroutine
    public IEnumerator Devriye()
    {
        HuysuzAgent.updateRotation = true; // Agent'ýn dönmesine izin ver
        Bounds b = devriyeYeri.bounds; // Devriye alanýnýn sýnýrlarýný al
        Vector3 picket = transform.position; // Gidilecek nokta
        bool found = false;

        // NavMesh üzerinde geçerli rastgele bir nokta bulmak için 10 deneme yap
        for (int i = 0; i < 10; i++)
        {
            // Alan içinde rastgele bir koordinat üret
            Vector3 rnd = new Vector3(Random.Range(b.min.x, b.max.x), b.center.y, Random.Range(b.min.z, b.max.z));

            if (HuysuzAgent != null)
            {
                // Üretilen nokta NavMesh üzerinde mi kontrol et
                if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    picket = hit.position; // Geçerli noktayý al
                    found = true;
                    break; // Döngüden çýk
                }
                else
                {
                    picket = rnd; // NavMesh bulunamazsa direkt random noktayý al (Riskli olabilir)
                    found = true;
                    break;
                }
            }
        }

        // Bulunan noktayý hedef olarak ayarla
        GidilmekIstenilenYer = found ? picket : b.center;
        GidilmekIstenilenYer.y = transform.position.y; // Yükseklik farkýný eþitle

        // Agent aktifse hedefi belirle
        if (HuysuzAgent != null && HuysuzAgent.enabled && HuysuzAgent.isOnNavMesh)
        {
            HuysuzAgent.SetDestination(GidilmekIstenilenYer);
        }

        float T = Time.time;
        bool bekle = false;

        // 10 saniye boyunca o noktaya gitmeye çalýþ
        while (T + 10 >= Time.time)
        {
            // Hedefe 0.2 birim kadar yaklaþýldý mý?
            if (Vector3.Distance(new Vector3(GidilmekIstenilenYer.x, 0, GidilmekIstenilenYer.z), new Vector3(transform.position.x, 0, transform.position.z)) <= 0.2f)
            {
                Debug.Log("Huysuz Devriye noktasýna geldi");
                bekle = true; // Hedefe vardý, bekleme moduna geç
                break;
            }
            yield return null;
        }

        // Hedefe vardýysa 2 saniye bekle
        if (bekle == true)
        {
            T = Time.time;
            while (T + 2 >= Time.time)
            {
                yield return null;
            }
        }

        devriye = null; // Rutini sýfýrla
        devriye = StartCoroutine(Devriye()); // Yeni bir devriye noktasý için tekrar baþlat (Rekürsif döngü)
    }

    // Karakteri Takip Etme / Önünü Kesme Fonksiyonu
    public void OnunuKesmeRutin()
    {
        karaktereBak(); // Karaktere dön
        // Karakterin hareket scriptinden hedef konumunu al (Tahminleme için kullanýlabilir)
        GidilmekIstenilenYer = karakter.GetComponent<karakter>().hareketKonum;
        GidilmekIstenilenYer.y = transform.position.y;
        HuysuzAgent.SetDestination(GidilmekIstenilenYer); // Hedefe git
    }

    // Kaçma Mantýðý
    public void KacmaRutin()
    {
        HuysuzAgent.updateRotation = true;
        // Eðer karakter çok yakýnsa kaçýþ yönü hesapla
        if (Vector3.Distance(transform.position, karakter.transform.position) <= kacmaMesafesi)
        {
            // Karakterden zýt yönde bir vektör hesapla
            GidilmekIstenilenYer = new Vector3(karakter.transform.position.x, 0, karakter.transform.position.z) - new Vector3(transform.position.x, 0, transform.position.z);
            // Vektörü ters çevir ve belirlenen mesafe kadar öteye git
            GidilmekIstenilenYer = (-GidilmekIstenilenYer.normalized * kacmaMesafesi2) + transform.position;
        }
        else
        {
            GidilmekIstenilenYer = transform.position; // Yeterince uzaksa dur
        }
        GidilmekIstenilenYer.y = transform.position.y;
        HuysuzAgent.SetDestination(GidilmekIstenilenYer);
    }

    // Görüþ Alaný Tarama (Raycasting)
    void GorusAlaniniTara()
    {
        int raySayisi = 20; // Atýlacak ýþýn sayýsý
        float toplamAci = 90f; // Görüþ açýsý geniþliði
        float menzil = 15f; // Görüþ mesafesi
        float baslangicAcisi = -toplamAci / 2; // Taramaya baþlanacak sol açý (-45 derece)
        float aciAdimi = toplamAci / (raySayisi - 1); // Her ýþýn arasýndaki açý farký

        for (int i = 0; i < raySayisi; i++)
        {
            float gecerliAci = baslangicAcisi + (aciAdimi * i);
            // Yapay zekanýn baktýðý yöne göre açýyý hesapla
            Vector3 yon = Quaternion.Euler(0, gecerliAci, 0) * transform.forward;
            Vector3 cikisNoktasi = transform.position; // Iþýnýn çýkýþ noktasý
            RaycastHit hit;

            // SphereCast ile hacimli bir ýþýn gönder (daha iyi çarpýþma tespiti için)
            if (Physics.SphereCast(cikisNoktasi, 0.5f, yon, out hit, menzil))
            {
                // Çarpan obje karakter mi?
                if (hit.collider.gameObject.GetComponent<karakter>())
                {
                    Debug.DrawRay(cikisNoktasi, yon * hit.distance, Color.black); // Görünce siyah çizgi çiz
                    Debug.Log("Huysuz oyuncuyu gördü");
                    state = State.OnunuKesme; // Durumu takip/ön kesme olarak deðiþtir
                    return; // Gördüyse döngüden çýk
                }
                else
                {
                    Debug.DrawRay(cikisNoktasi, yon * hit.distance, Color.red); // Engel gördüyse kýrmýzý çizgi
                }
            }
            else
            {
                Debug.DrawRay(cikisNoktasi, yon * menzil, Color.green); // Boþluksa yeþil çizgi
            }
        }

        // Eðer takip modundaysak ve raycast ile göremiyorsak kovalama (son konuma gitme) moduna geç
        if (state == State.OnunuKesme)
        {
            state = State.Kovalama;
        }
    }

    // Kovalama Rutini (Görüþ kaybolduðunda son konuma gitme)
    public IEnumerator kovalamaRutin()
    {
        float T = Time.time;
        HuysuzAgent.updateRotation = true;

        // 2 saniye boyunca karakterin son bilinen konumuna gitmeye çalýþ
        while (T + 2 >= Time.time)
        {
            GidilmekIstenilenYer = karakter.transform.position;
            GidilmekIstenilenYer.y = transform.position.y;
            HuysuzAgent.SetDestination(GidilmekIstenilenYer);
            yield return null;

            // Eðer bu sýrada tekrar görürse rutini kýr
            if (state == State.OnunuKesme)
            {
                Kovalama = null;
                break;
            }
        }

        // 2 saniye sonunda hala göremiyorsa "Tahmini Yere Gitme" durumuna geç
        state = State.TahminiYereGitme;
        Kovalama = null;
    }

    // Tahmini Yere Gitme Rutini (Aramayý sonlandýrma aþamasý)
    public IEnumerator TahmineGitmeRutin()
    {
        HuysuzAgent.updateRotation = true;

        // Belirlenen son hedef noktaya varana kadar git
        while (Vector3.Distance(new Vector3(GidilmekIstenilenYer.x, 0, GidilmekIstenilenYer.z), new Vector3(transform.position.x, 0, transform.position.z)) >= 0.2f)
        {
            HuysuzAgent.SetDestination(GidilmekIstenilenYer);
            yield return null;
        }

        // Oraya varýnca 2 saniye etrafa bakýn (bekle)
        float T = Time.time;
        while (T + 2 >= Time.time)
        {
            yield return null;
            // Beklerken karakteri görürse rutini kýr
            if (state == State.OnunuKesme)
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

        // Hiçbir þey bulamazsa devriye moduna geri dön
        state = State.Devriye;
    }

    // Yapay zekanýn yüzünü karaktere döndürme iþlemi
    public void karaktereBak()
    {
        HuysuzAgent.updateRotation = false; // Agent'ýn otomatik dönüþünü kapat
        Vector3 bakis = karakter.gameObject.transform.position;
        bakis = bakis - transform.position; // Yön vektörünü bul
        bakis.y = transform.position.y; // Y eksenindeki eðilmeyi engelle
        // Yumuþak bir dönüþ (Interpolation) uygula
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(bakis), Time.fixedDeltaTime * 2.4f);
    }

    // Editör ekranýnda debug çizimlerini gösterme (Gizmos)
    private void OnDrawGizmos()
    {
        // Kaçma durumundaysak kaçma mesafesini kýrmýzý küre ile çiz
        if (state == State.Kacma)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, kacmaMesafesi);
        }
        // Takip durumundaysak saldýrý mesafesini sarý küre ile çiz
        if (state == State.OnunuKesme)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, saldiriMesafesi);
        }
    }
}