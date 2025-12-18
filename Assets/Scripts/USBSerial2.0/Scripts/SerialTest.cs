// Assets/SerialTest.cs
using UnityEngine;
using System.Text;
using System;
using System.Collections;

public class SerialTest : MonoBehaviour
{
    bool portOpen = false;
    private float readTimer = 0f;
    private float readInterval = 0.2f; // Try reading every 200ms instead of 100ms
    private StringBuilder messageBuffer = new StringBuilder();

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        UsbSerial.Init();
        Debug.Log("USB Serial initialized, attempting to open connection...");

        try
        {
            portOpen = UsbSerial.Open();
            Debug.Log("Open() method returned: " + portOpen);

            if (!portOpen)
            {
                Debug.LogWarning("USB serial not opened. Permission may have been requested. Will try again in 3 seconds...");
                
                // Create a coroutine to retry connection after a delay to allow user to grant permission
                StartCoroutine(RetryConnectAfterDelay(3.0f));
                return;
            }
            
            Debug.Log("Serial opened!");

            // Send a test message to pico with proper line ending
            byte[] testMessage = Encoding.ASCII.GetBytes("Hello Pico\r\n");
            UsbSerial.Write(testMessage);
            Debug.Log("Sent test message to Pico");
        }
        catch (Exception e)
        {
            Debug.LogError("Exception during Connect: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private IEnumerator RetryConnectAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log("Retrying connection after delay...");
        Connect();
    }

    void Update()
    {
        if (!portOpen) return;

        // Use a timer to control read frequency
        readTimer += Time.deltaTime;
        if (readTimer < readInterval) return;

        readTimer = 0f;

        try
        {
            byte[] incoming = UsbSerial.Read(1024); // Increase buffer size
            if (incoming.Length > 0)
            {
                string text = Encoding.ASCII.GetString(incoming);
                Debug.Log("Raw data: [" + BitConverter.ToString(incoming) + "]");
                Debug.Log("Raw text: '" + text + "'");

                // Append to buffer and process complete lines
                messageBuffer.Append(text);
                string bufferContent = messageBuffer.ToString();

                // Process any complete lines
                int newlineIndex = bufferContent.IndexOf('\n');
                while (newlineIndex >= 0)
                {
                    string line = bufferContent.Substring(0, newlineIndex).Trim('\r');
                    Debug.Log("Complete message: '" + line + "'");

                    // Remove processed line from buffer
                    bufferContent = bufferContent.Substring(newlineIndex + 1);
                    newlineIndex = bufferContent.IndexOf('\n');
                }

                // Store remaining incomplete data
                messageBuffer.Clear();
                messageBuffer.Append(bufferContent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Serial read error: " + e.Message);
        }
    }

    void OnDestroy()
    {
        if (portOpen)
        {
            UsbSerial.Close();
            Debug.Log("Serial closed.");
        }
    }
}
