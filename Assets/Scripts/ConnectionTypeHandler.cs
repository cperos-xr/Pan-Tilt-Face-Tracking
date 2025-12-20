using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionTypeHandler : MonoBehaviour
{
    [Header("References")]
    public SerialConnection serialConnection;
    public BleManager bleManager;
    public PanTiltController panTiltController;

    private ConnectionType currentConnectionType = ConnectionType.Serial; // Default to Serial
    private bool isConnected = false;
    private string lastAddress = "";

    public ConnectionType CurrentConnectionType => currentConnectionType;
    public bool IsConnected => isConnected;

    public void SetConnectionType(ConnectionType type)
    {
        currentConnectionType = type;
    }

    public void SetAddress(string address)
    {
        lastAddress = address;
    }

    public void Connect()
    {
        switch (currentConnectionType)
        {
            case ConnectionType.Serial:
                if (serialConnection != null)
                {
                    isConnected = serialConnection.Connect();
                }
                break;
            case ConnectionType.BLE:
                if (bleManager != null)
                {
                    bleManager.Connect(lastAddress);
                    // BLE connect is async, so we can't set isConnected immediately
                }
                break;
        }
    }

    public void Disconnect()
    {
        switch (currentConnectionType)
        {
            case ConnectionType.Serial:
                if (serialConnection != null)
                {
                    serialConnection.Disconnect();
                    isConnected = false;
                }
                break;
            case ConnectionType.BLE:
                if (bleManager != null)
                {
                    bleManager.Disconnect();
                    isConnected = false;
                }
                break;
        }
    }

    void Update()
    {
        // Poll connection state for BLE (Serial is immediate)
        switch (currentConnectionType)
        {
            case ConnectionType.Serial:
                isConnected = serialConnection != null && serialConnection.IsConnected;
                break;
            case ConnectionType.BLE:
                isConnected = bleManager != null && bleManager.IsConnected;
                break;
        }
    }

    // Call this to send a command from PanTiltController
    public void SendPanTiltCommand(string json)
    {
        if (!isConnected) return;
        switch (currentConnectionType)
        {
            case ConnectionType.Serial:
                if (serialConnection != null)
                    serialConnection.SendString(json);
                break;
            case ConnectionType.BLE:
                if (bleManager != null)
                    bleManager.SendJsonLine(json);
                break;
        }
    }
}

[Serializable]
public enum ConnectionType
{
    Serial,
    BLE
}