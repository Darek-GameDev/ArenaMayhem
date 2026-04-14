using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaveRoomandRemoveRoom : MonoBehaviour
{
    private const string CreatedRoomIdPrefsKey = "CREATED_ROOM_ID";

    [Header("UI")]
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject menuUI;
    [SerializeField] private TMP_InputField roomIdTmpInputField;
    [SerializeField] private InputField roomIdLegacyInputField;

    [Header("Options")]
    [SerializeField] private bool clearRoomInputOnRemove = true;

    public void OnLeaveRoomClicked()
    {
        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
        if (sessionManager != null)
        {
            // Leave current Fusion/Photon room.
            sessionManager.LeaveRoom();
        }

        OpenMenuUI();
    }

    public void OnRemoveRoomClicked()
    {
        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
        if (sessionManager != null && sessionManager.HasActiveSession && !sessionManager.IsLocalPlayerOwner())
        {
            Debug.LogWarning("LeaveRoomandRemoveRoom: chi owner moi duoc xoa room.");
            return;
        }

        if (sessionManager != null)
        {
            sessionManager.RemoveRoomAndKickAll();
        }

        DeleteCreatedRoomId();
        ClearRoomInputIfNeeded();
        OpenMenuUI();
    }

    private static void DeleteCreatedRoomId()
    {
        if (!PlayerPrefs.HasKey(CreatedRoomIdPrefsKey))
        {
            return;
        }

        PlayerPrefs.DeleteKey(CreatedRoomIdPrefsKey);
        PlayerPrefs.Save();
    }

    private void ClearRoomInputIfNeeded()
    {
        if (!clearRoomInputOnRemove)
        {
            return;
        }

        if (roomIdTmpInputField != null)
        {
            roomIdTmpInputField.text = string.Empty;
        }

        if (roomIdLegacyInputField != null)
        {
            roomIdLegacyInputField.text = string.Empty;
        }
    }

    private void OpenMenuUI()
    {
        if (lobbyUI != null)
        {
            lobbyUI.SetActive(false);
        }

        if (menuUI != null)
        {
            menuUI.SetActive(true);
        }
    }
}
