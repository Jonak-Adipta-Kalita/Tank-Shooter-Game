using Godot;
using System;

[GlobalClass, Icon("res://Ass/tank.png")]
public partial class Tank : CharacterBody2D
{
	[Export]
	public int Initial_Speed { get; set; } = 1;
	[Export]
	public float Acceleration { get; set; } = 50f;
	
	private float _speed = 0f;
	private float _time = 0f;
	private Vector2 _lastDirection = Vector2.Zero;

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

			GetParent().GetParent<Playground>().GetNode("Projectiles").AddChild(projectile);
		}

		Rpc(nameof(SyncTransform), GlobalPosition, GlobalRotation);
	}


	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void SyncTransform(Vector2 pos, float rot)
	{
		GlobalPosition = pos;
		GlobalRotation = rot;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void GotHit()
	{
		if (_isDead) return;
		_isDead = true;

		Visible = false;

		GetNode<CollisionPolygon2D>("Collider").SetDeferred("disabled", true);

		if (!IsMultiplayerAuthority()) return;

		Random random = new Random();
		int markIndex = random.Next(1, 5);

		Vector2 spawnPos = GetParent().GetParent<Playground>().GetNode<Marker2D>("Tanks/Markers/Mark" + markIndex).Position;

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

		GetNode<CollisionPolygon2D>("Collider").Disabled = false;

		Visible = true;
		_isDead = false;
	}

	private void HandleMovement(double delta)
	{
		var velocity = Vector2.Zero;

		if (Input.IsActionPressed("move_right")) velocity.X += 1;
		if (Input.IsActionPressed("move_left"))  velocity.X -= 1;
		if (Input.IsActionPressed("move_down"))  velocity.Y += 1;
		if (Input.IsActionPressed("move_up"))    velocity.Y -= 1;

		bool moving = velocity != Vector2.Zero;

		if (moving)
		{
			_lastDirection = velocity.Normalized();
			_time += (float)delta;
			_speed = Initial_Speed + _time * Acceleration;
		}
		else
		{
			_time -= (float)delta;
			_speed = Initial_Speed - _time * Acceleration;
		}

		_speed = Mathf.Clamp(_speed, 0, 500);

		if (!moving && _speed <= 0.01f)
		{
			_speed = 0;
			_lastDirection = Vector2.Zero;
			_time = 0;
		}

		Velocity = _lastDirection * _speed;
		MoveAndSlide();

		Position = new Vector2(
			Mathf.Clamp(Position.X, 0, ScreenSize.X),
			Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);

		if (Input.IsActionPressed("rotate_left"))  Rotation -= 0.05f;
		if (Input.IsActionPressed("rotate_right")) Rotation += 0.05f;
	}
}
