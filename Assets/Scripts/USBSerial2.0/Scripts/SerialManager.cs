using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class SerialManager : MonoBehaviour
{
    [Header("Serial Connection")]
    [SerializeField] private SerialConnection serialConnection;
    
    [Header("Input Controls")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendStringButton;
    [SerializeField] private Button sendCharButton;
    [SerializeField] private Button sendBytesButton;
    [SerializeField] private Button sendValuesButton;
    [SerializeField] private Button clearButton;
    
    [Header("Connection Controls")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Image connectionIndicator;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;
    
    [Header("Test Values")]
    [SerializeField] private int[] testValues = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
    
    [Header("Output Display")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ScrollRect outputScrollRect;
    [SerializeField] private int maxLogLines = 100;
    
    private Queue<string> logLines = new Queue<string>();
    private bool needsScrollUpdate = false;

    private void Start()
    {
        // Set up button listeners
        sendStringButton.onClick.AddListener(OnSendStringClicked);
        sendCharButton.onClick.AddListener(OnSendCharClicked);
        sendBytesButton.onClick.AddListener(OnSendBytesClicked);
        sendValuesButton.onClick.AddListener(OnSendValuesClicked);
        clearButton.onClick.AddListener(ClearMessage);
        
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);

        // Register for events
        SerialConnection.OnDataReceived += OnSerialDataReceived;
        SerialConnection.OnConnectionStateChanged += OnConnectionStateChanged;

        SendStringButton.OnSendString += OnSendStringClicked;

        // Initialize output
        UpdateOutputText("Serial interface ready.");
        
        // Initialize UI state
        UpdateConnectionUI(serialConnection != null && serialConnection.IsConnected);
    }

    private void OnDestroy()
    {
        // Unregister event listeners
        SerialConnection.OnDataReceived -= OnSerialDataReceived;
        SerialConnection.OnConnectionStateChanged -= OnConnectionStateChanged;

        SendStringButton.OnSendString -= OnSendStringClicked;
    }

    public void ClearMessage()
    {
        // Clear both the UI text and the log history queue
        outputText.text = string.Empty;
        logLines.Clear();
        
        // Add a "cleared" message
        UpdateOutputText("Log cleared.");
    }
    
    private void OnConnectClicked()
    {
        if (serialConnection != null)
        {
            bool success = serialConnection.Connect();
            if (success)
                UpdateOutputText("Connection initiated...");
            else
                UpdateOutputText("Connection attempt initiated. Waiting for permission...");
        }
    }
    
    private void OnDisconnectClicked()
    {
        if (serialConnection != null)
        {
            serialConnection.Disconnect();
        }
    }
    
    private void OnConnectionStateChanged(bool connected)
    {
        UpdateConnectionUI(connected);
        UpdateOutputText(connected ? 
            "Connected to serial device." : 
            "Disconnected from serial device.");
    }
    
    private void UpdateConnectionUI(bool connected)
    {
        if (connectionIndicator != null)
            connectionIndicator.color = connected ? connectedColor : disconnectedColor;
            
        if (connectButton != null)
            connectButton.interactable = !connected;
            
        if (disconnectButton != null)
            disconnectButton.interactable = connected;
            
        // Enable/disable controls based on connection state
        sendStringButton.interactable = connected;
        sendCharButton.interactable = connected;
        sendBytesButton.interactable = connected;
        sendValuesButton.interactable = connected;
    }

    private void OnSendStringClicked()
    {
        OnSendStringClicked(inputField.text);
    }

    public void OnSendStringClicked(string input)
    {
        Debug.Log($"<Color=red>Sending string: {input}</Color>");
        if (!string.IsNullOrEmpty(input) && serialConnection != null)
        {
            serialConnection.SendString(input);
            UpdateOutputText($"<color=#00FF00>[TX String]</color> {input}");
        }
    }

    private void OnSendCharClicked()
    {
        string input = inputField.text;
        if (!string.IsNullOrEmpty(input) && serialConnection != null)
        {
            serialConnection.SendChar(input[0]);
            UpdateOutputText($"<color=#00FF00>[TX Char]</color> {input[0]}");
        }
    }

    private void OnSendBytesClicked()
    {
        string input = inputField.text;
        if (!string.IsNullOrEmpty(input) && serialConnection != null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            serialConnection.SendBytes(bytes);
            UpdateOutputText($"<color=#00FF00>[TX Bytes]</color> Length: {bytes.Length}");
        }
    }

private void OnSendValuesClicked()
{
    if (serialConnection == null) return;

    string raw = inputField.text.Trim();
    int[] values = new int[8]; // Always use 8 values for motors

    if (string.IsNullOrEmpty(raw))
    {
        // Use default values (all zeros)
        values = testValues;
    }
    else
    {
        // Split on any whitespace (spaces, tabs, etc.)
        string[] parts = System.Text.RegularExpressions.Regex.Split(raw, @"\s+");
        
        // Parse the values, clamping to valid motor range (-128 to 127)
        for (int i = 0; i < 8; i++)
        {
            // If we have enough parts, parse them
            if (i < parts.Length && int.TryParse(parts[i], out int val))
            {
                // Clamp to valid motor range as the Pico code does
                values[i] = Mathf.Clamp(val, -128, 127);
            }
            else
            {
                // For missing values, use 0
                values[i] = 0;
            }
        }
    }

    // Build space-separated string of values that Pico expects
    var sb = new StringBuilder();
    for (int i = 0; i < values.Length; i++)
    {
        sb.Append(values[i]);
        if (i < values.Length - 1) sb.Append(' ');
    }
    sb.Append('\n'); // Add newline for fgets in Pico code
    string payload = sb.ToString();

    // Send as text over USB-CDC
    serialConnection.SendString(payload, false); // Use false to avoid double newlines

    UpdateOutputText($"<color=#00FF00>[TX Motors]</color> {string.Join(" ", values)}");
}
    
    private void OnSerialDataReceived(string message)
    {
        // This will be called from another thread, so we need to use the main thread
        UpdateOutputText($"<color=#00FFFF>[RX]</color> {message}");
    }
    
    private void UpdateOutputText(string newLine)
    {
        // Check if user was already scrolled to bottom before adding new text
        bool wasAtBottom = false;
        if (outputScrollRect != null)
        {
            // If scrollbar is within 0.01 of bottom, consider it "at bottom"
            wasAtBottom = outputScrollRect.verticalNormalizedPosition <= 0.01f;
        }
        
        // Add the new line to our queue
        logLines.Enqueue(newLine);
        
        // Trim if we exceed max lines
        while (logLines.Count > maxLogLines)
        {
            logLines.Dequeue();
        }
        
        // Update the text
        outputText.text = string.Join("\n", logLines);
        
        // Only auto-scroll if user was already at the bottom
        needsScrollUpdate = wasAtBottom;
    }
    
    private void LateUpdate()
    {
        // Ensure we scroll to bottom after rendering
        if (needsScrollUpdate && outputScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            outputScrollRect.verticalNormalizedPosition = 0f;
            needsScrollUpdate = false;
        }
    }

    // You may also want to add a manual "scroll to bottom" button
    public void ScrollToBottom()
    {
        if (outputScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            outputScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
