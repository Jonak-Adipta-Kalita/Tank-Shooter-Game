using Godot;

public partial class Projectile : CharacterBody2D
{
	private AnimatedSprite2D sprite;


	public override void _Ready()
	{
		Visible = false;
		sprite = GetNode<AnimatedSprite2D>("Sprite");
	}

	public override void _PhysicsProcess(double delta)
	{
	}
}
