using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
	protected void SetImageTransparent(Image image)
	{
		image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
	}

	protected void SetImageVisible(Image image)
	{
		image.color = new Color(image.color.r, image.color.g, image.color.b, 1);
	}

	protected float Lerp(float a, float b, float t)
	{
		return ((1.0f - t) * a) + (b * t);
	}

	protected float InverseLerp(float a, float b, float v)
	{
		return (v - a) / (b - a);
	}
}
