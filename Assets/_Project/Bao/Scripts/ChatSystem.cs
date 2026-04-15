using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System;

public class ChatSystem : NetworkBehaviour
{
    public TextMeshProUGUI textMessage;
    public TMP_InputField inputFieldMessage;
    public Button buttonSend;

    //Chạy ngay sau khi nhân vật được spawn trong network
    public override void Spawned()
    {
        textMessage = GameObject.Find("TextMessage").GetComponent<TextMeshProUGUI>();
        inputFieldMessage = GameObject.Find("InputFieldMessage").GetComponent<TMP_InputField>();
        buttonSend = GameObject.Find("ButtonSend").GetComponent<Button>();
        buttonSend.onClick.AddListener(SendMessageChat);
    }

    public void SendMessageChat()
    {
        var message = inputFieldMessage.text;
        if (string.IsNullOrWhiteSpace(message)) return;
        var id = Runner.LocalPlayer.PlayerId;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var nameColor = GetPlayerColorHex(id);
        var text = $"[{timestamp}] <color=#{nameColor}>Player {id}</color>: {message}";
        RpcChat(text);
        inputFieldMessage.text = "";
    }

    private string GetPlayerColorHex(int playerId)
    {
        // Golden ratio spread gives distinct, stable colors per player id.
        var hue = (playerId * 0.61803398875f) % 1f;
        var color = Color.HSVToRGB(hue, 0.65f, 1f);
        return ColorUtility.ToHtmlStringRGB(color);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcChat(string message)
    {
        textMessage.text += message + "\n";
    }
}
