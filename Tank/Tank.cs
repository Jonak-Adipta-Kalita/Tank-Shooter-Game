using Godot;

public partial class Tank : CharacterBody2D
{
	[Export]
	public int Initial_Speed { get; set; } = 1;
	[Export]
	public float Acceleration { get; set; } = 0.01f;
	private float _speed = 0f;
	private float _time = 0f;

	public Vector2 ScreenSize;

	private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Tank/Projectile.tscn");
	private Node2D ProjectilePosition;

	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		ProjectilePosition = GetNode<Node2D>("Projectile");
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement(delta);

		if (Input.IsActionJustPressed("shoot"))
		{
			Projectile projectile = ProjectileScene.Instantiate<Projectile>();

			projectile.direction = Rotation;
			projectile.position = ProjectilePosition.GlobalPosition;
			projectile.rotation = GlobalRotation;

			GetParent<Playground>().AddChild(projectile);
		}
	}

	private void HandleMovement(double delta)
	{
		var velocity = Vector2.Zero;
		_speed += Initial_Speed + _time * Acceleration;

		if (Input.IsActionPressed("move_right"))
		{
			velocity.X += _speed;
		}

		if (Input.IsActionPressed("move_left"))
		{
			velocity.X -= _speed;
		}

		if (Input.IsActionPressed("move_down"))
		{
			velocity.Y += _speed;
		}

		if (Input.IsActionPressed("move_up"))
		{
			velocity.Y -= _speed;
		}

		Position += velocity * (float)delta;
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);

		bool allPressed = Input.IsActionPressed("move_right") || Input.IsActionPressed("move_left") || Input.IsActionPressed("move_down") || Input.IsActionPressed("move_up");

		if (allPressed)
		{
			_time += (float)delta;
		}
		else
		{
			_time = 0f;
			_speed = Initial_Speed;
		}

		if (Input.IsActionPressed("rotate_left"))
		{
			Rotation -= 0.05f;
		}
		if (Input.IsActionPressed("rotate_right"))
		{
			Rotation += 0.05f;
		}
	}
}