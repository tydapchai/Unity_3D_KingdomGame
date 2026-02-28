
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class AddMeshColliderLOD0Only
{
    // Loại trừ theo keyword (tên object/material/mesh/shader)
    static readonly string[] ExcludeKeywords =
    {
        "water", "river", "lake", "sea",
        "leaf", "leaves", "foliage", "grass", "flower", "plant", "tree", "bush", "vine",
        "vfx", "fx", "particle",
        "probe", "reflectionprobe", "lightprobe",
        "volume", "trigger"
    };

    [MenuItem("Tools/Colliders/Add MeshCollider for LOD0 Only (Selection)")]
    public static void AddMeshColliderForLOD0Only()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("Hãy chọn root object (ví dụ group Props) trong Scene trước.");
            return;
        }

        int added = 0, skipped = 0;

        foreach (var root in roots)
        {
            // Lấy tất cả object con có tên chứa "LOD0"
            var lod0Transforms = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.ToLowerInvariant().Contains("lod0"))
                .ToArray();

            foreach (var t in lod0Transforms)
            {
                var go = t.gameObject;

                // Nếu đã có collider thì bỏ qua
                if (go.GetComponent<Collider>() != null)
                {
                    skipped++;
                    continue;
                }

                // Phải có MeshFilter + mesh
                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();
                if (mf == null || mf.sharedMesh == null || mr == null)
                {
                    skipped++;
                    continue;
                }

                // Loại trừ theo keyword trong name/mesh/material/shader
                string objName = go.name.ToLowerInvariant();
                string meshName = mf.sharedMesh.name.ToLowerInvariant();
                string matNames = string.Join(" ", mr.sharedMaterials.Where(m => m != null).Select(m => m.name.ToLowerInvariant()));
                string shaderNames = string.Join(" ", mr.sharedMaterials.Where(m => m != null && m.shader != null).Select(m => m.shader.name.ToLowerInvariant()));

                bool keywordExcluded = ExcludeKeywords.Any(k =>
                    objName.Contains(k) || meshName.Contains(k) || matNames.Contains(k) || shaderNames.Contains(k));

                // Loại trừ material trong suốt (URP Lit: _Surface 1 = Transparent) hoặc renderQueue >= 3000
                bool isTransparent = mr.sharedMaterials.Any(m =>
                    m != null && (
                        (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) ||
                        (m.renderQueue >= 3000)
                    )
                );

                if (keywordExcluded || isTransparent)
                {
                    skipped++;
                    continue;
                }

                // Add MeshCollider
                var mc = Undo.AddComponent<MeshCollider>(go);
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false; // static environment collider
                added++;
            }
        }

        Debug.Log($"Done. Added MeshCollider (LOD0 only): {added}, Skipped: {skipped}");
    }

    [MenuItem("Tools/Colliders/Remove MeshColliders In Selection")]
    public static void RemoveMeshCollidersInSelection()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("Hãy chọn root object trong Scene trước.");
            return;
        }

        int removed = 0;
        foreach (var root in roots)
        {
            var colliders = root.GetComponentsInChildren<MeshCollider>(true);
            foreach (var c in colliders)
            {
                Undo.DestroyObjectImmediate(c);
                removed++;
            }
        }

        Debug.Log($"Removed MeshColliders: {removed}");
    }
}
