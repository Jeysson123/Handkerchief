using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class LandingManager : MonoBehaviour
{
    [Header("Nombre de la siguiente escena")]
    public string nextScene = "MenuScene";

    [Header("Tiempo de duración del landing")]
    public float duration = 4f;

    [Header("Logo de la escena (Image)")]
    public Image logoImage;

    [Header("Texto a animar")]
    public string gameName = "Handkerchief";

    [Header("Configuración de animación")]
    public float letterSpeed = 700f;
    public float letterDelay = 0.08f;
    public float logoScreenHeightPercent = 0.1f;
    public float fontScreenHeightPercent = 0.12f;
    public float minFontScale = 0.6f;

    [Header("Logo waving")]
    public float waveAngle = 10f; // grados máximos de rotación
    public float waveSpeed = 2f;  // velocidad del ondeo

    private Canvas canvas;
    private Image backgroundImage;

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        if (logoImage == null) return;

        // Obtener o crear Canvas
        canvas = logoImage.canvas ?? FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            logoImage.transform.SetParent(canvas.transform, false);
        }

        CreateBackgroundEffect();
        SetupLogo();

        StartCoroutine(AnimateTextFromLogo());
        StartCoroutine(AnimateBackground());
        StartCoroutine(WaveLogo());
        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private void CreateBackgroundEffect()
    {
        GameObject bgGO = new GameObject("LandingBackground");
        bgGO.transform.SetParent(canvas.transform, false);

        backgroundImage = bgGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.3f, 0.5f, 0.9f, 1f);

        RectTransform bgRT = backgroundImage.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        backgroundImage.transform.SetAsFirstSibling();
    }

    private void SetupLogo()
    {
        RectTransform logoRT = logoImage.GetComponent<RectTransform>();
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
        float logoHeight = canvasHeight * logoScreenHeightPercent;
        float ratio = logoImage.sprite.bounds.size.x / logoImage.sprite.bounds.size.y;
        float logoWidth = logoHeight * ratio;
        logoRT.sizeDelta = new Vector2(logoWidth, logoHeight);

        logoRT.anchorMin = new Vector2(0f, 0.5f);
        logoRT.anchorMax = new Vector2(0f, 0.5f);
        logoRT.pivot = new Vector2(0f, 0.5f);
        logoRT.anchoredPosition = new Vector2(50f, 0f);
    }

    IEnumerator AnimateBackground()
    {
        Color startColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        Color endColor = new Color(0.02f, 0.05f, 0.1f, 1f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            backgroundImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        backgroundImage.color = endColor;
    }

    IEnumerator AnimateTextFromLogo()
    {
        RectTransform logoRT = logoImage.rectTransform;
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
        float baseFontSize = canvasHeight * fontScreenHeightPercent;

        // Medir letras
        float[] letterWidths = new float[gameName.Length];
        float totalWidth = 0f;
        for (int i = 0; i < gameName.Length; i++)
        {
            GameObject tempGO = new GameObject();
            TextMeshProUGUI tmp = tempGO.AddComponent<TextMeshProUGUI>();
            tmp.text = gameName[i].ToString();
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = baseFontSize * minFontScale;
            tmp.fontSizeMax = baseFontSize;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.enableWordWrapping = false;
            Vector2 pref = tmp.GetPreferredValues();
            letterWidths[i] = pref.x;
            totalWidth += pref.x;
            Destroy(tempGO);
        }

        // Escalar si excede canvas
        float availableWidth = canvasWidth - 40f - logoRT.anchoredPosition.x;
        float scaleFactor = totalWidth > availableWidth ? availableWidth / totalWidth : 1f;

        // Ajustar posición inicial más cerca del logo
        float currentX = logoRT.anchoredPosition.x + logoRT.sizeDelta.x - 500f; // Cambiado de 20f a 5f
        float extraOffset = (availableWidth - totalWidth * scaleFactor) / 2f;
        currentX += extraOffset;

        for (int i = 0; i < gameName.Length; i++)
        {
            char c = gameName[i];
            GameObject letterGO = new GameObject("Letter_" + c);
            letterGO.transform.SetParent(canvas.transform, false);

            TextMeshProUGUI letterText = letterGO.AddComponent<TextMeshProUGUI>();
            letterText.text = c.ToString();
            letterText.color = Color.white;
            letterText.alignment = TextAlignmentOptions.Center;
            letterText.enableWordWrapping = false;
            letterText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

            // AutoSize con factor de escala
            letterText.enableAutoSizing = true;
            letterText.fontSizeMin = baseFontSize * minFontScale * scaleFactor;
            letterText.fontSizeMax = baseFontSize * scaleFactor;

            RectTransform rt = letterText.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);

            Vector2 targetPos = new Vector2(currentX, logoRT.anchoredPosition.y);
            rt.anchoredPosition = logoRT.anchoredPosition; // Start desde logo
            StartCoroutine(MoveLetter(rt, targetPos));

            currentX += letterWidths[i] * scaleFactor;
            yield return new WaitForSeconds(letterDelay);
        }
    }

    IEnumerator MoveLetter(RectTransform rt, Vector3 target)
    {
        Vector3 startPos = rt.anchoredPosition;
        Vector3 endPos = target;
        float t = 0f;

        while (Vector3.Distance(rt.anchoredPosition, endPos) > 0.1f)
        {
            t += Time.deltaTime * letterSpeed / 100f;
            rt.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        rt.anchoredPosition = endPos;
    }

    IEnumerator WaveLogo()
    {
        RectTransform logoRT = logoImage.rectTransform;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * waveSpeed * Mathf.PI * 2f / duration) * waveAngle;
            logoRT.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        logoRT.localRotation = Quaternion.identity;
    }

    IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(duration);

        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null) Destroy(eventSystem.gameObject);

        SceneManager.LoadScene(nextScene);
    }
}