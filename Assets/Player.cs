using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] public float speed;
    public new Rigidbody2D rigidbody;

    private Host host;

    public static readonly Color[] colors = { Color.red, Color.yellow / 2 + Color.red, Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };
    public Color color;

    public string Name;
    public byte colorCode;
    public bool local;

    public const byte MaxHP = 5;
    private byte hp = MaxHP;
    public byte HP
    {
        get { return hp; }
        set
        {
            hp = value;

            GetComponent<SpriteRenderer>().color = color * ((float)hp / MaxHP) + Color.black;

            if (hp <= 0)
                Destroy(gameObject);
        }
    }

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        host = GameObject.FindObjectOfType<Host>();

        color = colors[colorCode];

        HP = MaxHP;
    }

    void Update()
    {
        if (!local)
            return;

        if (NetworkManager.isHost)
        {
            rigidbody.MovePosition(transform.position + speed * new Vector3(Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0, Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0));

            if (Input.GetMouseButtonDown(0))
                host.SpawnBulletForPlayer(this);
        }

        Vector3 mouseScreenPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 lookAt = mouseScreenPosition;
        float AngleRad = Mathf.Atan2(lookAt.y - transform.position.y, lookAt.x - transform.position.x);
        float AngleDeg = (180 / Mathf.PI) * AngleRad - 90;
        transform.rotation = Quaternion.Euler(0, 0, AngleDeg);
    }

    private void FixedUpdate()
    {
        rigidbody.velocity /= 1.2f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!NetworkManager.isHost)
            return;

        if (collision.gameObject.GetComponent<Enemy>() != null)
        {
            Enemy_Spawner.DeleteEnemy(collision.gameObject.GetComponent<Enemy>());

            HP -= 1;
            host.SetPlayerHP(this);
        }
    }
}