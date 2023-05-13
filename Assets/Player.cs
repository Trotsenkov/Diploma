using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] public float speed;
    public new Rigidbody2D rigidbody;
    private Bullet bulletPrefab;

    public static readonly Color[] colors = { Color.white, Color.green, Color.red, Color.yellow, Color.cyan, Color.blue, Color.magenta };
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
        bulletPrefab = Resources.Load<Bullet>("Bullet");

        color = colors[colorCode];

        HP = MaxHP;
    }

    void Update()
    {
        if (!local)
            return;

        rigidbody.MovePosition(transform.position + new Vector3(speed * Input.GetAxis("Horizontal"), speed * Input.GetAxis("Vertical")));

        Vector3 mouseScreenPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 lookAt = mouseScreenPosition;
        float AngleRad = Mathf.Atan2(lookAt.y - transform.position.y, lookAt.x - transform.position.x);
        float AngleDeg = (180 / Mathf.PI) * AngleRad - 90;
        transform.rotation = Quaternion.Euler(0, 0, AngleDeg);

        if (Input.GetMouseButtonDown(0))
            Instantiate(bulletPrefab, transform.position + transform.up, transform.rotation);
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