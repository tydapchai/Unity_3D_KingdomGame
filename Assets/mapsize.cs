using UnityEngine;

public class mapsize : MonoBehaviour
{
    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning($"No Terrain found for {name}.", this);
            return;
        }

        Vector3 size = terrain.terrainData.size;

        Debug.Log("Width (X): " + size.x);
        Debug.Log("Height (Y): " + size.y); // Chiều cao tối đa có thể vẽ
        Debug.Log("Length (Z): " + size.z);
    }
}