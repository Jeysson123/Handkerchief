using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HandkerchiefSpawner : MonoBehaviour
{
    [Header("Prefabs disponibles (stickman_1 ... stickman_9)")]
    public GameObject[] allPrefabs;

    [Header("Prefabs especiales")]
    public GameObject centerPrefab;

    [Header("Prefab del pañuelo")]
    public GameObject handkerchiefPrefab;

    [Header("Posiciones base")]
    public Vector3 teamAPosition = new Vector3(0f, 0.05f, -10f);
    public Vector3 teamBPosition = new Vector3(0f, 0.05f, 10f);
    public Vector3 centerPosition = new Vector3(0f, 0.05f, 0f);

    [Header("Configuración")]
    public int playersPerTeam = 3;
    public float spacingReduction = 0f;
    public float scale = 3f;
    public float facingOffsetY = 0f;

    [Header("Outline")]
    public Material outlineMaterial;

    [Header("Listas de jugadores instanciados")]
    public List<GameObject> teamAPlayers = new List<GameObject>();
    public List<GameObject> teamBPlayers = new List<GameObject>();

    [Header("Cámara")]
    public Camera mainCamera;
    private Vector3 cameraStartPos;
    private Quaternion cameraStartRot;

    [Header("Botones de selección")]
    public Button[] selectionButtons; // asigna tus botones de selección aquí

    [Header("Barra de velocidad")]
    public Slider speedSlider;
    private float originalSpeedValue = 1f;

    private GameObject hk;
    public GameObject Handkerchief => hk;

    public Vector3 OriginalHandkerchiefPos { get; private set; }
    private PlayerMovement playerMovement;
    private DialogAndEffectsManager dialogAndEffectsManager;

    void Start()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        dialogAndEffectsManager = FindObjectOfType<DialogAndEffectsManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraStartPos = mainCamera.transform.position;
            cameraStartRot = mainCamera.transform.rotation;
        }

        if (speedSlider != null)
            originalSpeedValue = speedSlider.value;

        SpawnAll();
    }

    public void SpawnAll()
    {
        dialogAndEffectsManager.StartShowNumber(); //show number

        if (allPrefabs == null || centerPrefab == null || handkerchiefPrefab == null) return;
        if (allPrefabs.Length < playersPerTeam * 2) return;

        List<GameObject> available = new List<GameObject>(allPrefabs);

        SpawnTeam(teamAPosition, available, "Team A");
        SpawnTeam(teamBPosition, available, "Team B");

        GameObject center = Instantiate(centerPrefab, centerPosition, Quaternion.identity);
        center.transform.localScale = Vector3.one * scale;

        Vector3 dirToA = (teamAPosition - centerPosition).normalized;
        dirToA.y = 0;
        Quaternion rot = Quaternion.LookRotation(Vector3.Cross(dirToA, Vector3.up), Vector3.up);
        center.transform.rotation = rot;

        // Instanciar pañuelo
        Vector3 hkPosition = new Vector3(-26.75f, 6.48f, -15.14f);
        Quaternion hkRotation = Quaternion.Euler(87.688f, 51.121f, -128.9f);

        hk = Instantiate(handkerchiefPrefab, hkPosition, hkRotation);
        hk.transform.localScale = new Vector3(15f, 15f, 15f);
        OriginalHandkerchiefPos = hk.transform.position;

        // Restaurar cámara
        if (mainCamera != null)
        {
            mainCamera.transform.position = cameraStartPos;
            mainCamera.transform.rotation = cameraStartRot;
        }

        // Restaurar barra de velocidad
        if (speedSlider != null)
            speedSlider.value = originalSpeedValue;

        // Habilitar botones de selección
        EnableSelectionButtons(true);

        playerMovement.currentSpeed = 0f;
    }

    void SpawnTeam(Vector3 basePosition, List<GameObject> available, string teamName)
    {
        if (available == null || available.Count == 0) return;

        GameObject temp = Instantiate(available[0]);
        temp.transform.localScale = Vector3.one * scale;

        Renderer[] rends = temp.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(temp.transform.position, Vector3.zero);
        if (rends.Length > 0)
        {
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        }
        float prefabWidth = b.size.x;
        Destroy(temp);

        float totalSpacing = prefabWidth - spacingReduction;

        for (int i = 0; i < playersPerTeam; i++)
        {
            if (available.Count == 0) break;

            int index = Random.Range(0, available.Count);
            float xOffset = (i - (playersPerTeam - 1) / 2f) * totalSpacing;
            Vector3 pos = basePosition + new Vector3(xOffset, 0, 0);

            Vector3 dirToCenter = centerPosition - pos;
            dirToCenter.y = 0f;
            if (dirToCenter.sqrMagnitude < 0.0001f) dirToCenter = Vector3.forward;
            Quaternion rot = Quaternion.LookRotation(dirToCenter.normalized, Vector3.up);
            if (Mathf.Abs(facingOffsetY) > 0.001f) rot *= Quaternion.Euler(0f, facingOffsetY, 0f);

            GameObject go = Instantiate(available[index], pos, rot);
            go.transform.localScale = Vector3.one * scale;

            ApplyOutline(go);

            if (teamName == "Team A")
                teamAPlayers.Add(go);
            else
                teamBPlayers.Add(go);

            available.RemoveAt(index);
        }
    }

    void ApplyOutline(GameObject go)
    {
        if (outlineMaterial == null) return;

        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = mr.materials;
            System.Array.Resize(ref mats, mats.Length + 1);
            mats[mats.Length - 1] = outlineMaterial;
            mr.materials = mats;
        }

        foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mats = smr.materials;
            System.Array.Resize(ref mats, mats.Length + 1);
            mats[mats.Length - 1] = outlineMaterial;
            smr.materials = mats;
        }
    }

    private void EnableSelectionButtons(bool enabled)
    {
        if (selectionButtons == null || selectionButtons.Length == 0) return;
        foreach (var btn in selectionButtons)
        {
            if (btn != null)
                btn.interactable = enabled;
        }
    }
}
