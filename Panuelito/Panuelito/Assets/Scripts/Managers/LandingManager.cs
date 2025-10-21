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
    public float letterSpacing = 70f;
    public float letterSpeed = 700f;
    public float letterDelay = 0.08f;
    public float logoScreenHeightPercent = 0.8f;
    public float fontScreenHeightPercent = 0.12f;
    public float minFontScale = 0.6f;

    [Header("Logo waving")]
    public float waveAngle = 10f; // grados máximos de rotación
    public float waveSpeed = 2f;  // velocidad del ondeo

    private Canvas canvas;
    private Image backgroundImage;

    void Start()
    {
        if (logoImage == null)
        {
            Debug.LogError("Asigna logoImage en LandingManager");
            return;
        }

        // Obtener o crear Canvas
        canvas = logoImage.canvas;
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
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
            }
            logoImage.transform.SetParent(canvas.transform, false);
        }

        // Crear background
        CreateBackgroundEffect();

        // Ajustar tamaño del logo manteniendo proporción
        RectTransform logoRT = logoImage.GetComponent<RectTransform>();
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
        float logoHeight = canvasHeight * logoScreenHeightPercent;
        float ratio = logoImage.sprite.bounds.size.x / logoImage.sprite.bounds.size.y;
        float logoWidth = logoHeight * ratio;
        logoRT.sizeDelta = new Vector2(logoWidth, logoHeight);

        // Posicionar logo a la izquierda
        logoRT.anchorMin = new Vector2(0f, 0.5f);
        logoRT.anchorMax = new Vector2(0f, 0.5f);
        logoRT.pivot = new Vector2(0f, 0.5f);
        logoRT.anchoredPosition = new Vector2(50f, 0f);

        // Iniciar animaciones
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
        backgroundImage.color = new Color(0.3f, 0.5f, 0.9f, 1f); // azul claro inicial

        RectTransform bgRT = backgroundImage.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Asegurar que esté detrás de todo
        backgroundImage.transform.SetAsFirstSibling();
    }

    IEnumerator AnimateBackground()
    {
        Color startColor = new Color(0.3f, 0.5f, 0.9f, 1f); // Azul claro
        Color endColor = new Color(0.02f, 0.05f, 0.1f, 1f); // Azul oscuro casi negro

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
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float baseFontSize = canvasHeight * fontScreenHeightPercent;

        Vector2 startPos = logoRT.anchoredPosition + new Vector2(logoRT.sizeDelta.x + 20f, 0f);
        float availableWidth = canvasWidth - startPos.x - 40f;

        float maxLetterSpacing = availableWidth / (gameName.Length + 1);
        float adjustedLetterSpacing = Mathf.Min(letterSpacing, maxLetterSpacing);

        float totalTextWidth = adjustedLetterSpacing * gameName.Length;
        float generalScale = Mathf.Min(1f, availableWidth / totalTextWidth);
        baseFontSize *= generalScale;

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

            RectTransform rt = letterText.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);

            float fontSize = baseFontSize * Mathf.Lerp(1f, minFontScale, (float)i / (gameName.Length - 1));
            letterText.fontSize = fontSize;
            rt.sizeDelta = new Vector2(fontSize, fontSize);

            float xPos = (i + 1) * adjustedLetterSpacing;
            rt.anchoredPosition = startPos + new Vector2(xPos, 0f);
            rt.anchoredPosition -= new Vector2(logoRT.sizeDelta.x * 0.2f, 0f);

            StartCoroutine(MoveLetter(rt, startPos + new Vector2(xPos, 0f)));
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
        SceneManager.LoadScene(nextScene);
    }
}
