using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class NetworkManager : MonoBehaviour
{
    public static bool isHost = false;
    public static string Name;
    public static string IPAddress = null;
    public static ushort IPPort = 9000;

    [SerializeField] TMP_InputField nameText;
    [SerializeField] TMP_InputField IPAddressText;

    public static string FailReason = "";
    [SerializeField] TMP_Text ErrorText;

    private void Start()
    {
        ErrorText.text = FailReason;

        FailReason = "";
    }

    public void StartHost()
    {
        if (string.IsNullOrEmpty(nameText.text))
        {
            ErrorText.text = "Empty name!";
            return;
        }

        isHost = true;
        Name = nameText.text;
        SceneManager.LoadScene(1);
    }

    public void StartClient()
    {
        if (string.IsNullOrWhiteSpace(nameText.text))
        {
            ErrorText.text = "Empty name!";
            return;
        }

        if (!new Regex("^(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")
            .IsMatch(IPAddressText.text))
        {
            ErrorText.text = "Wrong IP adress!";
            return;
        }

        isHost = false;
        Name = nameText.text;
        IPAddress = IPAddressText.text;
        SceneManager.LoadScene(1);
    }
}