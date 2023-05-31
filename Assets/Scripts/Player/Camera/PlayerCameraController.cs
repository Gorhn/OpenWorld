using Cinemachine;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Transform))]
public class PlayerCameraController : NetworkBehaviour {

    [SerializeField]
    private InputActionReference lookAction;

    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();

        Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.LookAt = transform;
        Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.Follow = transform;
    }

    public void OnEnableCamera(InputValue value) {
        Camera.main.GetComponent<CinemachineInputProvider>().XYAxis = value.isPressed ? lookAction : null;
    }

}
