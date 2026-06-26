using Godot;

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
		_defaultSpawn = GlobalPosition; // Fallback if no spawn points exist in the scene
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

		if (Input.IsActionPressed("move_right"))
			velocity.X += _speed;
		if (Input.IsActionPressed("move_left"))
			velocity.X -= _speed;
		if (Input.IsActionPressed("move_down"))
			velocity.Y += _speed;
		if (Input.IsActionPressed("move_up"))
			velocity.Y -= _speed;

		Velocity = velocity;
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

		if (Input.IsActionPressed("rotate_left"))
			Rotation -= 0.05f;
		if (Input.IsActionPressed("rotate_right"))
			Rotation += 0.05f;
	}

	public void GotHit()
	{
		if (_isDead) return; // Ignore hits while already dead/respawning
		_isDead = true;

		Visible = false;

		// Disable both collision shapes so dead tanks can't be hit by stray projectiles
		GetNode<CollisionShape2D>("Collision1").SetDeferred("disabled", true);
		GetNode<CollisionShape2D>("Collision2").SetDeferred("disabled", true);

		// Pick a random SpawnPoint from the scene; fall back to where the tank started
		var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
		Vector2 spawnPos = spawnPoints.Count > 0
			? ((Node2D)spawnPoints[GD.RandRange(0, spawnPoints.Count - 1)]).GlobalPosition
			: _defaultSpawn;

		// Respawn after 1.5 seconds
		GetTree().CreateTimer(1.5).Timeout += () => Respawn(spawnPos);
	}

	private void Respawn(Vector2 pos)
	{
		GlobalPosition = pos;
		Rotation = 0f;
		Velocity = Vector2.Zero;
		_speed = 0f;
		_time = 0f;

		// Re-enable collision shapes
		GetNode<CollisionShape2D>("Collision1").Disabled = false;
		GetNode<CollisionShape2D>("Collision2").Disabled = false;

		Visible = true;
		_isDead = false;
	}
}
