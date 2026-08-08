using System.Collections.Generic;
using UnityEngine;

public sealed class ZGSpawner : MonoBehaviour
{
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private Transform player;
    [SerializeField] private ZGGameManager gameManager;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private int totalToSpawn = 15;
    [SerializeField] private float spawnInterval = 1.8f;

    private readonly List<GameObject> alive = new List<GameObject>();
    private int spawned;
    private float nextSpawnTime;
    private bool stopped;

    public int TotalToSpawn => totalToSpawn;

    private void Update()
    {
        if (stopped || zombiePrefab == null || player == null || spawned >= totalToSpawn || Time.time < nextSpawnTime)
        {
            return;
        }

        alive.RemoveAll(item => item == null);
        if (alive.Count >= maxAlive)
        {
            return;
        }

        Spawn();
        nextSpawnTime = Time.time + spawnInterval;
    }

    public void Stop()
    {
        stopped = true;
    }

    private void Spawn()
    {
        spawned++;
        var point = spawnPoints.Length > 0 ? spawnPoints[Random.Range(0, spawnPoints.Length)] : transform;
        var zombie = Instantiate(zombiePrefab, point.position, point.rotation);
        zombie.name = "ZG_Zombie";
        alive.Add(zombie);

        var ai = zombie.GetComponent<ZGZombieAI>();
        var bonusHealth = spawned > 11 ? 70 : spawned > 7 ? 35 : 0;
        var speedBonus = spawned > 11 ? 0.7f : spawned > 7 ? 0.35f : 0f;
        ai.Initialize(player, gameManager, bonusHealth, speedBonus);
    }
}
