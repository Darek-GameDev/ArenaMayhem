using UnityEngine;
using Fusion;

public class PlayerRunner : SimulationBehaviour,IPlayerJoined
{
    [SerializeField] private GameObject playerPrefab;   

    public void PlayerJoined(PlayerRef player)
    {
        if(player==Runner.LocalPlayer)
        {
            Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    
}
