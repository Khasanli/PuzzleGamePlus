using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SimpleFPS
{
	/// <summary>
	/// Main UI script that stores references to other elements (views).
	/// </summary>
	public class GameUI : MonoBehaviour
	{
		public Gameplay       Gameplay;
		[HideInInspector]
		public NetworkRunner  Runner;

		public UIPlayerView   PlayerView;
		public UIGameplayView GameplayView;
		public GameObject     MenuView;
		public UISettingsView SettingsView;
		public GameObject     DisconnectedView;

		// Called from NetworkEvents on NetworkRunner object
		public void OnRunnerShutdown(NetworkRunner runner, ShutdownReason reason)
		{
			SettingsView.gameObject.SetActive(false);
			MenuView.gameObject.SetActive(false);

			DisconnectedView.SetActive(true);
		}

		public void GoToMenu()
		{
			if (Runner != null)
			{
				Runner.Shutdown();
			}

			SceneManager.LoadScene("Startup");
		}

		private void Awake()
		{
			PlayerView.gameObject.SetActive(false);
			MenuView.SetActive(false);
			SettingsView.gameObject.SetActive(false);
			DisconnectedView.SetActive(false);

			SettingsView.LoadSettings();

			// Make sure the cursor starts unlocked
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void Update()
		{
			if (Application.isBatchMode == true)
				return;

			if (Gameplay.Object == null || Gameplay.Object.IsValid == false)
				return;

			Runner = Gameplay.Runner;

			var keyboard = Keyboard.current;
			bool gameplayActive = Gameplay.State < EGameplayState.Finished;

			if (gameplayActive && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
			{
				MenuView.SetActive(!MenuView.activeSelf);
			}

			GameplayView.gameObject.SetActive(gameplayActive);

			var playerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
			if (playerObject != null)
			{
				var player = playerObject.GetComponent<Player>();
				var playerData = Gameplay.PlayerData.Get(Runner.LocalPlayer);

				PlayerView.UpdatePlayer(player, playerData);
				PlayerView.gameObject.SetActive(gameplayActive);
			}
			else
			{
				PlayerView.gameObject.SetActive(false);
			}
		}
	}
}
