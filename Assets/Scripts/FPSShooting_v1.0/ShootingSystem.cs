using System;
using Sirenix.OdinInspector;
using StatusEffects;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShootingSystem : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private Transform _gunShootTransform;

	[SerializeField]
	private GameObject _bulletPrefab;

	[Header("Main Gun Data")]
	[Tooltip("Damage of each bullet in the gun")]
	[SerializeField]
	private float _damage = 1f;

	[Tooltip("How fast the gun fires")]
	[SerializeField]
	private float _fireRate = 0.8f;

	[Tooltip("Max amount of ammo in the gun")]
	[SerializeField]
	private int _maxAmmo = 5;

	[Tooltip("Bullet travel speed")]
	[SerializeField]
	private float _bulletVelocity = 100f;

	[Tooltip("Buffer to register a player shooting from their input")]
	[SerializeField]
	private float _fireBuffer = 0.1f;

	[Tooltip("Holding down the gun shoots it continuously")]
	[SerializeField]
	private bool _holdToShoot = true;

	[Tooltip("Amounts of bullets shot during one shot")]
	[SerializeField]
	private int _bulletsPerShoot = 3;

	[Header("Reload")]
	[Tooltip("Reloading does not give the max amount of ammo back")]
	[SerializeField]
	private bool _partialReload;

	[Tooltip("Ammo given per reload complete")]
	[ShowIf("_partialReload")]
	[SerializeField]
	private int _ammoPerReload = 1;

	[Tooltip("Time it takes to reload a certain amount of ammo")]
	[SerializeField]
	private float _reloadTime = 3f;

	[Tooltip("Buffer to register a reload from a given their input")]
	[SerializeField]
	private float _reloadBuffer = 0.1f;

	[Header("Bullet Spread")]
	[Tooltip("Curve of bullet spread")]
	[SerializeField]
	private AnimationCurve _bulletSpreadCurve;

	[Tooltip("How fast the spread increases")]
	[SerializeField]
	private float _bulletSpreadAdd = 0.8f;

	[Header("Status Effects")]
	[Tooltip("Status Effects To Apply")]
	[SerializeField]
	private Effect _statusEffect;

	[Tooltip("How much build up to apply per bullet")]
	[Range(0, 1)]
	[SerializeField]
	private float _statusEffectBuildUp = 0.1f;

	[Header("Misc")]
	[Tooltip("How far should the raycast be")]
	[SerializeField]
	private float _raycastDistance = 25f;

	[SerializeField]
	private LayerMask _enemyLayerMask;

	private int _ammo;
	private float _bulletSpreadBuildUp;

	// Gun Info
	private float _fireBufferTimer;
	private Camera _fpsCamera;
	private bool _isShooting;
	private float _maxBulletSpreadBuildUp = 1f;
	private float _reloadBufferTimer;
	private float _reloadTimer;
	private float _shootingCooldownTimer;

	// Event
	private ShootingEvents _shootingEvents;

	private bool CanShoot => _shootingCooldownTimer <= 0f && _reloadTimer <= 0f && _ammo > 0;
	private bool CanReload => _shootingCooldownTimer <= 0f && _reloadTimer <= 0f && _ammo < _maxAmmo;
	private bool CancelledReload => _partialReload && _fireBufferTimer > 0 && _ammo != 0 && IsReloading;

	private bool IsReloading => _reloadTimer > 0f;
	private bool IsShooting => _shootingCooldownTimer > 0f;
	public float GetMaxAmmo => _maxAmmo;
	public float GetAmmo => _ammo;

	private void Start()
	{
		_ammo = _maxAmmo;
		_shootingEvents = GameManager.Instance.ShootingEventsRef;
		_maxBulletSpreadBuildUp = _bulletSpreadCurve.keys[_bulletSpreadCurve.length - 1].time;
		_fpsCamera = CameraManager.Instance.MainCameraGameObject.GetComponent<Camera>();
		Debug.Log(_statusEffect.ToString());
	}

	private void Update()
	{
		HandleFireBuffer(Time.deltaTime);
		HandleReloadBuffer(Time.deltaTime);
		DealWithFireRate(Time.deltaTime);
		DealWithReloadTime(Time.deltaTime);
		DealWithSpread(Time.deltaTime);
		if ((CanReload && _reloadBufferTimer > 0f && !IsReloading) || (_ammo == 0 && !IsShooting && !IsReloading))
		{
			StartReload();
		}
		else if (CanShoot && _fireBufferTimer > 0f)
		{
			ShootGun();
		}
	}

	private void OnEnable()
	{
		InputManager.Instance.OnReloadPerformed.AddListener(OnReloadInput);
		InputManager.Instance.OnChangeGun.AddListener(ChangeStatusEffect);

		InputManager.Instance.OnShootingPerformed.AddListener(OnShootingStart);
		InputManager.Instance.OnShootingReleased.AddListener(OnShootingStop);
	}

	private void OnDisable()
	{
		InputManager.Instance?.OnReloadPerformed.RemoveListener(OnReloadInput);
		InputManager.Instance?.OnChangeGun.AddListener(ChangeStatusEffect);

		InputManager.Instance?.OnShootingPerformed.RemoveListener(OnShootingStart);
		InputManager.Instance?.OnShootingReleased.RemoveListener(OnShootingStop);
	}

	private void HandleFireBuffer(float deltaTime)
	{
		if (_holdToShoot && _isShooting)
		{
			_fireBufferTimer = _fireBuffer;
		}
		else if (_fireBufferTimer > 0f)
		{
			_fireBufferTimer -= deltaTime;
		}
	}

	private void HandleReloadBuffer(float deltaTime)
	{
		if (_reloadBufferTimer > 0f)
		{
			_reloadBufferTimer -= deltaTime;
		}
	}

	private void DealWithFireRate(float deltaTime)
	{
		if (_shootingCooldownTimer > 0f)
		{
			_shootingCooldownTimer -= deltaTime;
		}
	}

	private void DealWithReloadTime(float deltaTime)
	{
		if (CancelledReload)
		{
			_shootingEvents.EndGunReload(_ammo, _maxAmmo);
			_reloadTimer = 0f;
		}

		if (IsReloading)
		{
			_reloadTimer -= deltaTime;

			if (_reloadTimer <= 0f)
			{
				if (_partialReload && _ammo + _ammoPerReload < _maxAmmo)
				{
					_ammo += _ammoPerReload;
					_reloadTimer = _reloadTime;
				}
				else
				{
					_ammo = _maxAmmo;
					_shootingEvents.EndGunReload(_ammo, _maxAmmo);
				}
			}
		}
	}

	private void DealWithSpread(float deltaTime)
	{
		if (_bulletSpreadBuildUp > _maxBulletSpreadBuildUp)
		{
			_bulletSpreadBuildUp = _maxBulletSpreadBuildUp;
		}
		else if (_bulletSpreadBuildUp > 0f)
		{
			_bulletSpreadBuildUp -= deltaTime;
		}
	}

	private void ShootGun()
	{
		_ammo--;
		_shootingCooldownTimer = _fireRate;
		_shootingEvents.ShootGun(_ammo, _maxAmmo);

		// Shoot out a ray in the middle of the fps camera
		Ray ray = _fpsCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		bool isEnemyHit = Physics.Raycast(ray, out RaycastHit bulletHit);

		// Find the first world space point that the ray intersects
		Vector3 targetPoint = isEnemyHit ? bulletHit.point : ray.GetPoint(_raycastDistance);

		// Calculate the direction velocity from the gun position to the target point
		Vector3 directionWithoutSpread = targetPoint - _gunShootTransform.position;

		// Spawn Bullets
		for (int i = 0; i < _bulletsPerShoot; i++)
		{
			InstantiateBullet(directionWithoutSpread, _bulletSpreadCurve.Evaluate(_bulletSpreadBuildUp));
		}
	}

	private void InstantiateBullet(Vector3 directionWithoutSpread, float spreadVal)
	{
		// Apply spread
		float spread = spreadVal;
		float spreadX = Random.Range(-spread, spread);
		float spreadY = Random.Range(-spread, spread);
		Vector3 directionWithSpread = directionWithoutSpread + new Vector3(spreadX, spreadY, 0);

		// Instantiate current bullet
		GameObject currBullet = Instantiate(_bulletPrefab, _gunShootTransform.position, Quaternion.identity);
		// Rotate bullet to the correct direction
		currBullet.transform.up = directionWithSpread.normalized;

		// Add forces to bullet
		currBullet
			.GetComponent<Rigidbody>()
			.AddForce(directionWithSpread.normalized * _bulletVelocity, ForceMode.Impulse);

		// Populate bullet data
		currBullet.GetComponent<Bullet>().InitiateBullet(_damage, _statusEffect, _statusEffectBuildUp);

		_bulletSpreadBuildUp += _bulletSpreadAdd;
	}

	private void StartReload()
	{
		_shootingEvents.StartGunReload();
		_reloadTimer = _reloadTime;
	}

	private void ChangeStatusEffect()
	{
		_statusEffect = (Effect)(((int)_statusEffect + 1) % Enum.GetNames(typeof(Effect)).Length);
		Debug.Log(_statusEffect.ToString());
	}

	private void OnReloadInput()
	{
		_reloadBufferTimer = _reloadBuffer;
	}

	private void OnShootingStart()
	{
		if (_holdToShoot)
		{
			_isShooting = true;
		}
		else
		{
			_fireBufferTimer = _fireBuffer;
		}
	}

	private void OnShootingStop()
	{
		_isShooting = false;
	}
}
