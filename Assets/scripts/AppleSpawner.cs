using UnityEngine;

public class AppleSpawner : MonoBehaviour
{
    public GameObject applePrefab;
    public Vector2 spawnRangeX = new Vector2(-8f, 8f);
    public Vector2 spawnRangeZ = new Vector2(-8f, 8f);

    void Start()
    {
        SpawnApple();
    }

    public void SpawnApple()
    {
        Vector3 position = new Vector3(
            Random.Range(spawnRangeX.x, spawnRangeX.y),
            0.5f,
            Random.Range(spawnRangeZ.x, spawnRangeZ.y)
        );

        Instantiate(applePrefab, position, Quaternion.identity);
    }
}
