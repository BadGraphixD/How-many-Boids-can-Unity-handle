using System.Collections.Generic;
using UnityEngine;

public class BoidsController : MonoBehaviour {
    public static BoidsController Instance;

    [SerializeField] private int boidAmount;
    [SerializeField] private GameObject boidPrefab;

    public float boidSpeed;
    public float boidPerceptionRadius;
    public float cageSize;

    public float separationWeight;
    public float cohesionWeight;
    public float alignmentWeight;

    public float avoidWallsWeight;
    public float avoidWallsTurnDist;

    public List<Boid> boids;

    private void Awake() {

        Instance = this;
        boids.Clear();

        for (int i = 0; i < boidAmount; i++) {
            Vector3 pos = new Vector3(
                Random.Range(-cageSize / 2f, cageSize / 2f),
                Random.Range(-cageSize / 2f, cageSize / 2f),
                Random.Range(-cageSize / 2f, cageSize / 2f)
            );
            Quaternion rot = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );

            Boid newBoid = Instantiate(boidPrefab, pos, rot).GetComponent<Boid>();
            boids.Add(newBoid);
        }
    }

    public List<Boid> GetBoids() { return boids; }

    private void OnDrawGizmos() {

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                cageSize,
                cageSize,
                cageSize
            )
        );
    }
}
