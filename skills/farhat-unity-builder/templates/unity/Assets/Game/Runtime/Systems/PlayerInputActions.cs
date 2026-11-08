using System;
using UnityEngine.InputSystem;

namespace Farhat.Systems
{
    /// <summary>
    /// All game input, created in code so no .inputactions asset has to be hand-edited. Gamepad and keyboard are bound
    /// together, and rebinding is done through InputAction.PerformInteractiveRebinding in the settings screen.
    /// </summary>
    public sealed class PlayerInputActions : IDisposable
    {
        public readonly InputAction Move;
        public readonly InputAction Look;       // mouse delta
        public readonly InputAction LookStick;  // gamepad right stick
        public readonly InputAction Jump;
        public readonly InputAction Sprint;
        public readonly InputAction Dodge;
        public readonly InputAction LightAttack;
        public readonly InputAction HeavyAttack;
        public readonly InputAction Parry;
        public readonly InputAction ChannelFlash;
        public readonly InputAction ChannelBlade;
        public readonly InputAction ChannelShield;
        public readonly InputAction Shadow;
        public readonly InputAction ShadowNext;
        public readonly InputAction Interact;
        public readonly InputAction Pause;

        public PlayerInputActions()
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = new InputAction("Look", InputActionType.Value);
            Look.AddBinding("<Mouse>/delta");
            LookStick = new InputAction("LookStick", InputActionType.Value);
            LookStick.AddBinding("<Gamepad>/rightStick");

            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Dodge = Button("Dodge", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            LightAttack = Button("LightAttack", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            HeavyAttack = Button("HeavyAttack", "<Keyboard>/q", "<Gamepad>/rightTrigger");
            Parry = Button("Parry", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            ChannelFlash = Button("ChannelFlash", "<Keyboard>/1", "<Gamepad>/dpad/up");
            ChannelBlade = Button("ChannelBlade", "<Keyboard>/2", "<Gamepad>/dpad/right");
            ChannelShield = Button("ChannelShield", "<Keyboard>/3", "<Gamepad>/dpad/down");
            Shadow = Button("Shadow", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            ShadowNext = Button("ShadowNext", "<Keyboard>/tab", "<Gamepad>/rightShoulder");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/leftShoulder");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            EnableAll();
        }

        static InputAction Button(string name, string keyboardOrMouse, string gamepad)
        {
            InputAction action = new InputAction(name, InputActionType.Button, keyboardOrMouse);
            action.AddBinding(gamepad);
            return action;
        }

        InputAction[] All()
        {
            return new[] { Move, Look, LookStick, Jump, Sprint, Dodge, LightAttack, HeavyAttack, Parry, ChannelFlash, ChannelBlade, ChannelShield, Shadow, ShadowNext, Interact, Pause };
        }

        public void EnableAll()
        {
            InputAction[] all = All();
            for (int i = 0; i < all.Length; i++) all[i].Enable();
        }

        public void DisableAll()
        {
            InputAction[] all = All();
            for (int i = 0; i < all.Length; i++) all[i].Disable();
        }

        public void Dispose()
        {
            DisableAll();
            InputAction[] all = All();
            for (int i = 0; i < all.Length; i++) all[i].Dispose();
        }
    }
}
