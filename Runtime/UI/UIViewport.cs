using UnityEngine;
using Xesin.GameplayFramework;

[RequireComponent(typeof(Canvas))]
public class UIViewport : MonoBehaviour
{
    protected Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    public void Initialize(PlayerController owner = null)
    {
        if (owner)
        {
            ViewportSubsystem.Instance.AddPlayerViewport(owner.GetPlayer(), this);
        }
        else
        {
            ViewportSubsystem.Instance.SetScreenViewport(this);
        }
    }

    public void AddWidget(GameObject gameObject)
    {
        Instantiate(gameObject, canvas.GetComponent<RectTransform>(), false);
    }

    public void SetOutputCamera(Camera camera)
    {
        canvas.planeDistance = camera.nearClipPlane + 0.01f;
        canvas.worldCamera = camera;
    }
}
