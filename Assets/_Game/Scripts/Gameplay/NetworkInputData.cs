using Fusion;

public struct NetworkInputData : INetworkInput
{
    public sbyte MoveX;
    public sbyte MoveY;
    public byte Buttons;

    public const byte INTERACT  = 1 << 0;
    public const byte ALTERNATE = 1 << 1;

    public bool IsInteractPressed  => (Buttons & INTERACT)  != 0;
    public bool IsAlternatePressed => (Buttons & ALTERNATE) != 0;
}