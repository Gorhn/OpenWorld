using ClientSidePrediction;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementClient : NetworkBehaviour {

    private float timeSinceLastTick = 0f;
    private float minTimeBetweenUpdates;

    private PlayerInput[] inputBuffer;
    private Queue<PlayerInput> inputQueue = new Queue<PlayerInput>(6);
    private PlayerState lastServerState = default;

    private uint currentTick = 0;
    private uint bufferSize = 1024;

    [SerializeField]
    private InputActionReference moveAction;

    private PlayerState lastProcessedState = default;
    private uint lastProcessedTick = 0;

    private CharacterController characterController;
    private float groundedGravity = 0.05f;
    private float standardGravity = 9.8f;

    private void Awake() {
        Physics.autoSimulation = false;
        minTimeBetweenUpdates = 1f / NetworkManager.singleton.serverTickRate;
        inputBuffer = new PlayerInput[bufferSize];
        moveAction.action.actionMap.Enable();
        characterController = GetComponent<CharacterController>();
    }

    protected PlayerInput GetInput(float deltaTime, uint currentTick) {
        Vector2 movement = moveAction.action.ReadValue<Vector2>();

        return new PlayerInput(deltaTime, currentTick, movement, transform.rotation);
    }

    [Server]
    protected PlayerState RecordState(uint lastTick) {
        return new PlayerState(lastTick, transform.position, transform.rotation);
    }

    [Server]
    public void SendState(PlayerState state) {
        RpcSendState(state);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcSendState(PlayerState state) {
        lastServerState = state;
    }

    private void ApplyState(PlayerState state) {
        if (!state.Equals(default(PlayerState)) && (!lastProcessedState.Equals(default(PlayerState)) || !lastProcessedState.Equals(lastServerState))) {
            lastProcessedState = lastServerState;
            SetState(lastServerState);
        }
    }

    private void UpdatePrediction() {
        lastProcessedState = lastServerState;
        SetState(lastProcessedState);

        uint nextTickToProcess = lastProcessedState.LastProcessedInputTick + 1;
        while (nextTickToProcess < currentTick) {
            ProcessInput(inputBuffer[nextTickToProcess % bufferSize]);
        }
    }

    public void SetState(PlayerState state) {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }

    [Client]
    public void SendInput(PlayerInput input) {
        CmdSendInput(input);
    }

    [Command(channel = Channels.Unreliable)]
    void CmdSendInput(PlayerInput input) {
        inputQueue.Enqueue(input);
    }

    private void ProcessInputServer() {
        while (inputQueue.Count > 0) {
            var dequeuedInput = inputQueue.Dequeue();
            ProcessInput(dequeuedInput);

            lastProcessedTick = dequeuedInput.Tick;
        }
    }

    private void ProcessInput(PlayerInput input) {
        Vector3 movement = Vector3.zero;

        if (input.movement != Vector2.zero) {
            float gravity = characterController.isGrounded ? groundedGravity : standardGravity;
            movement = new Vector3(input.movement.x * 2.0f, -gravity, input.movement.y * 2.0f);
        }

        characterController.Move(movement * input.DeltaTime);
        SceneManager.GetActiveScene().GetPhysicsScene().Simulate(input.DeltaTime);
    }

    private void HandleTick() {

        if (isClient) {
            if (isLocalPlayer) {
                var input = GetInput(timeSinceLastTick, currentTick);
                var bufferIndex = currentTick % bufferSize;

                inputBuffer[bufferIndex] = input;
                SendInput(input);
            }

            ApplyState(lastServerState);
        }

        if (isServer) {
            ProcessInputServer();
            PlayerState state = RecordState(lastProcessedTick);
            SendState(state);
        }

        currentTick++;
        timeSinceLastTick = 0f;

    }

    void FixedUpdate() {
        timeSinceLastTick += Time.deltaTime;

        if (timeSinceLastTick >= minTimeBetweenUpdates) {
            HandleTick();
        }
    }

}
