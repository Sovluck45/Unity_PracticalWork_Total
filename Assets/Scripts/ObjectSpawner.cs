using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs;
    [SerializeField] int spawnCount = 12;
    [SerializeField] Vector3 areaMin = new Vector3(-8f, 0.5f, -8f);
    [SerializeField] Vector3 areaMax = new Vector3(8f, 0.5f, 8f);

    void Start()
    {
        SpawnObjects();
    }

    public void SpawnObjects()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("ObjectSpawner: prefabs not assigned");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Vector3 position = new Vector3(
                Random.Range(areaMin.x, areaMax.x),
                Random.Range(areaMin.y, areaMax.y),
                Random.Range(areaMin.z, areaMax.z));
            float scale = Random.Range(0.5f, 2f);
            Color color = new Color(Random.value, Random.value, Random.value);

            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            Transform instanceTransform = instance.transform;
            instanceTransform.localScale = Vector3.one * scale;

            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                material.color = color;
            }

            Debug.Log($"Spawned {instance.name} at {position}, scale {scale}, color {color}");
        }
    }
}
