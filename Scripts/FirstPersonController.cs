// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

/// <summary>Eye-level movement constrained to the clear bartender aisle. Keyboard/mouse and touch share one camera.</summary>
public partial class FirstPersonController : Node3D
{
    public static readonly Vector3 StartPosition = new(-.1f, 1.72f, 2.55f);
    public Camera3D Camera { get; private set; } = null!;
    public event Action? ToggleHud;
    private float _yaw;
    private float _pitch = -.24f;
    private int _moveTouch = -1;
    private int _lookTouch = -1;
    private Vector2 _touchOrigin;
    private Vector2 _touchMove;

    public override void _Ready()
    {
        Name = "Bartender";
        Position = StartPosition;
        Camera = new Camera3D { Name = "Eyes", Current = true, Fov = 70, Near = .035f, Far = 80 };
        AddChild(Camera);
        ApplyLook();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        switch (input)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } when _moveTouch < 0 && _lookTouch < 0:
                Input.MouseMode = Input.MouseModeEnum.Captured;
                break;
            case InputEventMouseMotion motion when Input.MouseMode == Input.MouseModeEnum.Captured:
                Look(motion.Relative * .0022f);
                break;
            case InputEventKey { Pressed: true, Echo: false } key:
                switch (key.PhysicalKeycode)
                {
                    case Key.Escape: Input.MouseMode = Input.MouseModeEnum.Visible; break;
                    case Key.B: TurnAround(); break;
                    case Key.R: ResetView(); break;
                    case Key.H: ToggleHud?.Invoke(); break;
                    case Key.F11:
                        DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
                            ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
                        break;
                }
                break;
            case InputEventScreenTouch touch:
                if (touch.Pressed)
                {
                    if (touch.Position.X < GetViewport().GetVisibleRect().Size.X * .38f && _moveTouch < 0)
                    {
                        _moveTouch = touch.Index;
                        _touchOrigin = touch.Position;
                    }
                    else if (_lookTouch < 0) _lookTouch = touch.Index;
                }
                else
                {
                    if (touch.Index == _moveTouch) { _moveTouch = -1; _touchMove = Vector2.Zero; }
                    if (touch.Index == _lookTouch) _lookTouch = -1;
                }
                break;
            case InputEventScreenDrag drag:
                if (drag.Index == _moveTouch) _touchMove = ((drag.Position - _touchOrigin) / 90f).LimitLength();
                if (drag.Index == _lookTouch) Look(drag.Relative * .0035f);
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (!GetWindow().HasFocus()) return;
        var movement = _touchMove;
        if (Input.IsPhysicalKeyPressed(Key.W)) movement.Y -= 1;
        if (Input.IsPhysicalKeyPressed(Key.S)) movement.Y += 1;
        if (Input.IsPhysicalKeyPressed(Key.A)) movement.X -= 1;
        if (Input.IsPhysicalKeyPressed(Key.D)) movement.X += 1;
        movement = movement.LimitLength();
        var direction = new Vector3(movement.X, 0, movement.Y).Rotated(Vector3.Up, _yaw);
        var next = Position + direction * (float)delta * 1.45f;
        Position = new(Mathf.Clamp(next.X, -3.6f, 3.6f), StartPosition.Y, Mathf.Clamp(next.Z, 1.63f, 3.22f));

        var look = Vector2.Zero;
        if (Input.IsPhysicalKeyPressed(Key.Left)) look.X -= 1;
        if (Input.IsPhysicalKeyPressed(Key.Right)) look.X += 1;
        if (Input.IsPhysicalKeyPressed(Key.Up)) look.Y -= 1;
        if (Input.IsPhysicalKeyPressed(Key.Down)) look.Y += 1;
        if (look != Vector2.Zero) Look(look * (float)delta * 1.25f);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationApplicationFocusOut) return;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _moveTouch = _lookTouch = -1;
        _touchMove = Vector2.Zero;
    }

    public void TurnAround()
    {
        _yaw += Mathf.Pi;
        _pitch = Mathf.Cos(_yaw) < 0 ? .14f : -.24f;
        ApplyLook();
    }

    public void ResetView()
    {
        Position = StartPosition;
        _yaw = 0;
        _pitch = -.24f;
        ApplyLook();
    }

    private void Look(Vector2 change)
    {
        _yaw -= change.X;
        _pitch = Mathf.Clamp(_pitch - change.Y, -1.1f, 1.1f);
        ApplyLook();
    }

    private void ApplyLook() => Camera.Rotation = new(_pitch, _yaw, 0);
}
