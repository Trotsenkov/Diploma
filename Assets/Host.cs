using UnityEngine;
using Unity.Networking.Transport;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class Host : MonoBehaviour
{
    private Player playerPrefab;
    private Player player;
    private List<Player> players
    {
        get
        {
            List<Player> list = clients.Values.Where(pl => pl != null).ToList();
            if (player != null)
                list.Add(player);
            return list;
        }
    }

    private readonly List<byte> freeColorCodes = new();

    private Bullet bulletPrefab;
    private readonly Dictionary<ushort, Bullet> activeBullets = new();
    private ushort _bulletCounter;
    private ushort bulletCounter => (ushort)(_bulletCounter++ % 1500);

    private bool gameStarted = false;
    public bool GameStarted
    {
        get => gameStarted;
        set
        {
            gameStarted = value;
            Enemy_Spawner.isActive = gameStarted;
        }
    }

    public NetworkDriver Driver;
    private IReadOnlyList<NetworkConnection> Connections => clients.Keys.ToList();

    private readonly Dictionary<NetworkConnection, float> connectingTime = new ();

    private readonly Dictionary<NetworkConnection, Player> clients = new ();

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
        player.Name = NetworkManager.Name;
        freeColorCodes.Remove(player.colorCode);
        player.local = true;
    }

    void Start()
    {
        Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = NetworkManager.IPPort;
        if (Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 9000");
        else
            Driver.Listen();
    }

    void OnDestroy()
    {
        if (Driver.IsCreated)
        {
            Driver.Dispose();
        }
    }

    void Update()
    {
        Driver.ScheduleUpdate().Complete();

        // Clean up connections
        foreach (NetworkConnection connection in Connections)
            if (!connection.IsCreated)
                clients.Remove(connection);

        // Accept new connections
        NetworkConnection c;
        while ((c = Driver.Accept()) != default(NetworkConnection))
        {
            clients.Add(c, null);
            Debug.Log("Accepted a connection");
        }

        DataStreamReader stream;
        for (int i = 0; i < Connections.Count; i++)
        {
            if (connectingTime.ContainsKey(Connections[i]))
                connectingTime[Connections[i]] += Time.deltaTime;
            else
                connectingTime.Add(Connections[i], 0);

            if (connectingTime[Connections[i]] >= 5 && clients[Connections[i]] != null)
            {
                Debug.Log("Client is unreachable");
                clients[Connections[i]].HP = 0;
                SetPlayerHP(clients[Connections[i]]);
                Connections[i].Disconnect(Driver);
                connectingTime.Remove(Connections[i]);
                clients.Remove(Connections[i]);
                continue;
            }

            if (!Connections[i].IsCreated)
                continue;

            NetworkEvent.Type cmd;
            while ((cmd = Driver.PopEventForConnection(Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    if (connectingTime.ContainsKey(Connections[i]))
                        connectingTime[Connections[i]] = 0;
                    else
                        connectingTime.Add(Connections[i], 0);

                    #region Connection
                    Message message = stream.RecieveSSPMessage();
                    if (message.Code == MessageCode.ConnectReq)
                    {
                        ConnectReq msg = (ConnectReq)message;

                        if (players.Any(player => player.Name == msg.Name))
                        {
                            Driver.SendSSPMessage(Connections[i], new ConnectFail() { Reason = ConnectFail.FailReason.NameExists });
                            Connections[i].Disconnect(Driver);
                            continue;
                        }

                        if (players.Count >= 5)
                        {
                            Driver.SendSSPMessage(Connections[i], new ConnectFail() { Reason = ConnectFail.FailReason.ServerOverflow });
                            Connections[i].Disconnect(Driver);
                            continue;
                        }

                        if (GameStarted)
                        {
                            Driver.SendSSPMessage(Connections[i], new ConnectFail() { Reason = ConnectFail.FailReason.GameIsAlreadyStarted });
                            Connections[i].Disconnect(Driver);
                            continue;
                        }

                        Player client = Instantiate(playerPrefab);
                        client.colorCode = freeColorCodes[Random.Range(0, freeColorCodes.Count)];
                        freeColorCodes.Remove(client.colorCode);
                        client.Name = msg.Name;
                        clients[Connections[i]] = client;

                        Driver.SendSSPMessage(Connections[i], new ConnectAck() { ColorCode = client.colorCode });
                    }
                    else if (message.Code == MessageCode.ConnectOk)
                    {
                        Debug.Log("Connect OK!");

                        UpdatePlayersList msg = new UpdatePlayersList();
                        msg.amount = (byte)(players.Count);
                        msg.playerDatas = new System.Tuple<string, byte>[msg.amount];
                        for (int j = 0; j < msg.amount; j++)
                        {
                            msg.playerDatas[j] = new System.Tuple<string, byte>(players[j].Name, players[j].colorCode);
                        }

                        foreach (var connection in Connections)
                            Driver.SendSSPMessage(connection, msg);
                    }
                    #endregion

                    #region Commands
                    else if (message.Code == MessageCode.CommandMove)
                    {
                        CommandMove msg = (CommandMove)message;
                        if (clients[Connections[i]] != null)
                            clients[Connections[i]].rigidbody.MovePosition(clients[Connections[i]].transform.position + (Vector3)msg.direction * clients[Connections[i]].speed);
                    }
                    else if (message.Code == MessageCode.CommandLook)
                    {
                        CommandLook msg = (CommandLook)message;
                        if (clients[Connections[i]] != null)
                            clients[Connections[i]].transform.rotation = Quaternion.Euler(0, 0, msg.rotationZ);
                    }
                    else if (message.Code == MessageCode.CommandShoot)
                    {
                        SpawnBulletForPlayer(clients[Connections[i]]);
                    }
                    #endregion
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    clients.Remove(Connections[i]);// = default(NetworkConnection);
                }
            }
        }
    }
    private void FixedUpdate()
    {
        foreach (NetworkConnection connection in Connections)
        {
            foreach (Player player in players)
            {
                if (!clients.TryGetValue(connection, out var pl) || pl != player)
                        Driver.SendSSPMessage(connection, new CommandLook() { playerCode = player.colorCode, rotationZ = player.transform.rotation.eulerAngles.z });

                Driver.SendSSPMessage(connection, new UpdatePlayerPosition() { playerCode = player.colorCode, position = player.transform.position });
            }

            byte amount = (byte)Enemy_Spawner.Enemies.Count;
            Vector2[] positions = new Vector2[amount];
            for (int i = 0; i < amount; i++)
                positions[i] = Enemy_Spawner.Enemies[i].transform.position;

            Driver.SendSSPMessage(connection, new SetEnemies() { amount = amount, enemyPositions = positions });
        }

        if (players.Count == 0)
        {
            Debug.Log("Game ended!");

            StartCoroutine(ExitGame());
            foreach(var connection in Connections)
            {
                connection.Disconnect(Driver);
            }
        }
    }

    IEnumerator ExitGame()
    {
        yield return null;

        SceneManager.LoadScene(0);

    }

    public void SetPlayerHP(Player player)
    {
        foreach (NetworkConnection connection in Connections)
            Driver.SendSSPMessage(connection, new UpdatePlayerHP() { colorCode = player.colorCode, HP = player.HP }) ;
    }

    public void SpawnBulletForPlayer(Player player)
    {
        ushort ID = bulletCounter;
        activeBullets.Add(ID, Instantiate(bulletPrefab, player.transform.position + player.transform.up, player.transform.rotation));
        activeBullets[ID].ID = ID;
        activeBullets[ID].host = this;

        foreach (NetworkConnection connection in Connections)
            Driver.SendSSPMessage(connection, new AddBullet() { bulletID = ID, position = player.transform.position + player.transform.up, rotationZ = player.transform.rotation.eulerAngles.z });
    }

    public void RemoveBullet(ushort ID)
    {
        if (activeBullets.ContainsKey(ID))
        {
            activeBullets.Remove(ID);
        }

        foreach (NetworkConnection connection in Connections)
            Driver.SendSSPMessage(connection, new RemBullet() { bulletID = ID });
    }
}