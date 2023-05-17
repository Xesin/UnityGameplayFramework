using GameplayFramework;
using GameplayFramework.Input;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InitScript : MonoBehaviour
{
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return GameMode.Instance.OnLevelReady();

        yield return new WaitForSeconds(1);

        //InputManager.Instance.CreatePlayer(InputUser.GetUnpairedInputDevices().Where(device =>
        //{
        //    var type = device.name.ToLower();
        //    return type.Contains("gamepad") || type.Contains("xinput") || type.Contains("xbox");
        //}).ElementAt(0));
    }

    private void Update()
    {
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            ActivateGameplay();
        }


        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            ActivateUI();
        }
    }

    void ActivateUI()
    {
        PlayerController.GetPlayerController(0).SetInputUIOnly();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ActivateGameplay()
    {
        PlayerController.GetPlayerController(0).SetInputGameplayOnly();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
