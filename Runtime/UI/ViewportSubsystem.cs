using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Xesin.GameplayFramework;
using Xesin.GameplayFramework.Utils;

[AutoCreateSubsystem]
public class ViewportSubsystem : Xesin.GameplayFramework.Subsystem<ViewportSubsystem>
{
    private Dictionary<LocalPlayer, UIViewport> playerViewports = new Dictionary<LocalPlayer, UIViewport>();
    private UIViewport screenViewport;

    public override void OnRegistered()
    {
        SceneManager.sceneUnloaded += OnSceneUnLoaded;
    }

    public void AddPlayerViewport(LocalPlayer localPlayer, UIViewport uIViewport)
    {
        if (playerViewports.ContainsKey(localPlayer) && !playerViewports[localPlayer])
            playerViewports.Remove(localPlayer);
        playerViewports.Add(localPlayer, uIViewport);
    }

    public void RemovePlayerViewport(LocalPlayer localPlayer, UIViewport uIViewport)
    {
        if (!playerViewports.ContainsKey(localPlayer))
            return;
        playerViewports.Remove(localPlayer);
    }

    public UIViewport GetPlayerViewport(LocalPlayer localPlayer)
    {
        if (playerViewports.ContainsKey(localPlayer) && !playerViewports[localPlayer])
        {
            playerViewports.Remove(localPlayer);
            return null;
        }
        return playerViewports[localPlayer];
    }

    public void SetScreenViewport(UIViewport uIViewport)
    {
        screenViewport = uIViewport;
    }

    public UIViewport GetScreenViewport()
    {
        return screenViewport;
    }

    public T AddToScreen<T>(T gameObject, bool isPrefab = true) where T : UIWidget
    {
        if (screenViewport != null)
        {
            if (isPrefab)
                return screenViewport.AddWidget(gameObject);
            else
                return screenViewport.AddWidgetNoInstancing(gameObject);
        }

        return null;
    }

    public T AddToScreen<T>(PlayerController playerController, T gameObject, bool isPrefab = true) where T : UIWidget
    {
        Assert.IsNotNull(playerController, "Tried to add ui to the screen with null player controller");

        LocalPlayer player = playerController.GetPlayer();

        Assert.IsNotNull(player, "Tried to add ui to the screen with no player attached to PlayerController");

        if (playerViewports.ContainsKey(player))
        {
            if (isPrefab)
                return playerViewports[player].AddWidget(gameObject);
            else
                return playerViewports[player].AddWidgetNoInstancing(gameObject);
        }

        return null;
    }

    private void OnSceneUnLoaded(Scene scene)
    {
        List<LocalPlayer> unloadedViewports = new List<LocalPlayer>(playerViewports.Count);
        foreach (var viewport in playerViewports)
        {
            if (!viewport.Value)
                unloadedViewports.Add(viewport.Key);
        }

        for (int i = 0; i < unloadedViewports.Count; i++)
        {
            playerViewports.Remove(unloadedViewports[i]);
        }
    }
}
