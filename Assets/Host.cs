using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;

public class Host : MonoBehaviour
{
    private Player playerPrefab;
    private Player player;
    private List<Player> players = new List<Player>(); //delete
    private List<byte> freeColorCodes = new (); 

    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections; //delete
    private Dictionary<NetworkConnection, Player> clients = new ();

    private void Awake()
    {
        playerPrefab = Resources.Load<Player>("Player");

        if (NetworkManager.isHost)
            enabled = true;

        if (!enabled)
            return;

        freeColorCodes.Clear();
        for (byte i = 0; i < Player.colors.Length; i++)
            freeColorCodes.Add(i);

        player = Instantiate(playerPrefab);
        player.colorCode = freeColorCodes[Random.Range(0, freeColorCodes.Count)];
        freeColorCodes.Remove(player.colorCode);
        player.local = true;
        players.Add(player);
    }

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = NetworkManager.IPPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 9000");
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    void OnDestroy()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }

    void Update()
    {
        //    foreach (Player player__ in players)
        //        player__.transform.rotation = Quaternion.Euler(0, 0, player.transform.rotation.eulerAngles.z);
        m_Driver.ScheduleUpdate().Complete();

        // Clean up connections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // Accept new connections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;

            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    Message message = stream.RecieveSSPMessage();
                    if (message.Code == MessageCode.ConnectReq)
                    {
                        Debug.Log("Connect Req!");

                        Player client = Instantiate(playerPrefab);
                        client.colorCode = freeColorCodes[Random.Range(0, freeColorCodes.Count)];
                        freeColorCodes.Remove(client.colorCode);
                        players.Add(client);
                        clients.Add(m_Connections[i], client);

                        m_Driver.SendSSPMessage(m_Connections[i], new ConnectAck() { ColorCode = client.colorCode});
                    }
                    else if (message.Code == MessageCode.ConnectOk)
                    {
                        Debug.Log("Connect OK!");

                        UpdatePlayersList msg = new UpdatePlayersList();
                        msg.amount = (byte)(players.Count);
                        msg.playerDatas = new System.Tuple<string, byte>[players.Count];
                        for(int j = 0; j < msg.amount; j++)
                        {
                            msg.playerDatas[j] = new System.Tuple<string, byte>(players[j].Name, players[j].colorCode);
                        }

                        foreach(var connection in m_Connections)
                            m_Driver.SendSSPMessage(connection, msg);
                    }
                    //Commands
                    else if (message.Code == MessageCode.CommandMove)
                    {
                        CommandMove msg = (CommandMove)message;
                        //Debug.Log("CommandMove " + msg.direction * clients[m_Connections[i]].speed);
                        //clients[m_Connections[i]].rigidbody.MovePosition(clients[m_Connections[i]].transform.position + new Vector3(0, (msg.direction - 1) * clients[m_Connections[i]].speed));
                        clients[m_Connections[i]].rigidbody.MovePosition(clients[m_Connections[i]].transform.position + (Vector3)msg.direction * clients[m_Connections[i]].speed);
                    }
                    else if (message.Code == MessageCode.CommandLook)
                    {
                        CommandLook msg = (CommandLook)message;
                        //clients[m_Connections[i]].rigidbody.MovePosition(clients[m_Connections[i]].transform.position + new Vector3(0, (msg.direction - 1) * clients[m_Connections[i]].speed));
                        clients[m_Connections[i]].transform.rotation = Quaternion.Euler(0, 0, msg.rotationZ);
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }

        foreach(NetworkConnection connection in clients.Keys)
            foreach(Player player in players)
                if (clients[connection] != player)
                    m_Driver.SendSSPMessage(connection, new CommandLook() { playerCode = player.colorCode, rotationZ = player.transform.rotation.eulerAngles.z });

        /*foreach (NetworkConnection connection in m_Connections)
            foreach (Player player in players)
                if (clients[connection] != player)
                    m_Driver.SendSSPMessage(connection, new CommandLook() { playerCode = player.colorCode, rotationZ = player.transform.rotation.eulerAngles.z });*/
    }
}