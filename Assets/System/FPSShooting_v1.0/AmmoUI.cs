using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour {
	[SerializeField] private TextMeshProUGUI _text;

	private void OnEnable() {
		GameManager.Instance.ShootingEventsRef.OnShootGun += UpdateAmmoDisplay;
		GameManager.Instance.ShootingEventsRef.OnStartGunReload += UpdateAmmoDisplay_Reload;
		GameManager.Instance.ShootingEventsRef.OnEndGunReload += UpdateAmmoDisplay;
	}

	private void OnDisable() {
		if (GameManager.Instance) {
			GameManager.Instance.ShootingEventsRef.OnShootGun -= UpdateAmmoDisplay;
			GameManager.Instance.ShootingEventsRef.OnStartGunReload -= UpdateAmmoDisplay_Reload;
			GameManager.Instance.ShootingEventsRef.OnEndGunReload -= UpdateAmmoDisplay;
    }
	}

	private void UpdateAmmoDisplay(int ammo, int maxAmmo) {
		_text.text = ammo.ToString();
	}

	private void UpdateAmmoDisplay_Reload() {
    _text.text = "Reloading...";
  } 
}