using UnityEngine;
using System.Collections.Generic;

public class HandkerchiefSpawner : MonoBehaviour
{
    [Header("Prefabs disponibles (stickman_1 ... stickman_9)")]
    public GameObject[] allPrefabs;

    [Header("Prefabs especiales")]
    public GameObject centerPrefab;

    [Header("Prefab del pañuelo")]
    public GameObject handkerchiefPrefab; // arrastra tu pañuelo aquí

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

    // Posibles nombres de hueso para la mano izquierda
    private readonly string[] possibleHandNames =
    {
        "hand.l",
        "LeftHand",
        "mixamorig:LeftHand",
        "Bip001 L Hand",
        "Armature_hand_l"
    };

    private Transform handL;
    private GameObject hk;

    void Start()
    {
        Debug.Log("🚀 HandkerchiefSpawner START ejecutado");

        if (allPrefabs.Length < playersPerTeam * 2)
        {
            Debug.LogError("❌ No hay suficientes prefabs para llenar los equipos!");
            return;
        }

        if (centerPrefab == null)
        {
            Debug.LogError("❌ No asignaste el prefab central en el inspector!");
            return;
        }

        if (handkerchiefPrefab == null)
        {
            Debug.LogError("❌ No asignaste el prefab del PAÑUELO en el inspector!");
            return;
        }

        List<GameObject> available = new List<GameObject>(allPrefabs);

        SpawnTeam(teamAPosition, available, "Team A");
        SpawnTeam(teamBPosition, available, "Team B");

        GameObject center = Instantiate(centerPrefab, centerPosition, Quaternion.identity);
        center.transform.localScale = Vector3.one * scale;

        // 🟡 aplicar borde al centro
        ApplyOutline(center);

        Debug.Log($"✅ Centro instanciado en {centerPosition}");

        Vector3 dirToA = (teamAPosition - centerPosition).normalized;
        dirToA.y = 0;
        Quaternion rot = Quaternion.LookRotation(Vector3.Cross(dirToA, Vector3.up), Vector3.up);
        center.transform.rotation = rot;

        Debug.Log("🔍 Listando huesos del prefab central...");
        PrintAllChildren(center.transform);

        // 🔎 Buscar hueso de la mano
        handL = null;
        foreach (var name in possibleHandNames)
        {
            handL = FindDeepChild(center.transform, name);
            if (handL != null)
            {
                Debug.Log($"✅ Hueso encontrado: {handL.name} en {handL.position}");
                break;
            }
        }

        // 🎉 Instanciar pañuelo
        Vector3 hkPosition = new Vector3(-26.75f, 6.48f, -15.14f);
        Quaternion hkRotation = Quaternion.Euler(87.688f, 51.121f, -128.9f);

        hk = Instantiate(handkerchiefPrefab, hkPosition, hkRotation);

        // Escala fija
        hk.transform.localScale = new Vector3(15f, 15f, 15f);

        Debug.Log($"🎉 Pañuelo instanciado en {hk.transform.position} con rotación {hk.transform.rotation.eulerAngles} y escala {hk.transform.localScale}");
    }

    void SpawnTeam(Vector3 basePosition, List<GameObject> available, string teamName)
    {
        if (available.Count == 0) return;

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

            // 🟡 aplicar borde al jugador
            ApplyOutline(go);

            // ✅ Guardar en la lista correcta
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

    Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    void PrintAllChildren(Transform parent, string indent = "")
    {
        Debug.Log(indent + parent.name);
        foreach (Transform child in parent)
        {
            PrintAllChildren(child, indent + "  ");
        }
    }

    void OnDrawGizmos()
    {
        if (handL != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(handL.position, 0.2f);
        }
        if (hk != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(hk.transform.position, Vector3.one * 0.3f);
        }
    }
}
