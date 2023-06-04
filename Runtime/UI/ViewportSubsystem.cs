using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Xesin.GameplayFramework;
using Xesin.GameplayFramework.Utils;

public class ViewportSubsystem : MonoSingleton<ViewportSubsystem>
{
    private Dictionary<LocalPlayer, UIViewport> playerViewports = new Dictionary<LocalPlayer, UIViewport>();
    private UIViewport screenViewport;

    public void AddPlayerViewport(LocalPlayer localPlayer, UIViewport uIViewport)
    {
        playerViewports.Add(localPlayer, uIViewport);
    }

    public void RemovePlayerViewport(LocalPlayer localPlayer, UIViewport uIViewport)
    {
        playerViewports.Remove(localPlayer);
    }

    public UIViewport GetPlayerViewport(LocalPlayer localPlayer)
    {
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
}
