using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Cinemachine;

namespace SimpleFPS
{
	/// <summary>
	/// Main player script which handles input processing, visuals.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public class Player : NetworkBehaviour
	{
		[Header("Components")]
		public SimpleKCC     KCC;
		public Animator      Animator;

		[Header("Setup")]
		public float         MoveSpeed = 6f;
		public float         JumpForce = 10f;
		public AudioSource   JumpSound;
		public AudioClip[]   JumpClips;
		public Transform     CameraHandle;
		public GameObject    FirstPersonRoot;
		public GameObject    ThirdPersonRoot;

		[Header("Movement")]
		public float         UpGravity = 15f;
		public float         DownGravity = 25f;
		public float         GroundAcceleration = 55f;
		public float         GroundDeceleration = 25f;
		public float         AirAcceleration = 25f;
		public float         AirDeceleration = 1.3f;

		[Networked]
		private NetworkButtons _previousButtons { get; set; }
		[Networked]
		private int _jumpCount { get; set; }
		[Networked]
		private Vector3 _moveVelocity { get; set; }

		private int _visibleJumpCount;

		private SceneObjects _sceneObjects;

		public override void Spawned()
		{
			name = $"{Object.InputAuthority} ({(HasInputAuthority ? "Input Authority" : (HasStateAuthority ? "State Authority" : "Proxy"))})";

			// Enable first person visual for local player, third person visual for proxies.
			SetFirstPersonVisuals(HasInputAuthority);

			if (HasInputAuthority == false)
			{
				// Virtual cameras are enabled only for local player.
				var virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
				for (int i = 0; i < virtualCameras.Length; i++)
				{
					virtualCameras[i].enabled = false;
				}
			}

			_sceneObjects = Runner.GetSingleton<SceneObjects>();
		}

		public override void FixedUpdateNetwork()
		{
			if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
			{
				// After gameplay is finished we still want the player to finish movement and not stuck in the air.
				MovePlayer();
				return;
			}

			if (IsProxy)
				return;

			if (GetInput(out NetworkedInput input))
			{
				// Input is processed on InputAuthority and StateAuthority.
				ProcessInput(input);
			}
			else
			{
				// When no input is available, at least continue with movement (e.g. falling).
				MovePlayer();
			}

			// Camera is set based on KCC look rotation.
			Vector2 pitchRotation = KCC.GetLookRotation(true, false);
			CameraHandle.localRotation = Quaternion.Euler(pitchRotation);
		}

		public override void Render()
		{
			if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
				return;

			var moveVelocity = GetAnimationMoveVelocity();

			// Set animation parameters.
			Animator.SetBool("IsAlive", true);
			Animator.SetBool("IsGrounded", KCC.IsGrounded);
			Animator.SetFloat("MoveX", moveVelocity.x, 0.05f, Time.deltaTime);
			Animator.SetFloat("MoveZ", moveVelocity.z, 0.05f, Time.deltaTime);
			Animator.SetFloat("MoveSpeed", moveVelocity.magnitude);
			Animator.SetFloat("Look", -KCC.GetLookRotation(true, false).x / 90f);

			if (_visibleJumpCount < _jumpCount)
			{
				Animator.SetTrigger("Jump");

				JumpSound.clip = JumpClips[Random.Range(0, JumpClips.Length)];
				JumpSound.Play();
			}

			_visibleJumpCount = _jumpCount;
		}

		private void LateUpdate()
		{
			if (HasInputAuthority == false)
				return;

			// Camera is set based on interpolated KCC look rotation.
			var pitchRotation = KCC.GetLookRotation(true, false);
			CameraHandle.localRotation = Quaternion.Euler(pitchRotation);
		}

		private void ProcessInput(NetworkedInput input)
		{
			KCC.AddLookRotation(input.LookRotationDelta, -89f, 89f);

			// It feels better when player falls quicker
			KCC.SetGravity(KCC.RealVelocity.y >= 0f ? Vector3.down * UpGravity : Vector3.down * DownGravity);

			var inputDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			var jumpImpulse = Vector3.zero;

			if (input.Buttons.WasPressed(_previousButtons, EInputButton.Jump) && KCC.IsGrounded)
			{
				jumpImpulse = Vector3.up * JumpForce;
			}

			MovePlayer(inputDirection * MoveSpeed, jumpImpulse);

			if (KCC.HasJumped)
			{
				_jumpCount++;
			}

			// Store input buttons when the processing is done - next tick it is compared against current input buttons.
			_previousButtons = input.Buttons;
		}

		private void MovePlayer(Vector3 desiredMoveVelocity = default, Vector3 jumpImpulse = default)
		{
			float acceleration = 1f;

			if (desiredMoveVelocity == Vector3.zero)
			{
				// No desired move velocity - we are stopping.
				acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
			KCC.Move(_moveVelocity, jumpImpulse);
		}

		private void SetFirstPersonVisuals(bool firstPerson)
		{
			FirstPersonRoot.SetActive(firstPerson);
			ThirdPersonRoot.SetActive(firstPerson == false);
		}

		private Vector3 GetAnimationMoveVelocity()
		{
			if (KCC.RealSpeed < 0.01f)
				return default;

			var velocity = KCC.RealVelocity;

			// We only care about X an Z directions.
			velocity.y = 0f;

			if (velocity.sqrMagnitude > 1f)
			{
				velocity.Normalize();
			}

			// Transform velocity vector to local space.
			return transform.InverseTransformVector(velocity);
		}
	}
}
