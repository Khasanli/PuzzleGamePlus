using TMPro;
using UnityEngine;

namespace SimpleFPS
{
	/// <summary>
	/// Main gameplay info panel at the top of the screen.
	/// Also handles displaying of announcements (e.g. Lead Lost, Lead Taken)
	/// </summary>
	public class UIGameplayInfo : MonoBehaviour
	{
		[Header("Announcer")]
		public GameObject      GameplayStart;
		public GameObject      LeadTaken;
		public GameObject      LeadLost;

		private GameUI _gameUI;

		private int _lastPosition;

		private void Awake()
		{
			// Make sure to turn all announcements off on start.
			GameplayStart.SetActive(false);
			LeadTaken.SetActive(false);
			LeadLost.SetActive(false);

			_gameUI = GetComponentInParent<GameUI>();
		}

		private void Update()
		{
			if (_gameUI.Runner == null)
				return;

			var gameplay = _gameUI.Gameplay;

			if (gameplay.Object == null || gameplay.Object.IsValid == false)
				return;

			GameplayStart.SetActive(gameplay.State == EGameplayState.Running);
	
			if (gameplay.PlayerData.TryGet(_gameUI.Runner.LocalPlayer, out PlayerData playerData))
			{
				ShowPlayerData(playerData);
			}
		}

		private void ShowPlayerData(PlayerData playerData)
		{
			if (playerData.StatisticPosition == _lastPosition)
				return;

			if (_lastPosition == 1)
			{
				LeadLost.SetActive(false);
				LeadLost.SetActive(true);
			}

			_lastPosition = playerData.StatisticPosition;
		}
	}
}
