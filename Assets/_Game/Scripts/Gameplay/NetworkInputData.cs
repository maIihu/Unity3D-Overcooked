using Fusion;

public struct NetworkInputData : INetworkInput
{
    // sbyte (-128..127) thay vì float (4 bytes) — chỉ cần -1, 0, 1 cho 8-direction movement
    // Tiết kiệm 3 bytes/field × 2 = 6 bytes/tick/player → ~360 bytes/s/player ở 60Hz
    public sbyte MoveX;
    public sbyte MoveY;
    public byte Buttons;

    public const byte INTERACT  = 1 << 0;
    public const byte ALTERNATE = 1 << 1;

    public bool IsInteractPressed  => (Buttons & INTERACT)  != 0;
    public bool IsAlternatePressed => (Buttons & ALTERNATE) != 0;
}