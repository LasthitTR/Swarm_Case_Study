using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; 
using TMPro; 
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class SwarmManager : MonoBehaviour
{
    [Header("Sürü Araçları")]
    public List<UGVController> groundVehicles;
    private bool isMissionActive = false;

    [Header("Kamera Kontrolü")]
    public GameObject droneCamera;
    public GameObject actionCamera;

    [Header("Arayüz ve Metrikler")]
    public TextMeshProUGUI missionLogText;
    private float missionTimer = 0f;
    private bool isSearching = true;

    void Update()
    {
        // Sistem drone taramasını yaparken süreyi arka planda sayar
        if (isSearching)
        {
            missionTimer += Time.deltaTime;
        }

        // --- YENİ INPUT SİSTEMİ İLE KONTROLLER ---
        if (Keyboard.current != null)
        {
            // R Tuşu: Sahneyi ve görevi tamamen sıfırla (Reset)
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            // 1 ve 2 Tuşları: Oyuncunun kameralar arası manuel geçiş yapabilmesi
            if (Keyboard.current.digit1Key.wasPressedThisFrame && droneCamera != null)
            {
                droneCamera.SetActive(true);
                actionCamera.SetActive(false);
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame && actionCamera != null)
            {
                droneCamera.SetActive(false);
                actionCamera.SetActive(true);
            }
        }
    }

    public void OnTargetFound(Transform foundTarget)
    {
        if (isMissionActive) return;

        isMissionActive = true;
        isSearching = false; // Süreyi durdur

        Debug.Log("Swarm Merkezi: Kayıp araç tespit edildi! Koordinatlar işleniyor...");

        // Jürinin göreceği HUD paneline ilk logları düşüyoruz
        if (missionLogText != null)
        {
            missionLogText.text = $"[SİSTEM] Otonom Arama Başlatıldı...\n";
            missionLogText.text += $"[HEDEF TESPİT] Kayıp obje bulundu!\n";
            missionLogText.text += $"> Tarama Süresi: {missionTimer:F2} saniye\n";
        }

        // Kamyonun tepesinde dramatik kırmızı spot ışığını yak
        HighlightTarget(foundTarget);

        // Sinematik ve operasyonel akışı başlat
        StartCoroutine(EngageRescueSequence(foundTarget));
    }

    private IEnumerator EngageRescueSequence(Transform target)
    {
        // 1. Kamera geçişi için 4 saniye bekle
        yield return new WaitForSeconds(4f);

        if (droneCamera != null && actionCamera != null)
        {
            droneCamera.SetActive(false);
            actionCamera.SetActive(true);
        }

        if (missionLogText != null) missionLogText.text += "\n[MÜZAYEDE] Contract Net Protocol devrede. Araçlar teklif veriyor...\n";

        // GÖRSEL ŞOV: Araçlardan hedefe sarı lazer çizgileri (teklifler) çekilsin
        if (groundVehicles != null)
        {
            foreach (var ugv in groundVehicles)
            {
                if (ugv != null) ugv.DrawBidLine(target.position);
            }
        }

        // 2. İletişim ve teklif değerlendirme simülasyonu için 1 saniye bekle
        yield return new WaitForSeconds(1f);

        UGVController winningUGV = null;
        float lowestCost = Mathf.Infinity;
        string auctionLog = "[Auction Details]\n";

        // 3. Müzayede (Auction) Süreci - Maliyet Hesaplama
        if (groundVehicles != null)
        {
            foreach (var ugv in groundVehicles)
            {
                if (ugv != null)
                {
                    float cost = ugv.CalculatePathCost(target.position);
                    auctionLog += $"- {ugv.gameObject.name} teklifi: {cost:F2} (PathStatus: Kontrol Edildi)\n";

                    if (cost < lowestCost)
                    {
                        lowestCost = cost;
                        winningUGV = ugv;
                    }
                }
            }
        }

        // 4. Görev Ataması ve ROL DAĞILIMI
        if (winningUGV != null && lowestCost != Mathf.Infinity)
        {
            auctionLog += $">>> BİRİNCİL MÜDAHALE: {winningUGV.gameObject.name} (Maliyet: {lowestCost:F2}).\n";
            winningUGV.targetNode = target;

            foreach (var ugv in groundVehicles)
            {
                if (ugv != null)
                {
                    // Kazanan aracı yeşil çizgiyle onayla, kaybedenlerin sarı çizgisini kapat
                    ugv.SetWinnerLine(ugv == winningUGV);

                    // Kaybeden aracı bul ve Çevre Güvenliği rolünü ata
                    if (ugv != winningUGV)
                    {
                        auctionLog += $">>> ÇEVRE GÜVENLİĞİ: {ugv.gameObject.name} kordon noktasına geçiyor.\n";

                        // Hedefin çaprazında 12 birimlik güvenlik ofseti
                        Vector3 rawSupportPosition = target.position + new Vector3(12f, 0f, 12f);
                        Vector3 safePosition = rawSupportPosition;

                        // NavMesh Güvenliği: Eğer bu ofset bir binanın içindeyse, maksimum 20 birim yarıçapta en yakın yola (NavMesh'e) yapıştır
                        NavMeshHit hit;
                        if (UnityEngine.AI.NavMesh.SamplePosition(rawSupportPosition, out hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            safePosition = hit.position;
                        }

                        // Dinamik kordon objesi yarat ve aracı oraya yolla
                        GameObject perimeterTarget = new GameObject(ugv.gameObject.name + "_PerimeterTarget");
                        perimeterTarget.transform.position = safePosition;
                        ugv.targetNode = perimeterTarget.transform;
                    }
                }
            }

            Debug.Log(auctionLog);
            if (missionLogText != null)
            {
                missionLogText.text += $"\n[GÖREV ATAMASI BAŞARILI]\n";
                missionLogText.text += $"> Birincil Müdahale: {winningUGV.gameObject.name}\n";
                missionLogText.text += $"> Çevre Güvenliği: Destek Ekibi";
            }
        }
        else
        {
            Debug.LogWarning("Swarm Merkezi: UYARI! Hiçbir araç hedefe giden geçerli bir yol bulamadı.");
            if (missionLogText != null) missionLogText.text += "\n[UYARI] Hedefe ulaşılamıyor. Operasyon iptal!";
        }
    }

    /// <summary>
    /// Hedef bulunduğunda üzerine dikkat çekici kırmızı bir Spot ışığı ekler.
    /// </summary>
    private void HighlightTarget(Transform target)
    {
        GameObject highlight = new GameObject("TargetHighlightLight");
        highlight.transform.position = target.position + Vector3.up * 5f;
        highlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Light spotLight = highlight.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = Color.red;
        spotLight.intensity = 15f;
        spotLight.range = 20f;
        spotLight.spotAngle = 45f;
    }
}