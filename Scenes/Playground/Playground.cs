using Godot;
using System.Collections.Generic;

public partial class Playground : Node2D
{
	private static readonly int PORT = OS.GetEnvironment("PORT") != ""
		? int.Parse(OS.GetEnvironment("PORT"))
		: 7777;

	private const int MAX_PLAYERS = 4;

//render url here
	private const string JOIN_ADDRESS = "tank-shooter-game-xpg4.onrender.com";

	private readonly PackedScene TankScene = GD.Load<PackedScene>("res://Tank/Tank.tscn");

	private readonly Dictionary<long, Vector2> _peerSpawnPositions = new();
	private int _spawnIndex = 1;

	public override void _Ready()
	{
		GD.Print("Playground ready. Peer: " + Multiplayer.MultiplayerPeer);
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += () => GD.PrintErr("Connection failed.");
		if (OS.HasFeature("dedicated_server"))
			StartHost();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		GD.Print("Input received: " + @event);
		if (@event is not InputEventKey key || !key.Pressed || key.IsEcho()) return;
		if (Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer) return; // block only when needed

		if (key.Keycode == Key.H) StartHost();
		else if (key.Keycode == Key.J) StartJoin();
	}

	private void StartHost()
	{
		var peer = new WebSocketMultiplayerPeer();
		if (peer.CreateServer(PORT) != Error.Ok)
		{
			GD.PrintErr("Failed to start server.");
			return;
		}
		Multiplayer.MultiplayerPeer = peer;
		GD.Print($"Hosting on port {PORT}.");

		var spawnPos = GetNextSpawnPosition();
		Rpc(nameof(SpawnTank), 1, spawnPos);
	}

	private void StartJoin()
	{
		var peer = new WebSocketMultiplayerPeer();

		// Use wss:// for the Render URL (Render terminates SSL for you).
		// Use ws:// for local testing.
		string url = JOIN_ADDRESS == "127.0.0.1"
			? $"ws://{JOIN_ADDRESS}:{PORT}"
			: $"wss://{JOIN_ADDRESS}";

		if (peer.CreateClient(url) != Error.Ok)
		{
			GD.PrintErr("Failed to connect.");
			return;
		}
		Multiplayer.MultiplayerPeer = peer;
		GD.Print($"Connecting to {url}...");
	}

	private void OnPeerConnected(long id)
	{
		if (!Multiplayer.IsServer()) return;

		foreach (var kvp in _peerSpawnPositions)
			RpcId(id, nameof(SpawnTank), (int)kvp.Key, kvp.Value);

		var spawnPos = GetNextSpawnPosition();
		Rpc(nameof(SpawnTank), (int)id, spawnPos);
	}

	
	private void OnPeerDisconnected(long id)
	{
		if (!Multiplayer.IsServer()) return;
		Rpc(nameof(RemoveTank), (int)id);
	}

	private void OnConnectedToServer()
	{
		GD.Print("Connected. Waiting for server to spawn tanks...");
	}

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
		GetNode<Node2D>("Tanks").AddChild(tank);

		_peerSpawnPositions[(long)peerId] = spawnPos;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RemoveTank(int peerId)
	{
		GetNodeOrNull("Tanks/Tank_" + peerId)?.QueueFree();
		_peerSpawnPositions.Remove((long)peerId);
	}

	private Vector2 GetNextSpawnPosition()
	{
		Vector2 spawnPos = GetNode<Marker2D>("Tanks/Markers/Mark" + _spawnIndex).Position;

		_spawnIndex++;

		return spawnPos;
	}
}
