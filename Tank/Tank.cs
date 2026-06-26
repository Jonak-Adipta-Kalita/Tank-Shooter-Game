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

	private readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://Tank/Projectile.tscn");
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

		if (!IsMultiplayerAuthority()) return;

		HandleMovement(delta);

		if (Input.IsActionJustPressed("shoot"))
		{
			Projectile projectile = ProjectileScene.Instantiate<Projectile>();
			projectile.OwnerTank = this;
			projectile.direction = Rotation;
			projectile.position = ProjectilePosition.GlobalPosition;
			projectile.rotation = GlobalRotation;

			GetParent<Playground>().AddChild(projectile);
		}

		Rpc(nameof(SyncTransform), GlobalPosition, GlobalRotation);
	}

// position syncing

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void SyncTransform(Vector2 pos, float rot)
	{
		GlobalPosition = pos;
		GlobalRotation = rot;
	}

// hit and rspwn logic 

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void GotHit()
	{
		if (_isDead) return;
		_isDead = true;

		Visible = false;
		GetNode<CollisionShape2D>("Collision1").SetDeferred("disabled", true);
		GetNode<CollisionShape2D>("Collision2").SetDeferred("disabled", true);

		if (!IsMultiplayerAuthority()) return;

		var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
		Vector2 spawnPos = spawnPoints.Count > 0
			? ((Node2D)spawnPoints[GD.RandRange(0, spawnPoints.Count - 1)]).GlobalPosition
			: _defaultSpawn;

		GetTree().CreateTimer(1.5).Timeout += () => Rpc(nameof(RespawnAt), spawnPos);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RespawnAt(Vector2 pos)
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

	private void HandleMovement(double delta)
	{
		var velocity = Vector2.Zero;
		_speed += Initial_Speed + _time * Acceleration;

		// Input gathering
		if (Input.IsActionPressed("move_right")) velocity.X += 1;
		if (Input.IsActionPressed("move_left"))  velocity.X -= 1;
		if (Input.IsActionPressed("move_down"))  velocity.Y += 1;
		if (Input.IsActionPressed("move_up"))    velocity.Y -= 1;

		// Normalize to prevent faster diagonal movement, then apply speed
		velocity = velocity.Normalized();
		Velocity = velocity * _speed;
		
		MoveAndSlide();
		
		Position = new Vector2(
			Mathf.Clamp(Position.X, 0, ScreenSize.X),
			Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);

		bool moving = Input.IsActionPressed("move_right") || Input.IsActionPressed("move_left") ||
					  Input.IsActionPressed("move_down")  || Input.IsActionPressed("move_up");

		if (moving)
		{
			_time += (float)delta;
		}
		else
		{
			_time = 0f;
			_speed = Initial_Speed;
		}

		if (Input.IsActionPressed("rotate_left"))  Rotation -= 0.05f;
		if (Input.IsActionPressed("rotate_right")) Rotation += 0.05f;
	}
}
