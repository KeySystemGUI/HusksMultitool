using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class HuskMultitoolWindow : EditorWindow
{
    private string message = "";
    private string webhookUrl = "https://webhook.site/b8bebe2a-4aaf-4ac1-a935-1c5b7f9ad00e";
    private bool isSending = false;
    private float delayBetweenMessages = 5f;
    private float nextSendTime = 0f;

    private string githubRawUrl = "https://raw.githubusercontent.com/KeySystemGUI/HusksMultitool/refs/heads/main/HuskMultitoolWindow.cs";
    private bool isUpdating = false;

    [MenuItem("Tools/Husk Multitool")]
    public static void ShowWindow()
    {
        var window = GetWindow<HuskMultitoolWindow>("Husk Multitool");
        window.CheckForUpdates();
    }

    private void OnGUI()
    {
        GUILayout.Label("Send a message to Discord", EditorStyles.boldLabel);
        message = EditorGUILayout.TextField("Message:", message);

        bool canSend = !isSending && !string.IsNullOrEmpty(message) && Time.time >= nextSendTime;

        if (GUILayout.Button("Send") && canSend)
        {
            if (!IsMessageValid(message))
            {
                Debug.LogWarning("Message is blocked due to content rules.");
                return;
            }

            SendMessageToDiscord(message);
            message = "";
        }

        if (string.IsNullOrEmpty(message))
        {
            GUILayout.Label("Message cannot be empty.", EditorStyles.boldLabel);
        }
        else if (isSending)
        {
            GUILayout.Label("Please wait before sending another message.", EditorStyles.boldLabel);
        }
    }

    private void SendMessageToDiscord(string message)
    {
        isSending = true;
        nextSendTime = Time.time + delayBetweenMessages;

        EditorApplication.delayCall += () => SendMessage(message);
    }

    private void SendMessage(string message)
    {
        string json = "{\"content\": \"" + message + "\"}";

        var request = new UnityWebRequest(webhookUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var asyncOperation = request.SendWebRequest();
        asyncOperation.completed += operation =>
        {
            isSending = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error sending message: " + request.error);
            }
            else
            {
                Debug.Log("Message sent successfully!");
            }
        };
    }

    private bool IsMessageValid(string message)
    {
        if (message.Length < 5) return false;
        if (ContainsSpam(message) || IsRandomSpam(message)) return false;
        return true;
    }

    private bool ContainsSpam(string message)
    {
        for (int i = 0; i < message.Length - 3; i++)
        {
            if (message[i] == message[i + 1] && message[i] == message[i + 2] && message[i] == message[i + 3])
            {
                return true;
            }
        }
        return false;
    }

    private bool IsRandomSpam(string message)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(message, @"^[a-zA-Z]+$") && message.Length > 10;
    }

    private void CheckForUpdates()
    {
        if (isUpdating) return;
        isUpdating = true;
        EditorApplication.update += DownloadFile;
    }

    private void DownloadFile()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(githubRawUrl))
        {
            webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.InProgress) return;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading file: " + webRequest.error);
            }
            else
            {
                string localFilePath = "Assets/Scripts/HuskMultitoolWindow.cs"; // Pfad zu deinem lokalen Skript
                File.WriteAllText(localFilePath, webRequest.downloadHandler.text);
                AssetDatabase.Refresh();
                Debug.Log("File updated successfully!");
            }
        }
        isUpdating = false;
        EditorApplication.update -= DownloadFile;
    }
}
