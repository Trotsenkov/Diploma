using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public static bool isHost = false;
    public static string Name;
    public static string IPAddress = null;
    public static ushort IPPort = 9000;

    [SerializeField] TMP_InputField nameText;
    [SerializeField] TMP_InputField IPAddressText;
    public void StartHost()
    {
        isHost = true;
        Name = nameText.text;
        SceneManager.LoadScene(1);
    }

    public void StartClient()
    {
        isHost = false;
        Name = nameText.text;
        IPAddress = IPAddressText.text;
        SceneManager.LoadScene(1);
    }
}