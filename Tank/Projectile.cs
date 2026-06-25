using Godot;

public partial class Projectile : CharacterBody2D
{
	public Vector2 position;
	public float rotation;
	public float direction;

	[Export]
	public float Speed = -200;

	public override void _Ready()
	{
		GlobalPosition = position;
		GlobalRotation = rotation;
	}

	public override void _PhysicsProcess(double delta)
	{
		Velocity = new Vector2(Speed, 0).Rotated(direction);
		MoveAndSlide();
	}
}
