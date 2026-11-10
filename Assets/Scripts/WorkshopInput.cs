using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jiaozi
{
    public sealed class WorkshopInput : IDisposable
    {
        readonly InputActionMap map;
        readonly InputAction[] move = new InputAction[2], interact = new InputAction[2], work = new InputAction[2];
        public readonly InputAction Pause, Confirm, Restart, Up, Down;
        public WorkshopInput()
        {
            map = new InputActionMap("Workshop");
            for (int i = 0; i < 2; i++)
            {
                move[i] = map.AddAction("Move" + i, InputActionType.Value);
                move[i].AddCompositeBinding("2DVector").With("Up", i == 0 ? "<Keyboard>/w" : "<Keyboard>/upArrow").With("Down", i == 0 ? "<Keyboard>/s" : "<Keyboard>/downArrow").With("Left", i == 0 ? "<Keyboard>/a" : "<Keyboard>/leftArrow").With("Right", i == 0 ? "<Keyboard>/d" : "<Keyboard>/rightArrow");
                interact[i] = map.AddAction("Interact" + i, InputActionType.Button, i == 0 ? "<Keyboard>/f" : "<Keyboard>/slash");
                work[i] = map.AddAction("Work" + i, InputActionType.Button, i == 0 ? "<Keyboard>/g" : "<Keyboard>/period");
            }

            Pause = map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            Confirm = map.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
            Restart = map.AddAction("Restart", InputActionType.Button, "<Keyboard>/r");
            Up = map.AddAction("Up", InputActionType.Button, "<Keyboard>/upArrow");
            Down = map.AddAction("Down", InputActionType.Button, "<Keyboard>/downArrow");
            map.Enable();
        }

        public Vector2 Move(int i) => Vector2.ClampMagnitude(move[i].ReadValue<Vector2>(), 1);
        public bool Interact(int i) => interact[i].WasPressedThisFrame();
        public bool Work(int i) => work[i].IsPressed();
        public void Dispose()
        {
            map.Disable();
            map.Dispose();
        }
    }
}
