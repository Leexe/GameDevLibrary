using System.Collections;
using StatusEffects;
using UnityEngine;

public class Bullet : MonoBehaviour
{
	[SerializeField]
	private LayerMask _destroyLayer;

	[SerializeField]
	private LayerMask _targetLayer;

	[SerializeField]
	private Rigidbody _rb;

	[SerializeField]
	private float _bulletCollisionLifetime = 0.6f;

	[SerializeField]
	private float _bulletLifetime = 2.5f;

	private float _damage;
	private IEnumerator _lifetime;
	private Effect _statusEffect;
	private float _statusEffectBuildUp;

	private void Start()
	{
		_lifetime = DestroyBullet();
		StartCoroutine(_lifetime);
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Hits the target layer
		if (((1 << collision.gameObject.layer) & _targetLayer) > 0)
		{
			collision.gameObject.GetComponent<HealthController>().TakeDamage(_damage);
			collision
				.gameObject.GetComponent<StatusEffectController>()
				.ApplyStatusEffect(_statusEffect, _statusEffectBuildUp);

			// Stop Bullet
			_rb.linearVelocity = Vector3.zero;

			// Deal with bullet lifetime
			StopCoroutine(_lifetime);
			_lifetime = DestroyBulletAfterCollision();
			StartCoroutine(_lifetime);
		}

		// Hits a layer that destroys the bullet
		if (((1 << collision.gameObject.layer) & _destroyLayer) > 0)
		{
			// Stop Bullet
			_rb.linearVelocity = Vector3.zero;

			// Deal with bullet lifetime
			StopCoroutine(_lifetime);
			_lifetime = DestroyBulletAfterCollision();
			StartCoroutine(_lifetime);
		}
	}

	public void InitiateBullet(float damage, Effect effect = 0, float buildUp = 0.1f)
	{
		_damage = damage;
		_statusEffect = effect;
		_statusEffectBuildUp = buildUp;
	}

	private IEnumerator DestroyBullet()
	{
		yield return new WaitForSeconds(_bulletLifetime);
		Destroy(gameObject);
	}

	private IEnumerator DestroyBulletAfterCollision()
	{
		_rb.linearVelocity = Vector3.zero;
		yield return new WaitForSeconds(_bulletCollisionLifetime);
		Destroy(gameObject);
	}
}
