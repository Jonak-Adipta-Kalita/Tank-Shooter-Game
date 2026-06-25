using System;
using Godot;

public partial class Tank : CharacterBody2D
{
	[Export]
	public int Initial_Speed { get; set; } = 1;

	[Export]
	public float Acceleration { get; set; } = 0.01f;

	public Vector2 ScreenSize;

	private float _speed = 0f;
	private float _time = 0f;

	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
	}

	public override void _Process(double delta)
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
		GD.Print(allPressed);

		if (allPressed)
		{
			_time += (float)delta;
			GD.Print(_time.ToString());
		}
		else
		{
			_time = 0f;
			_speed = Initial_Speed;
		}

	}
}