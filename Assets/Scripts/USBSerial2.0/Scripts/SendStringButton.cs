using UnityEngine;
using TMPro;

public class SendStringButton : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI displayText;

    [SerializeField] private string value;

    public delegate void SendString(string text);
    public static event SendString OnSendString;


    public void OnClick()
    {
        Debug.Log("Button clicked, value is " + value);
        OnSendString?.Invoke(value);
    }

    public void SaveValue()
    {
        value = inputField.text;
        displayText.text = value;
    }

}