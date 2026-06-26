using Godot;

[GlobalClass]
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
		GlobalPosition = position;
		GlobalRotation = rotation;
	}

	public override void _PhysicsProcess(double delta)
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);

			if (collision.GetCollider() is Tank tank && tank != OwnerTank)
			{
				tank.Rpc("GotHit");

				QueueFree();
				return;
			}
		}

		Velocity = new Vector2(Speed, 0).Rotated(direction);
		MoveAndSlide();
	}
}
