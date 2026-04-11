using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public GameObject menuUI;
    public GameObject lobbyUI;
    public GameObject roomCreateUI;

    private NetworkRunner _runner;

    private void Update()
    {
        // Liên tục tìm Runner cho đến khi thấy thì đăng ký
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner != null)
            {
                _runner.AddCallbacks(this);
            }
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogError($"🔴 [UIManager] ĐÃ CHẠY VÀO ONSHUTDOWN! LÝ DO: {shutdownReason}");
        
        // Cố tình Delay 0.1 giây để đợi các script khác tắt UI hoặc Fusion dọn dẹp xong
        Invoke(nameof(ForceShowMenu), 0.1f);
    }

    // --- ĐÂY CHÍNH LÀ ĐOẠN ĐÃ ĐƯỢC CẬP NHẬT ---
    private void ForceShowMenu()
    {
        // Tắt các giao diện phòng
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (roomCreateUI != null) roomCreateUI.SetActive(false);
        
        // Bật lại MenuUI và toàn bộ các nút con bên trong
        if (menuUI != null) 
        {
            menuUI.SetActive(true); // Bật thằng cha
            
            // Duyệt qua tất cả các thằng con (Start, Settings, Exit...) và ép bật lên
            foreach (Transform child in menuUI.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        
        Debug.Log("✅ [UIManager] Đã ép bật lại MenuUI và toàn bộ các nút con!");
    }

    // --- CÁC HÀM BẮT BUỘC (Để trống) ---
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}