using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Xesin.GameplayFramework;
using Xesin.GameplayFramework.Utils;

public class ViewportSubsystem : MonoSingleton<ViewportSubsystem>
{
    private Dictionary<LocalPlayer, UIViewport> playerViewports = new Dictionary<LocalPlayer, UIViewport>();
    private UIViewport screenViewport;

    private void Awake()
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

    public void AddToScreen(GameObject gameObject)
    {
        if (screenViewport != null)
        {
            screenViewport.AddWidget(gameObject);
        }
    }

    public void AddToScreen(PlayerController playerController, GameObject gameObject)
    {
        Assert.IsNotNull(playerController, "Tried to add ui to the screen with null player controller");

        LocalPlayer player = playerController.GetPlayer();

        Assert.IsNotNull(player, "Tried to add ui to the screen with no player attached to PlayerController");

        if (playerViewports.ContainsKey(player))
        {
            playerViewports[player].AddWidget(gameObject);
        }
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
