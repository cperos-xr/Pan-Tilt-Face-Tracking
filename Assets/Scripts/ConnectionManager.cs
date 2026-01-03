using System;
using UnityEngine;

[Serializable]
public class ConnectionDataSet
{
    public string SdpData;
    public string IceData;
}

public class ConnectionManager : MonoBehaviour
{
    public ConnectionDataSet completeConnectionDataSet;

    public GameObject readQRPanel;

    public GameObject ConnectionEstablishedPanel;

    public delegate void OnConnectionDataCompleted(ConnectionDataSet connectionData);
    public static event OnConnectionDataCompleted ConnectionDataCompleted;

    void Start()
    {
        completeConnectionDataSet.SdpData = "";
        completeConnectionDataSet.IceData = "";
    }

    void OnEnable()
    {
        QRCodeScanner.QRCodeRead += OnQRCodeRead;
    }

    void OnDisable()
    {
        QRCodeScanner.QRCodeRead -= OnQRCodeRead;
    }

    private void OnQRCodeRead(ConnectionData connectionData)
    {
        if (connectionData.type == QRCodeType.sdp)
        {
            completeConnectionDataSet.SdpData = connectionData.data;
            Debug.Log("SDP Data received: " + connectionData.data);

        }
        else if (connectionData.type == QRCodeType.ice)
        {
            completeConnectionDataSet.IceData = connectionData.data;
            Debug.Log("ICE Data received: " + connectionData.data);
        }

        if (!string.IsNullOrEmpty(completeConnectionDataSet.SdpData) && !string.IsNullOrEmpty(completeConnectionDataSet.IceData))
        {
            Debug.Log("Both SDP and ICE data received. Ready to establish connection.");
            readQRPanel.SetActive(false);
            ConnectionEstablishedPanel.SetActive(true);
            ConnectionDataCompleted?.Invoke(completeConnectionDataSet);
            // Proceed with connection establishment logic here
        }
    }


}
