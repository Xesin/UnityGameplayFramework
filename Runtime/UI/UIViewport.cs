using UnityEngine;
using Xesin.GameplayFramework;

[RequireComponent(typeof(Canvas))]
public class UIViewport : MonoBehaviour
{
    protected Canvas canvas;
    public LocalPlayer Owner { get; private set; } 

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    public void Initialize(LocalPlayer owner = null)
    {
        Owner = owner;
        if (owner)
        {
            ViewportSubsystem.Instance.AddPlayerViewport(owner, this);
        }
        else
        {
            ViewportSubsystem.Instance.SetScreenViewport(this);
        }
    }

    public UIWidget AddWidget(UIWidget widget)
    {
        var newWidget = Instantiate(widget, canvas.GetComponent<RectTransform>());
        newWidget.Owner = Owner;

        return newWidget;
    }

    public UIWidget AddWidgetNoInstancing(UIWidget widget)
    {
        widget.transform.SetParent(canvas.GetComponent<RectTransform>());
        widget.Owner = Owner;

        return widget;
    }

    public void SetOutputCamera(Camera camera)
    {
        canvas.planeDistance = camera.nearClipPlane + 0.01f;
        canvas.worldCamera = camera;
    }
}
