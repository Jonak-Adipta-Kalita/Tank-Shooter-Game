using Godot;
using System;

public partial class Playground : Node2D
{
	private PackedScene TankScene = GD.Load<PackedScene>("res://Tank/Tank.tscn");

	public override void _Ready()
	{
		Random random = new Random();
		int markIndex = random.Next(1, 5);

		Node2D Tanks = GetNode<Node2D>("Tanks");
		Vector2 pos = Tanks.GetNode<Node2D>("Markers/Mark" + markIndex).Position;

		Tank tank = TankScene.Instantiate<Tank>();
		tank.Position = pos;
		Tanks.AddChild(tank);
	}

	public override void _Process(double delta)
	{
	}
}
