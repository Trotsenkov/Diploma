using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;

    void Update()
    {
        transform.position += speed * Time.deltaTime * transform.up;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Enemy>() != null)
            Destroy(collision.gameObject);
        
        Destroy(gameObject);
    }
}
