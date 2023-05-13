using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Spawner : MonoBehaviour
{
    private Enemy prefab;

    private float time = 0;
    [SerializeField] private float delay = 4.5f;

    [SerializeField] private Vector2 mapSize = new Vector2(11, 7);

    private void Start()
    {
        prefab = Resources.Load<Enemy>("Enemy");
    }

    void Update()
    {
        time += Time.deltaTime;

        if (time > delay)
        {
            time -= delay;
            if (delay > 0.5f)
                delay *= 0.9f;

            int amount = Random.Range(1, 4);
            for (int i = 0; i < amount; i++)
            {
                Vector2 position;
                float pos = Random.Range(0, mapSize.x * 2 + mapSize.y * 2);
                if (pos < mapSize.x * 2)
                    position = new Vector2(pos - mapSize.x, (Random.Range(-1, 1) * 2 + 1) * mapSize.y);
                else
                    position = new Vector2((Random.Range(-1, 1) * 2 + 1) * mapSize.x, pos - mapSize.x * 2 - mapSize.y);
                Instantiate(prefab, position, Quaternion.identity);
            }
        }
    }
}