using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private float speed;
    private new Rigidbody2D rigidbody;

    private Transform[] players;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        players = GameObject.FindObjectsOfType<Player>().Select(player => player.transform).ToArray();
    }

    void Update()
    {
        Transform player = players.Where(player => player != null).OrderBy(player => Vector3.Distance(player.position, transform.position)).FirstOrDefault() ?? transform;

        rigidbody.MovePosition(transform.position + speed * (player.position - transform.position).normalized);
    }
}