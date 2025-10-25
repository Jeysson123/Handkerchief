using GoogleMobileAds.Api;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    private InterstitialAd interstitialAd;
    private string adUnitId = "TU_INTERSTITIAL_ID";

    private void Start()
    {
        MobileAds.Initialize(initStatus => {
            LoadInterstitial();
        });
    }

    private void LoadInterstitial()
    {
        // Nueva forma de crear AdRequest
        AdRequest request = new AdRequest();

        InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogWarning("Error al cargar interstitial: " + error);
                return;
            }

            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial cerrado");
                LoadInterstitial(); // recargar después de cerrar
            };
        });
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null)
            interstitialAd.Show();
        else
            Debug.LogWarning("Interstitial no listo");
    }
}
