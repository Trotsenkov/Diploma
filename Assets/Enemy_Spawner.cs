using System.Collections.Generic;
using UnityEngine;

public class Enemy_Spawner : MonoBehaviour
{
    private static Enemy prefab;
    private static readonly List<Enemy> enemies = new ();
    public static IReadOnlyList<Enemy> Enemies => enemies;

    private float time = 0;
    [SerializeField] private float delay = 4.5f;

    [SerializeField] private Vector2 mapSize = new Vector2(11, 7);

    private void Start()
    {
        prefab = Resources.Load<Enemy>("Enemy");
    }

    void Update()
    {
        if (!NetworkManager.isHost)
            return;

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

                enemies.Add(Instantiate(prefab, position, Quaternion.identity));
            }
        }
    }

    public static void SetEnemies(byte amount, Vector2[] positions)
    {
        for(int i = 0; i < amount; i++)
        {
            if (i < enemies.Count)
            {
                enemies[i].gameObject.SetActive(true);
                enemies[i].transform.position = positions[i];
            }
            else
                enemies.Add(Instantiate(prefab, positions[i], Quaternion.identity));
        }

        if (enemies.Count >= amount)
            for (int i = amount; i < enemies.Count; i++)
                enemies[i].gameObject.SetActive(false);
    }

    public static void DeleteEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
        Destroy(enemy.gameObject);
    }
}