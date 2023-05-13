using UnityEngine;
using Unity.Networking.Transport;
using System.Collections.Generic;
using System.Linq;

public class Client : MonoBehaviour
{
    private byte colorCode;
    private Player player;
    private Player playerPrefab;
    private List<Player> players = new List<Player>();

    [SerializeField] private string connectionAdress;
    [SerializeField] private ushort connectionPort;

    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool Connected;

    private void Awake()
    {
        playerPrefab = Resources.Load<Player>("Player");
    }

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.Parse(connectionAdress, connectionPort);
        m_Connection = m_Driver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server");
                m_Driver.SendSSPMessage(m_Connection, new ConnectReq() { Name = "Vasya" });
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                Message message = stream.RecieveSSPMessage();

                if (message is ConnectAck)
                {
                    Debug.Log("Connect Ack!");
                    colorCode = ((ConnectAck)message).ColorCode;

                    m_Driver.SendSSPMessage(m_Connection, new ConnectOk());
                    Connected = true;
                }
                else if (message is UpdatePlayersList)
                {
                    Debug.Log("UpdatePlayersList!");
                    UpdatePlayersList msg = (UpdatePlayersList)message;
                    foreach(System.Tuple<string, byte> data in msg.playerDatas)
                    {
                        if (players.Select(player => player.colorCode).Contains(data.Item2))
                            continue;

                        Player player = Instantiate(playerPrefab);
                        player.Name = data.Item1;
                        player.colorCode = data.Item2;

                        if (colorCode == player.colorCode)
                        {
                            player.local = true;
                            this.player = player;
                        }
                        players.Add(player);
                    }
                }

                else if (message is CommandLook)
                {
                    CommandLook msg = (CommandLook)message;
                    players.Find(player => player.colorCode == msg.playerCode).transform.rotation = Quaternion.Euler(0, 0, msg.rotationZ);
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
        }
    }

    private void FixedUpdate()
    {
        //m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated || !Connected)
        {
            return;
        }

        m_Driver.SendSSPMessage(m_Connection, new CommandMove()
        {
            direction = new Vector2(Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0, Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0)
        });
        m_Driver.SendSSPMessage(m_Connection, new CommandLook() { playerCode = player.colorCode, rotationZ = player.transform.rotation.eulerAngles.z });
    }
}