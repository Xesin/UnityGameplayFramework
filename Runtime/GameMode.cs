using System.Collections;
using UnityEngine;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayFramework
{

    public class GameMode : MonoSingleton<GameMode>
    {
        [field: SerializeField]
        public Pawn pawnPrefab { get; private set; }

        [field: SerializeField]
        public PlayerController playerControllerPawn { get; private set; }

        [field: SerializeField]
        public ScreenViewport screenViewport { get; private set; }

        [field: SerializeField]
        public UIViewport playerViewport { get; private set; }

        private bool isPaused = false;

        private PlayerStart[] playerStarts;
        private int lastPlayerStartIndex = -1;

        public virtual IEnumerator OnLevelReady()
        {
            Instantiate(screenViewport);
            CreateInitialPlayerControllers();
            CreateInitialPlayerPawns();

            yield return null;
        }

        public virtual PlayerController CreatePlayerController(LocalPlayer localPlayer)
        {
            var player = Instantiate(playerControllerPawn);

            var pc = player.GetComponent<PlayerController>();

            pc.GetInputComponent().SetDevices(localPlayer.Devices);

            localPlayer.SetPlayerController(pc);
            pc.SetPlayer(localPlayer);

            var viewport = Instantiate(playerViewport);
            viewport.Initialize(pc);
            localPlayer.SetPlayerViewport(viewport);

            return pc;
        }

        protected virtual Pawn CreatePlayerPawn(PlayerController playerController)
        {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnOrientation = Quaternion.identity;

            var spawnPoint = GetSpawnPoint();

            if(spawnPoint)
            {
                spawnPosition = spawnPoint.position;
                spawnOrientation = spawnPoint.rotation;
            }

            var pawn = Instantiate(pawnPrefab, spawnPosition, spawnOrientation);

            playerController.Posses(pawn);
            return pawn;
        }

        protected virtual void CreateInitialPlayerControllers()
        {
            int numPlayers = LocalPlayer.GetNumPlayers();

            for (int i = 0; i < numPlayers; i++)
            {
                LocalPlayer localPlayer = LocalPlayer.GetLocalPlayer(i);
                CreatePlayerController(localPlayer);
            }
        }

        protected virtual void CreateInitialPlayerPawns()
        {
            int numPlayers = PlayerController.GetNumPlayerControllers();

            for (int i = 0; i < numPlayers; i++)
            {
                CreatePlayerPawn(PlayerController.GetPlayerController(i));
            }
        }

        public virtual void OnNewPlayerAdded(LocalPlayer localPlayer)
        {
            var pc = CreatePlayerController(localPlayer);
            CreatePlayerPawn(pc);
        }

        public virtual void TogglePause()
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else
            {
                UnpauseGame();
            }
        }

        public virtual void PauseGame()
        {
            isPaused = true;
            int numPlayers = PlayerController.GetNumPlayerControllers();

            for (int i = 0; i < numPlayers; i++)
            {
                PlayerController.GetPlayerController(i).GetInputComponent().DisableInput();
            }
        }

        public virtual void UnpauseGame()
        {
            isPaused = false;
            int numPlayers = PlayerController.GetNumPlayerControllers();

            for (int i = 0; i < numPlayers; i++)
            {
                PlayerController.GetPlayerController(i).GetInputComponent().ActivateInput();
            }
        }

        public virtual Transform GetSpawnPoint()
        {
            if(playerStarts == null) playerStarts = FindObjectsOfType<PlayerStart>();
            if (playerStarts.Length == 0) return null;


            lastPlayerStartIndex = (lastPlayerStartIndex + 1) % playerStarts.Length;

            return playerStarts[lastPlayerStartIndex].transform;
        }
    }
}