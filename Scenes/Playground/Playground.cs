using Godot;
using System.Collections.Generic;

public partial class Playground : Node2D
{
	private const int PORT = 7777;
	private const int MAX_PLAYERS = 4;
	private const string JOIN_ADDRESS = "127.0.0.1"; 

	private readonly PackedScene TankScene = GD.Load<PackedScene>("res://Tank/Tank.tscn");

// gives relative pos of new tanks to every pther presetn tank
	private readonly Dictionary<long, Vector2> _peerSpawnPositions = new();
	private int _spawnIndex = 0;

	public override void _Ready()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += () => GD.PrintErr("Connection failed.");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.IsEcho()) return;
		if (Multiplayer.MultiplayerPeer is not null) return; 

		if (key.Keycode == Key.H) StartHost();
		else if (key.Keycode == Key.J) StartJoin();
	}

// Hodting connection startup 

	private void StartHost()
	{
		var peer = new ENetMultiplayerPeer();
		if (peer.CreateServer(PORT, MAX_PLAYERS) != Error.Ok)
		{
			GD.PrintErr("Failed to start server.");
			return;
		}
		Multiplayer.MultiplayerPeer = peer;
		GD.Print($"Hosting on port {PORT}. Press H on another instance to... wait, press J to join.");

		var spawnPos = GetNextSpawnPosition();
		Rpc(nameof(SpawnTank), 1, spawnPos);
	}

	private void StartJoin()
	{
		var peer = new ENetMultiplayerPeer();
		if (peer.CreateClient(JOIN_ADDRESS, PORT) != Error.Ok)
		{
			GD.PrintErr("Failed to connect.");
			return;
		}
		Multiplayer.MultiplayerPeer = peer;
		GD.Print($"Connecting to {JOIN_ADDRESS}:{PORT}...");
	}

// Multiplayer signal handlers
	private void OnPeerConnected(long id)
	{
		if (!Multiplayer.IsServer()) return;

		// updayte for new tank added
		foreach (var kvp in _peerSpawnPositions)
			RpcId(id, nameof(SpawnTank), (int)kvp.Key, kvp.Value);

		// pawn the bitch in
		var spawnPos = GetNextSpawnPosition();
		Rpc(nameof(SpawnTank), (int)id, spawnPos);
	}

// debug connecting and disconnecting
	
	private void OnPeerDisconnected(long id)
	{
		if (!Multiplayer.IsServer()) return;
		Rpc(nameof(RemoveTank), (int)id);
	}

	private void OnConnectedToServer()
	{
		GD.Print("Connected. Waiting for server to spawn tanks...");
	}


// RPCs implementation 

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnTank(int peerId, Vector2 spawnPos)
	{
		string tankName = "Tank_" + peerId;
		if (GetNodeOrNull(tankName) is not null) return; 
		var tank = TankScene.Instantiate<Tank>();
		tank.Name = tankName;
		tank.Position = spawnPos;          
		tank.SetMultiplayerAuthority(peerId); 
		AddChild(tank);

		_peerSpawnPositions[(long)peerId] = spawnPos;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RemoveTank(int peerId)
	{
		GetNodeOrNull("Tank_" + peerId)?.QueueFree();
		_peerSpawnPositions.Remove((long)peerId);
	}

// Some fallbacks

	private Vector2 GetNextSpawnPosition()
	{
		var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
		if (spawnPoints.Count > 0)
		{
			var point = (Node2D)spawnPoints[_spawnIndex % spawnPoints.Count];
			_spawnIndex++;
			return point.GlobalPosition;
		}

		// Fallback four corners — adjust to fit your level
		Vector2[] fallback = {
			new(100, 100), new(700, 100), new(100, 500), new(700, 500)
		};
		return fallback[_spawnIndex++ % fallback.Length];
	}
}
