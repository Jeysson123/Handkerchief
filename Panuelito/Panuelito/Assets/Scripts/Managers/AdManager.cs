using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    private static AdManager instance;
    private InterstitialAd interstitial;

    // Tu Ad Unit ID real
    private string adUnitId = "ca-app-pub-3940256099942544/1033173712";//"ca-app-pub-9411528693526438/6036546495"; // Interstitial Ad Unit ID

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar AdMob
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob inicializado correctamente");
            LoadInterstitial();
        });
    }

    private void LoadInterstitial()
    {
        InterstitialAd.Load(adUnitId, null, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Error cargando interstitial: " + error);
                return;
            }

            interstitial = ad;
            Debug.Log("Interstitial cargado correctamente");

            interstitial.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Interstitial abierto");
            };

            interstitial.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial cerrado, cargando uno nuevo");
                LoadInterstitial();
            };

            interstitial.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                Debug.LogError("Interstitial falló: " + adError);
            };
        });
    }

    public void ShowInterstitial()
    {
        if (interstitial != null)
        {
            interstitial.Show();
        }
        else
        {
            Debug.Log("Interstitial aún no cargado, cargando uno nuevo...");
            LoadInterstitial();
        }
    }

    private void OnDestroy()
    {
        if (interstitial != null)
        {
            interstitial.Destroy();
        }
    }
}
