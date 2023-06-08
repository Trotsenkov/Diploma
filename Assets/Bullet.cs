using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;
    public ushort ID;
    public Host host;

    void Update()
    {
        transform.position += speed * Time.deltaTime * transform.up;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!NetworkManager.isHost)
            return;

        if (collision.gameObject.GetComponent<Enemy>() != null)
            Enemy_Spawner.DeleteEnemy(collision.gameObject.GetComponent<Enemy>());
        
        Destroy(gameObject);

        host.RemoveBullet(ID);
    }
}