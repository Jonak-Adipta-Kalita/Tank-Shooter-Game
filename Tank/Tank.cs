using Godot;
using System;

public partial class Tank : CharacterBody2D
{
	[Export]
	public int Initial_Speed { get; set; } = 1;
	[Export]
	public float Acceleration { get; set; } = 0.01f;
	private float _speed = 0f;
	private float _time = 0f;
	private bool _isDead = false;
	private Vector2 _defaultSpawn;

	public Vector2 ScreenSize;

	private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Tank/Projectile.tscn");
	private Node2D ProjectilePosition;

	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		ProjectilePosition = GetNode<Node2D>("Projectile");
		_defaultSpawn = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;

		HandleMovement(delta);

		if (Input.IsActionJustPressed("shoot"))
		{
			Projectile projectile = ProjectileScene.Instantiate<Projectile>();
			projectile.OwnerTank = this;

			projectile.direction = Rotation;
			projectile.position = ProjectilePosition.GlobalPosition;
			projectile.rotation = GlobalRotation;

			GetParent().GetParent<Playground>().AddChild(projectile);
		}
	}

	private void HandleMovement(double delta)
	{
		var velocity = Vector2.Zero;
		_speed += Initial_Speed + _time * Acceleration;

		if (Input.IsActionPressed("move_right")) velocity.X += 1;
		if (Input.IsActionPressed("move_left"))	velocity.X -= 1;
		if (Input.IsActionPressed("move_down"))	velocity.Y += 1;
		if (Input.IsActionPressed("move_up")) velocity.Y -= 1;

		velocity = velocity.Normalized();

		Velocity = velocity * _speed;
		MoveAndSlide();
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);

		bool moving = Input.IsActionPressed("move_right") || Input.IsActionPressed("move_left") ||
					  Input.IsActionPressed("move_down") || Input.IsActionPressed("move_up");

		if (moving)
		{
			_time += (float)delta;
		}
		else
		{
			_time = 0f;
			_speed = Initial_Speed;
		}

		if (Input.IsActionPressed("rotate_left")) Rotation -= 0.05f;
		if (Input.IsActionPressed("rotate_right")) Rotation += 0.05f;
	}

	public void GotHit()
	{
		if (_isDead) return;
		_isDead = true;

		Visible = false;

		GetNode<CollisionShape2D>("Collision1").SetDeferred("disabled", true);
		GetNode<CollisionShape2D>("Collision2").SetDeferred("disabled", true);

		Random random = new Random();
		int markIndex = random.Next(1, 5);

		Vector2 spawnPos = GetParent().GetParent<Playground>().GetNode<Marker2D>("Tanks/Markers/Mark" + markIndex).Position;

		GetTree().CreateTimer(1.5).Timeout += () => Respawn(spawnPos);
	}

	private void Respawn(Vector2 pos)
	{
		GlobalPosition = pos;
		Rotation = 0f;
		Velocity = Vector2.Zero;
		_speed = 0f;
		_time = 0f;

		GetNode<CollisionShape2D>("Collision1").Disabled = false;
		GetNode<CollisionShape2D>("Collision2").Disabled = false;

		Visible = true;
		_isDead = false;
	}
}
