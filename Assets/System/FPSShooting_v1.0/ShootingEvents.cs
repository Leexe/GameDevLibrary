using System;
using UnityEngine;

public class ShootingEvents {
	public Action<int, int> OnShootGun;

	public void ShootGun(int ammo, int maxAmmo) {
		OnShootGun?.Invoke(ammo, maxAmmo);
	}

	public Action OnStartGunReload;

	public void StartGunReload() {
		OnStartGunReload?.Invoke();
	}

	public Action<int, int> OnEndGunReload;

	public void EndGunReload(int ammo, int maxAmmo) {
		OnEndGunReload?.Invoke(ammo, maxAmmo);
	}
}