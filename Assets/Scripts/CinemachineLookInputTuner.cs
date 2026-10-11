using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>Scales mouse-delta and gamepad-stick input appropriately for Cinemachine's orbital axes.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineInputAxisController))]
public sealed class CinemachineLookInputTuner : MonoBehaviour
{
    [SerializeField, Min(0f)] private float mouseDegreesPerPixel = 0.12f;
    [SerializeField, Min(0f)] private float gamepadDegreesPerSecond = 140f;

    private CinemachineInputAxisController axisController;

    private void OnEnable()
    {
        axisController = GetComponent<CinemachineInputAxisController>();
        if (axisController != null) axisController.ReadControlValueOverride = ReadLookAxis;
    }

    private void OnDisable()
    {
        if (axisController != null) axisController.ReadControlValueOverride = null;
    }

    private float ReadLookAxis(InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, Object context, CinemachineInputAxisController.Reader.ControlValueReader defaultReader)
    {
        float value = defaultReader(action, hint, context, defaultReader);
        if (action.activeControl != null && action.activeControl.device is Mouse)
        {
            float deltaTime = Application.isPlaying ? Mathf.Max(Time.deltaTime, 0.0001f) : 1f;
            return value * mouseDegreesPerPixel / deltaTime;
        }
        return value * gamepadDegreesPerSecond;
    }
}
