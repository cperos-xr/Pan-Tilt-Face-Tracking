using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles only the UI for connection controls, delegates logic to ConnectionTypeHandler
public class ConnectionUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown connectionTypeDropdown;
    public Button connectButton;
    public TextMeshProUGUI connectButtonText;
    public TextMeshProUGUI connectionStatusText;

    [Header("Logic Reference")]
    public ConnectionTypeHandler connectionTypeHandler;

    [Header("BLE Settings")]
    [Tooltip("BLE device address to use when connecting (e.g. AA:BB:CC:DD:EE:FF)")]
    public string bleAddress = "";

    void Start()
    {
        if (connectionTypeDropdown != null)
        {
            // Set dropdown options to match ConnectionType enum
            connectionTypeDropdown.ClearOptions();
            connectionTypeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Serial", "BLE" });
            connectionTypeDropdown.onValueChanged.AddListener(OnConnectionTypeChanged);
            connectionTypeDropdown.value = (int)connectionTypeHandler.CurrentConnectionType;
        }
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonPressed);
        }
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void OnConnectionTypeChanged(int idx)
    {
        connectionTypeHandler.SetConnectionType((ConnectionType)idx);
        UpdateUI();
    }

    private void OnConnectButtonPressed()
    {
        if (!connectionTypeHandler.IsConnected)
        {
            // If BLE is selected, set the address from Inspector
            if (connectionTypeHandler.CurrentConnectionType == ConnectionType.BLE)
            {
                connectionTypeHandler.SetAddress(bleAddress);
            }
            connectionTypeHandler.Connect();
        }
        else
        {
            connectionTypeHandler.Disconnect();
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (connectButtonText != null)
            connectButtonText.text = connectionTypeHandler.IsConnected ? "Disconnect" : "Connect";
        if (connectionStatusText != null)
        {
            connectionStatusText.text = connectionTypeHandler.IsConnected
                ? (connectionTypeHandler.CurrentConnectionType == ConnectionType.Serial ? "Serial Connected" : "BLE Connected")
                : (connectionTypeHandler.CurrentConnectionType == ConnectionType.Serial ? "Serial Disconnected" : "BLE Disconnected");
        }
    }
}
