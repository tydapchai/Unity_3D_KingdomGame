using UnityEngine;

namespace Unity.FantasyKingdom
{
    [CreateAssetMenu(fileName = "Item", menuName = "NewItem")]
    public class ItemSO : ScriptableObject
    {
        public string itemName;
        public Sprite icon;
        public int maxStackSize;
        public GameObject itemPrefab;
        public GameObject handItemPrefab;
    }
}
