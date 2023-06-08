using UnityEngine;
using Unity.Networking.Transport;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    [SerializeField] GameObject LoadingText;

    private byte colorCode;
    private Player player;
    private Player playerPrefab;
    private readonly List<Player> players = new ();

    private Bullet bulletPrefab;
    private readonly Dictionary<ushort, Bullet> activeBullets = new ();

    public NetworkDriver Driver;
    public NetworkConnection Connection;
    public bool Connected;

    private float connectingTime = 0;

    private void Awake()
    {
        playerPrefab = Resources.Load<Player>("Player");
        bulletPrefab = Resources.Load<Bullet>("Bullet");

        if (!NetworkManager.isHost)
            enabled = true;
        else
            LoadingText.SetActive(false);
    }

    void Start()
    {
        GameObject.Find("Start Button").SetActive(false);

        Driver = NetworkDriver.Create();
        Connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.Parse(NetworkManager.IPAddress, NetworkManager.IPPort);
        Connection = Driver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        if (Driver.IsCreated)
            Driver.Dispose();
    }

    void Update()
    {
        Driver.ScheduleUpdate().Complete();

        if (!Connection.IsCreated)
            return;

        connectingTime += Time.deltaTime;
        if (connectingTime >= 5)
        {
            NetworkManager.FailReason = "Server is unreachable";
            SceneManager.LoadScene(0);
        }

        NetworkEvent.Type cmd;
        while ((cmd = Connection.PopEvent(Driver, out var stream)) != NetworkEvent.Type.Empty)
        {
            connectingTime = 0;
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server");
                Driver.SendSSPMessage(Connection, new ConnectReq() { Name = NetworkManager.Name });
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                Message message = stream.RecieveSSPMessage();

                #region Connection
                if (message is ConnectAck)
                {
                    Debug.Log("Connect Ack!");
                    colorCode = ((ConnectAck)message).ColorCode;

                    Driver.SendSSPMessage(Connection, new ConnectOk());

                    Connected = true;
                    LoadingText.SetActive(false);
                }
                else if (message is ConnectFail)
                {
                    Debug.Log("Connect Fail!");
                    NetworkManager.FailReason = ((ConnectFail)message).Reason.ToString();
                    SceneManager.LoadScene(0);
                }
                #endregion

                #region ServerBehaviour
                else if (message is UpdatePlayersList)
                {
                    Debug.Log("UpdatePlayersList!");
                    UpdatePlayersList msg = (UpdatePlayersList)message;
                    foreach (System.Tuple<string, byte> data in msg.playerDatas)
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
                else if (message is UpdatePlayerPosition)
                {
                    UpdatePlayerPosition msg = (UpdatePlayerPosition)message;

                    Player plr = players.Find(player => player.colorCode == msg.playerCode);
                    if (plr == null)
                        continue;

                    plr.transform.position = msg.position;
                }
                else if (message is UpdatePlayerHP)
                {
                    UpdatePlayerHP msg = (UpdatePlayerHP)message;

                    Player plr = players.Find(player => player.colorCode == msg.colorCode);
                    if (plr == null)
                        continue;

                    plr.HP = msg.HP;
                }
                else if (message is SetEnemies)
                {
                    SetEnemies msg = (SetEnemies)message;
                    Enemy_Spawner.SetEnemies(msg.amount, msg.enemyPositions);
                }
                else if (message is AddBullet)
                {
                    AddBullet msg = (AddBullet)message;
                    activeBullets.Add(msg.bulletID, Instantiate(bulletPrefab, msg.position, Quaternion.Euler(0, 0, msg.rotationZ)));
                }
                else if (message is RemBullet)
                {
                    RemBullet msg = (RemBullet)message;
                    Destroy(activeBullets[msg.bulletID].gameObject);
                    activeBullets.Remove(msg.bulletID);
                }
                #endregion

                #region Commands
                else if (message is CommandLook)
                {
                    CommandLook msg = (CommandLook)message;
                    Player plr = players.Find(player => player.colorCode == msg.playerCode);
                    if (plr == null)
                        continue;

                    plr.transform.rotation = Quaternion.Euler(0, 0, msg.rotationZ);
                }
                #endregion
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                Connection = default(NetworkConnection);
                SceneManager.LoadScene(0);
            }
        }

        if (Input.GetMouseButtonDown(0))
            Driver.SendSSPMessage(Connection, new CommandShoot());
    }

    private void FixedUpdate()
    {
        if (!Connection.IsCreated || !Connected || player == null)
            return;

        Driver.SendSSPMessage(Connection, new CommandMove()
        {
            direction = new Vector2(Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0, Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0)
        });
        Driver.SendSSPMessage(Connection, new CommandLook() { playerCode = player.colorCode, rotationZ = player.transform.rotation.eulerAngles.z });
    }
}