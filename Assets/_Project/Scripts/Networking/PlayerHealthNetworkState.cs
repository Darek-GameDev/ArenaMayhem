using Fusion;

public struct PlayerHealthNetworkState : INetworkStruct
{
    public int Current;
    public int Max;
    public NetworkBool IsDead;
}
