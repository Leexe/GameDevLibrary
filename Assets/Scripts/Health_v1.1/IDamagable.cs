public interface IDamageable
{
	float Health { get; }
	float MaxHealth { get; }

	bool IsAlive { get; }

	void TakeDamage(float damage);
	void Heal(float amount);
	void Kill();
}
