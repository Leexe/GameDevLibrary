using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class VNCharacter : MonoBehaviour
{
	[FoldoutGroup("References")]
	[SerializeField]
	private Image _image;

	[FoldoutGroup("References")]
	[SerializeField]
	private Image _tempImage;

	[FoldoutGroup("References")]
	[SerializeField]
	private LayoutElement _layoutElement;

	// Private Variables
	private Tween _positionTween;
	private Tween _alphaTween;
	private Tween _shakeTween;
	private Tween _scaleTween;
	private Tween _colorTween;

	private void OnDisable()
	{
		_positionTween.Stop();
		_shakeTween.Stop();
		_scaleTween.Stop();
		_alphaTween.Complete();
		_colorTween.Complete();
	}

	#region Transition Animations

	/// <summary>
	/// Fades in the character sprite.
	/// </summary>
	/// <param name="fadeDuration">Duration of the fade animation.</param>
	public void FadeIn(float fadeDuration = 1f)
	{
		if (fadeDuration == 0f)
		{
			SetTransparency(1f);
		}
		else
		{
			_alphaTween.Complete();
			_alphaTween = Tween.Alpha(_image, 1f, fadeDuration);
		}
	}

	/// <summary>
	/// Fades out the character sprite.
	/// </summary>
	/// <param name="fadeDuration">Duration of the fade animation.</param>
	public void FadeOut(float fadeDuration = 1f)
	{
		if (fadeDuration == 0f || Mathf.Approximately(_image.color.a, 0f))
		{
			SetTransparency(0f);
		}
		else
		{
			_alphaTween.Complete();
			_alphaTween = Tween.Alpha(_image, 0f, fadeDuration);
		}
	}

	/// <summary>
	/// Animates this character sprite from its old position to a target position.
	/// </summary>
	/// <param name="oldPosition">The original position before the layout change.</param>
	/// <param name="targetPosition">The target position to animate to.</param>
	/// <param name="tweenDuration">Duration of the movement animation.</param>
	public void TweenPositions(Vector3 oldPosition, Vector3 targetPosition, float tweenDuration = 1f)
	{
		if (oldPosition == targetPosition)
		{
			return;
		}

		// Complete any existing position tween
		_positionTween.Complete();

		// Ignore layout during animation
		_layoutElement.ignoreLayout = true;
		transform.position = oldPosition;
		_positionTween = Tween
			.Position(transform, targetPosition, tweenDuration)
			.OnComplete(() => _layoutElement.ignoreLayout = false);
	}

	/// <summary>
	/// Animates the character sliding in from the side while fading in.
	/// </summary>
	/// <param name="slideOffset">Horizontal offset to slide from (positive = from right, negative = from left).</param>
	/// <param name="duration">Duration of the slide and fade animation.</param>
	public void SlideIn(float slideOffset, float duration = 1f)
	{
		// Skip animation if duration is 0
		if (duration <= 0f)
		{
			return;
		}

		// Complete any existing position tween
		_positionTween.Complete();

		// Store the target position (current layout position)
		Vector3 targetPosition = transform.position;

		// Set starting position with offset
		Vector3 startPosition = targetPosition + new Vector3(slideOffset, 0f, 0f);
		transform.position = startPosition;

		// Ignore layout during animation
		_layoutElement.ignoreLayout = true;

		// Start combined slide and fade animations
		_positionTween = Tween
			.Position(transform, targetPosition, duration, Ease.OutCubic)
			.OnComplete(() => _layoutElement.ignoreLayout = false);
	}

	/// <summary>
	/// Animates the character sliding out from the current position.
	/// </summary>
	/// <param name="slideOffset">Horizontal offset to slide to.</param>
	/// <param name="duration">Duration of the slide animation.</param>
	public void SlideOut(float slideOffset, float duration = 1f)
	{
		// Skip animation if duration is 0
		if (duration <= 0f)
		{
			return;
		}

		// Complete any existing position tween
		_positionTween.Complete();

		// Ignore layout during animation
		_layoutElement.ignoreLayout = true;

		// Move to target
		Vector3 targetPosition = transform.position + new Vector3(slideOffset, 0f, 0f);

		_positionTween = Tween.Position(transform, targetPosition, duration, Ease.InCubic);
	}

	/// <summary>
	/// Cross-fades the character sprite with a new sprite.
	/// </summary>
	/// <param name="newSprite">The new sprite to cross-fade to.</param>
	/// <param name="duration">Duration of the cross-fade animation.</param>
	public void CrossFadeSprite(Sprite newSprite, float duration = 1f)
	{
		// Set the temp image to be the new sprite
		_tempImage.sprite = newSprite;
		_tempImage.gameObject.SetActive(true);

		// Set the temp image to be transparent
		Color startColor = _image.color;
		startColor.a = 0f;
		_tempImage.color = startColor;
		_tempImage.preserveAspect = _image.preserveAspect;

		Tween
			.Alpha(_tempImage, 1f, duration)
			.OnComplete(() =>
			{
				_image.sprite = newSprite;
				_tempImage.sprite = null;
				_tempImage.gameObject.SetActive(false);
			});
	}

	/// <summary>
	/// Fades out the character sprite and destroys it.
	/// </summary>
	/// <param name="fadeDuration">Duration of the fade animation.</param>
	public void FadeOutAndDestroy(float fadeDuration = 1f)
	{
		if (fadeDuration == 0)
		{
			Destroy(gameObject);
		}
		else
		{
			_alphaTween.Complete();
			_alphaTween = Tween.Alpha(_image, 0f, fadeDuration).OnComplete(() => Destroy(gameObject));
		}
	}

	#endregion

	#region Sprite Animations

	/// <summary>
	/// Shakes the character sprite horizontally.
	/// </summary>
	/// <param name="shakeOffset">Offset for the shake effect.</param>
	/// <param name="duration">Duration of the shake animation.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void ShakeHorizontal(float shakeOffset = 20f, float duration = 0.7f)
	{
		_shakeTween.Complete();
		_shakeTween = Tween.ShakeLocalPosition(
			_image.transform,
			new Vector3(shakeOffset, 0f, 0f),
			duration,
			15,
			easeBetweenShakes: Ease.Default
		);
	}

	/// <summary>
	/// Shakes the character sprite vertically.
	/// </summary>
	/// <param name="shakeOffset">Offset for the shake effect.</param>
	/// <param name="duration">Duration of the shake animation.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void ShakeVertical(float shakeOffset = 30f, float duration = 0.7f)
	{
		_shakeTween.Complete();
		_shakeTween = Tween.ShakeLocalPosition(
			_image.transform,
			new Vector3(0f, shakeOffset, 0f),
			duration,
			15,
			easeBetweenShakes: Ease.Default
		);
	}

	/// <summary>
	/// Makes the character sprite bounce like jelly
	/// </summary>
	/// <param name="bounceStrength">Strength of the bounce.</param>
	/// <param name="duration">Duration of the bounce animation.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void Bounce(float bounceStrength = 0.1f, float duration = 0.35f)
	{
		_scaleTween.Complete();
		_scaleTween = Tween.PunchScale(_image.transform, new Vector3(bounceStrength, bounceStrength, 0f), duration);
	}

	/// <summary>
	/// Expands the character scale and returns it back to normal.
	/// </summary>
	/// <param name="popStrength">Strength of the pop, relative to the scale of the character sprite</param>
	/// <param name="duration">Duration of the pop animation.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void Pop(float popStrength = 1.05f, float duration = 0.3f)
	{
		_scaleTween.Complete();
		_scaleTween = Tween.Scale(
			_image.transform,
			new Vector3(_image.transform.localScale.x * popStrength, _image.transform.localScale.y * popStrength, 0),
			duration / 2f,
			Ease.Default,
			2,
			CycleMode.Yoyo
		);
	}

	/// <summary>
	/// Character sprite jumps up and back down.
	/// </summary>
	/// <param name="jumpHeight">Height of the jump.</param>
	/// <param name="duration">Duration of the jump.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void Hop(float jumpHeight = 100f, float duration = 0.35f)
	{
		_shakeTween.Complete();
		Vector3 startPos = _image.transform.localPosition;
		_shakeTween = Tween
			.LocalPosition(
				_image.transform,
				startPos + new Vector3(0, jumpHeight, 0),
				duration / 2f,
				Ease.Default,
				2,
				CycleMode.Yoyo
			)
			.OnComplete(() =>
			{
				_image.transform.localPosition = startPos;
			});
	}

	/// <summary>
	/// Character sprite goes downwards and back up.
	/// </summary>
	/// <param name="hopHeight">Height of the hop.</param>
	/// <param name="duration">Duration of the hop.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void ReverseHop(float hopHeight = 33f, float duration = 0.35f)
	{
		_shakeTween.Complete();
		Vector3 startPos = _image.transform.localPosition;
		_shakeTween = Tween
			.LocalPosition(
				_image.transform,
				startPos + new Vector3(0, -hopHeight, 0),
				duration / 2f,
				Ease.OutQuad,
				2,
				CycleMode.Yoyo
			)
			.OnComplete(() =>
			{
				_image.transform.localPosition = startPos;
			});
	}

	/// <summary>
	/// Flashes the character sprite with a color.
	/// </summary>
	/// <param name="flashColor">Color to flash.</param>
	/// <param name="duration">Duration of the flash.</param>
	[Button("Flash")]
	[FoldoutGroup("Debug Animations")]
	public void Flash(Color flashColor, float duration = 0.25f)
	{
		_colorTween.Complete();
		Color originalColor = _image.color;
		_colorTween = Tween
			.Color(_image, flashColor, duration / 2f, cycles: 2, cycleMode: CycleMode.Yoyo)
			.OnComplete(() => _image.color = originalColor);
	}

	/// <summary>
	/// Pushes the sprite to the side horizontally and back.
	/// </summary>
	/// <param name="dodgeDistance">Distance to dodge.</param>
	/// <param name="duration">Duration of the dodge.</param>
	[Button]
	[FoldoutGroup("Debug Animations")]
	public void Dodge(float dodgeDistance = 100f, float duration = 0.35f)
	{
		_shakeTween.Complete();
		Vector3 startPos = _image.transform.localPosition;
		_shakeTween = Tween
			.LocalPosition(
				_image.transform,
				startPos + new Vector3(dodgeDistance, 0, 0),
				duration / 2f,
				Ease.OutQuad,
				2,
				CycleMode.Yoyo
			)
			.OnComplete(() =>
			{
				_image.transform.localPosition = startPos;
			});
	}

	#endregion

	#region Setup

	public void ChangeObjectName(string objName)
	{
		name = objName;
	}

	public void SwitchSprite(Sprite sprite)
	{
		_image.sprite = sprite;
	}

	public void SetTransparency(float alpha)
	{
		Color color = _image.color;
		color.a = alpha;
		_image.color = color;
	}

	public Vector3 GetPosition()
	{
		return transform.position;
	}

	public void SetParent(Transform newParent)
	{
		transform.SetParent(newParent);
	}

	public void Destroy()
	{
		Destroy(gameObject);
	}

	#endregion
}
