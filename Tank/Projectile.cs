using Godot;

public partial class Projectile : CharacterBody2D
{
	public Vector2 position;
	public float rotation;
	public float direction;
	public Tank OwnerTank;

	[Export]
	public float Speed = -200;

	public override void _Ready()
	{
		// NOTE: BodyEntered is NOT emitted by CharacterBody2D in Godot 4.
		// Collision is detected below via MoveAndSlide() results instead.
		GlobalPosition = position;
		GlobalRotation = rotation;
	}

	public override void _PhysicsProcess(double delta)
	{
		Velocity = new Vector2(Speed, 0).Rotated(direction);
		MoveAndSlide();

		// Check every body the projectile physically slid against this frame
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);

			if (collision.GetCollider() is Tank tank && tank != OwnerTank)
			{
				tank.GotHit();
				QueueFree(); // Destroy the projectile on impact
				return;      // Stop checking after first hit
			}
		}
	}
}
