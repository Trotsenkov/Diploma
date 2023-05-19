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

    public const int MaxHP = 5;
    private int hp = MaxHP;
    private int HP
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
                host.SpawnBulletForPlayer(this);//Instantiate(bulletPrefab, transform.position + transform.up, transform.rotation);
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
        if (collision.gameObject.GetComponent<Enemy>() != null)
        {
            Destroy(collision.gameObject);

            HP -= 1;
        }
    }
}