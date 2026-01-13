using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectIconUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Image _buildUpBar;
    [SerializeField] private Image _activeDurationBar;
    [SerializeField] private TextMeshProUGUI _countText;

    public void ActivatedStatusEffectUI() {}

    public void UpdateBuildUpUI(float normalizedVal) {
        _buildUpBar.fillAmount = normalizedVal;
    }

    public void UpdateActiveDurationUI(float normalizedVal, bool isActive = false) {
        if (isActive) {
            _activeDurationBar.fillAmount = normalizedVal;
        }
        else {
            _activeDurationBar.fillAmount = 1f;
        }
    }

    public void UpdateCountText(int count, bool countEnabled) {
        if (countEnabled && count != 0) {
            _countText.text = count.ToString();
        }
        else {
            _countText.text = "";
        }
    }
}