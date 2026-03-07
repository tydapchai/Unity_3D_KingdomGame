using UnityEngine;

public class MinimapWorldObject : MonoBehaviour
{
    [SerializeField]
    private bool followObject = false;
    [SerializeField]
    private Sprite minimapIcon;
    public Sprite MinimapIcon => minimapIcon;

    private void Start()
    {
        if (MinimapController.Instance == null)
            return;

        MinimapController.Instance.RegisterMinimapWorldObject(this, followObject);
    }

    private void OnDestroy()
    {
        if (MinimapController.Instance == null)
            return;

        MinimapController.Instance.RemoveMinimapWorldObject(this);
    }
}
