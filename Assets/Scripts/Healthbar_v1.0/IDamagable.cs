public interface IDamageable
{
	float Health { get; }
	float MaxHealth { get; }

	void TakeDamage(float damage);
	void Heal(float amount);
	void Kill();

	bool IsAlive { get; }
}
