#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class AddTreeCollidersLOD0Only
{
    static readonly string[] IncludeTreeTokens = { "tree" }; // TreePine_VarB, SM_Env_Tree...
    static readonly string[] ExcludeTokens =
    {
        "bush", "leaf", "leaves", "foliage", "grass", "flower", "vine",
        "vfx", "fx", "particle", "probe", "volume", "trigger"
    };

    [MenuItem("Tools/Colliders/Add Tree MeshColliders (from LOD0 children)")]
    public static void AddTreeMeshCollidersFromLOD0Children()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("Chọn root (ví dụ Environment_Folliage) hoặc chọn vài Tree... trong Hierarchy trước.");
            return;
        }

        int added = 0, skipped = 0, lod0FoldersFound = 0;

        foreach (var root in roots)
        {
            // Duyệt mọi transform con, tìm folder tên chính xác "LOD0"
            var lod0Folders = root.GetComponentsInChildren<Transform>(true)
                .Where(t => string.Equals(t.name, "LOD0", System.StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var lod0 in lod0Folders)
            {
                lod0FoldersFound++;

                // Chỉ xử lý nếu parent chain có vẻ là tree (để tránh nhầm object khác cũng có LOD0)
                // kiểm tra trên tên của chính lod0 và cha của nó
                string contextName = (lod0.name + " " + (lod0.parent != null ? lod0.parent.name : "")).ToLowerInvariant();
                if (!IncludeTreeTokens.Any(tok => contextName.Contains(tok)))
                {
                    // Nếu bạn chọn đúng root cây thì vẫn có thể muốn bỏ check này.
                    // Nhưng để an toàn, mình skip nếu không thấy "tree" trong ngữ cảnh.
                    skipped++;
                    continue;
                }

                // Lấy tất cả MeshRenderer nằm BÊN TRONG folder LOD0
                var renderers = lod0.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    var go = mr.gameObject;

                    // Bỏ qua nếu tên/material/shader có token loại trừ
                    string n = go.name.ToLowerInvariant();
                    if (ExcludeTokens.Any(tok => n.Contains(tok)))
                    {
                        skipped++;
                        continue;
                    }

                    var mf = go.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                    {
                        skipped++;
                        continue;
                    }

                    // loại trừ theo material/shader
                    string matNames = string.Join(" ", mr.sharedMaterials.Where(m => m != null).Select(m => m.name.ToLowerInvariant()));
                    string shaderNames = string.Join(" ", mr.sharedMaterials.Where(m => m != null && m.shader != null).Select(m => m.shader.name.ToLowerInvariant()));
                    bool excludedByMat = ExcludeTokens.Any(tok => matNames.Contains(tok) || shaderNames.Contains(tok));

                    // transparent check (URP Lit _Surface=1 hoặc renderQueue>=3000)
                    bool isTransparent = mr.sharedMaterials.Any(m =>
                        m != null && (
                            (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) ||
                            (m.renderQueue >= 3000)
                        )
                    );

                    if (excludedByMat || isTransparent)
                    {
                        skipped++;
                        continue;
                    }

                    // Nếu đã có collider thì skip
                    if (go.GetComponent<Collider>() != null)
                    {
                        skipped++;
                        continue;
                    }

                    var mc = Undo.AddComponent<MeshCollider>(go);
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                    added++;
                }
            }
        }

        Debug.Log($"Tree LOD0 folders found: {lod0FoldersFound}. Added MeshCollider: {added}, Skipped: {skipped}");
        if (lod0FoldersFound == 0)
            Debug.LogWarning("Không tìm thấy folder tên 'LOD0' trong selection. Hãy chọn đúng root (Environment_Folliage hoặc TreePine_VarX).");
    }

    [MenuItem("Tools/Colliders/Remove Tree MeshColliders (Selection)")]
    public static void RemoveTreeMeshCollidersInSelection()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("Chọn root hoặc vài cây trước.");
            return;
        }

        int removed = 0;
        foreach (var root in roots)
        {
            var mcs = root.GetComponentsInChildren<MeshCollider>(true);
            foreach (var mc in mcs)
            {
                // chỉ remove collider trong các object liên quan tree (tên có "tree")
                string n = mc.gameObject.name.ToLowerInvariant();
                if (n.Contains("tree"))
                {
                    Undo.DestroyObjectImmediate(mc);
                    removed++;
                }
            }
        }

        Debug.Log($"Removed Tree MeshColliders: {removed}");
    }
}
#endif