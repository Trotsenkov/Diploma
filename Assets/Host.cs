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

    private Bullet bulletPrefab;
    private Dictionary<ushort, Bullet> activeBullets = new ();
    private ushort _bulletCounter;
    private ushort bulletCounter => (ushort)(_bulletCounter++ % 1500);


    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections; //delete
    private Dictionary<NetworkConnection, Player> clients = new ();

    private void Awake()
    {
        playerPrefab = Resources.Load<Player>("Player");
        bulletPrefab = Resources.Load<Bullet>("Bullet");

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
                    #region Connection
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
                    #endregion

                    #region Commands
                    else if (message.Code == MessageCode.CommandMove)
                    {
                        CommandMove msg = (CommandMove)message;
                        clients[m_Connections[i]].rigidbody.MovePosition(clients[m_Connections[i]].transform.position + (Vector3)msg.direction * clients[m_Connections[i]].speed);
                    }
                    else if (message.Code == MessageCode.CommandLook)
                    {
                        CommandLook msg = (CommandLook)message;
                        clients[m_Connections[i]].transform.rotation = Quaternion.Euler(0, 0, msg.rotationZ);
                    }
                    else if (message.Code == MessageCode.CommandShoot)
                    {
                        SpawnBulletForPlayer(clients[m_Connections[i]]);
                    }
                    #endregion
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }

        foreach (NetworkConnection connection in clients.Keys)
        {
            foreach (Player player in players)
            {
                if (clients[connection] != player)
                    m_Driver.SendSSPMessage(connection, new CommandLook() { playerCode = player.colorCode, rotationZ = player.transform.rotation.eulerAngles.z });

                m_Driver.SendSSPMessage(connection, new UpdatePlayerPosition() { playerCode = player.colorCode, position = player.transform.position });
            }

            byte amount = (byte)Enemy_Spawner.Enemies.Count;
            Vector2[] positions = new Vector2[amount];
            for (int i = 0; i < amount; i++)
                positions[i] = Enemy_Spawner.Enemies[i].transform.position;
            m_Driver.SendSSPMessage(connection, new SetEnemies() { amount = amount, enemyPositions = positions });
        }
    }

    public void SetPlayerHP(Player player)
    {
        foreach (NetworkConnection connection in clients.Keys)
            m_Driver.SendSSPMessage(connection, new UpdatePlayerHP() { colorCode = player.colorCode, HP = player.HP }) ;
    }

    public void SpawnBulletForPlayer(Player player)
    {
        ushort ID = bulletCounter;
        activeBullets.Add(ID, Instantiate(bulletPrefab, player.transform.position + player.transform.up, player.transform.rotation));
        activeBullets[ID].ID = ID;
        activeBullets[ID].host = this;

        foreach (NetworkConnection connection in clients.Keys)
            m_Driver.SendSSPMessage(connection, new AddBullet() { bulletID = ID, position = player.transform.position + player.transform.up, rotationZ = player.transform.rotation.eulerAngles.z });
    }

    public void RemoveBullet(ushort ID)
    {
        if (activeBullets.ContainsKey(ID))
        {
            activeBullets.Remove(ID);
        }

        foreach (NetworkConnection connection in clients.Keys)
            m_Driver.SendSSPMessage(connection, new RemBullet() { bulletID = ID });
    }
}