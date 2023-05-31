using ClientSidePrediction;
using System;
using UnityEngine;

public struct PlayerState : IEquatable<PlayerState>, INetworkedClientState {

    public uint LastProcessedInputTick { get; }
    public Vector3 position { get; }
    public Quaternion rotation { get; }

    public PlayerState(uint LastProcessedInputTick, Vector3 position, Quaternion rotation) {
        this.LastProcessedInputTick = LastProcessedInputTick;
        this.position = position;
        this.rotation = rotation;
    }

    public bool Equals(PlayerState other) {
        return position.Equals(other.position) && rotation.Equals(other.rotation);
    }

    public bool Equals(INetworkedClientState other) {
        return other is PlayerState __other && Equals(__other);
    }
}

public struct PlayerInput : INetworkedClientInput {

    public float DeltaTime { get; }
    public uint Tick { get; }
    public Vector2 movement { get; }
    public Quaternion rotation { get; }

    public PlayerInput(float DeltaTime, uint Tick, Vector2 movement, Quaternion rotation) {
        this.DeltaTime = DeltaTime;
        this.Tick = Tick;
        this.movement = movement;
        this.rotation = rotation;
    }

}