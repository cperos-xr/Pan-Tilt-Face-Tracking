using UnityEngine;
using System;
using System.Text;
using System.Collections;

public class SerialConnection : MonoBehaviour
{
    // Events
    public delegate void DataReceivedHandler(string message);
    public static event DataReceivedHandler OnDataReceived;
    
    public delegate void ConnectionStateChangedHandler(bool connected);
    public static event ConnectionStateChangedHandler OnConnectionStateChanged;

    // Configuration
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private float readInterval = 0.1f;
    [SerializeField] private int readBufferSize = 1024;
    [SerializeField] private float reconnectDelay = 3.0f;
    [SerializeField] private bool appendNewLine = true;
    
    // State
    private bool isConnected = false;
    private float readTimer = 0f;
    private StringBuilder messageBuffer = new StringBuilder();
    
    // Properties
    public bool IsConnected => isConnected;

    private void Start()
    {
        if (autoConnect)
        {
            Connect();
        }
    }

    public bool Connect()
    {
        UsbSerial.Init();
        Debug.Log("USB Serial initialized, attempting to open connection...");

        try
        {
            isConnected = UsbSerial.Open();
            Debug.Log("Connection attempt result: " + isConnected);

            if (!isConnected)
            {
                Debug.LogWarning("USB serial not opened. Permission may have been requested.");
                StartCoroutine(RetryConnectAfterDelay(reconnectDelay));
                return false;
            }
            
            OnConnectionStateChanged?.Invoke(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Exception during Connect: " + e.Message + "\n" + e.StackTrace);
            StartCoroutine(RetryConnectAfterDelay(reconnectDelay));
            return false;
        }
    }

    public void Disconnect()
    {
        if (isConnected)
        {
            try
            {
                UsbSerial.Close();
                isConnected = false;
                OnConnectionStateChanged?.Invoke(false);
                Debug.Log("Serial connection closed.");
            }
            catch (Exception e)
            {
                Debug.LogError("Error disconnecting: " + e.Message);
            }
        }
    }

    private IEnumerator RetryConnectAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log("Retrying connection after delay...");
        Connect();
    }

    // General purpose sending methods
    public void SendString(string message, bool addNewLine = true)
    {
        if (!isConnected) return;

        try
        {
            Debug.Log("Sending string: " + message);
            string dataToSend = addNewLine && appendNewLine ? message + "\r\n" : message;
            byte[] data = Encoding.UTF8.GetBytes(dataToSend);
            UsbSerial.Write(data);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending string: " + e.Message);
            HandleSendError();
        }
    }

    public void SendChar(char character)
    {
        if (!isConnected) return;

        try
        {
            byte[] data = new byte[] { (byte)character };
            UsbSerial.Write(data);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending char: " + e.Message);
            HandleSendError();
        }
    }

    public void SendBytes(byte[] data)
    {
        if (!isConnected) return;

        try
        {
            UsbSerial.Write(data);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending bytes: " + e.Message);
            HandleSendError();
        }
    }

    public void SendValues(int[] values)
    {
        if (!isConnected) return;

        try
        {
            // Convert to string with comma separation
            string valuesStr = string.Join(",", values);
            SendString(valuesStr);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending values: " + e.Message);
            HandleSendError();
        }
    }

    private void HandleSendError()
    {
        isConnected = false;
        OnConnectionStateChanged?.Invoke(false);
        StartCoroutine(RetryConnectAfterDelay(reconnectDelay));
    }

    private void Update()
    {
        if (!isConnected) return;

        // Use a timer to control read frequency
        readTimer += Time.deltaTime;
        if (readTimer < readInterval) return;

        readTimer = 0f;
        PollSerialData();
    }

    private void PollSerialData()
    {
        try
        {
            byte[] incoming = UsbSerial.Read(readBufferSize);
            if (incoming.Length > 0)
            {
                string text = Encoding.UTF8.GetString(incoming);
                
                // Append to buffer and process complete lines
                messageBuffer.Append(text);
                string bufferContent = messageBuffer.ToString();

                // Process any complete lines
                int newlineIndex = bufferContent.IndexOf('\n');
                while (newlineIndex >= 0)
                {
                    string line = bufferContent.Substring(0, newlineIndex).Trim('\r');
                    
                    // Fire event with complete line
                    OnDataReceived?.Invoke(line);

                    // Remove processed line from buffer
                    bufferContent = bufferContent.Substring(newlineIndex + 1);
                    newlineIndex = bufferContent.IndexOf('\n');
                }

                // Store remaining incomplete data
                messageBuffer.Clear();
                messageBuffer.Append(bufferContent);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Serial read error: " + e.Message);
            isConnected = false;
            OnConnectionStateChanged?.Invoke(false);
            StartCoroutine(RetryConnectAfterDelay(reconnectDelay));
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}