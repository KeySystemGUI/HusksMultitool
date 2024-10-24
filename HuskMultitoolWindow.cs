using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class HuskMultitoolWindow : EditorWindow
{
    private string message = "";
    private string webhookUrl = "https://discord.com/api/webhooks/1299018014368206901/k-6K7GT5DHaX0JBwBqaaJz2G9280sTTywag4epabNsfytj1SKi8VZXYW4p5vvS9xVDsl";
    private bool isSending = false;
    private float delayBetweenMessages = 5f;
    private float nextSendTime = 0f;

    private string githubRawUrl = "https://raw.githubusercontent.com/KeySystemGUI/HusksMultitool/main/HuskMultitoolWindow.cs";
    private bool isUpdating = false;
    private UnityWebRequest currentRequest;

    [MenuItem("Tools/Husk Multitool")]
    public static void ShowWindow()
    {
        var window = GetWindow<HuskMultitoolWindow>("Husk Multitool");
        window.CheckForUpdates(); // Überprüfe auf Updates beim Öffnen des Fensters
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

        if (isUpdating)
        {
            GUILayout.Label("Checking for updates...", EditorStyles.boldLabel);
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
            if (message[i] == message[i + 1] && message[i] == message[i + 2] && message[i + 3] == message[i + 3])
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

        Debug.Log("Checking for updates...");
        currentRequest = UnityWebRequest.Get(githubRawUrl);
        currentRequest.SendWebRequest();
        EditorApplication.update += UpdateCheck;
    }

    private void UpdateCheck()
    {
        if (currentRequest.isDone)
        {
            if (currentRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading file: " + currentRequest.error);
            }
            else
            {
                string localFilePath = "Assets/Editor/HuskMultitoolWindow.cs"; // Pfad zum Editor-Ordner
                string remoteContent = currentRequest.downloadHandler.text;
                string localContent = File.Exists(localFilePath) ? File.ReadAllText(localFilePath) : "";

                if (remoteContent != localContent)
                {
                    File.WriteAllText(localFilePath, remoteContent);
                    AssetDatabase.Refresh();
                    Debug.Log("File updated successfully!");
                }
                else
                {
                    Debug.Log("Nothing new found.");
                }
            }

            isUpdating = false;
            currentRequest.Dispose();
            EditorApplication.update -= UpdateCheck;
        }
    }
}
