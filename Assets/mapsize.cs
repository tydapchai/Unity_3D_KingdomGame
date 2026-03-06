using UnityEngine;

public class mapsize : MonoBehaviour
{
    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        Vector3 size = terrain.terrainData.size;

        Debug.Log("Width (X): " + size.x);
        Debug.Log("Height (Y): " + size.y); // Chiều cao tối đa có thể vẽ
        Debug.Log("Length (Z): " + size.z);
    }
}