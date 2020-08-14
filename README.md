# How many Boids can Unity handle?
This is a collection of 4 different implementations of a boid simulation in Unity.
It's an experiment to find out how many boids Unity can simulate and render at once.
The four different implementations are the following:

1. Implementation with GameObjects and MonoBehaviours (~350 boids)
2. Implementation with ECS (no Jobs) (~500 boids)
3. Implementation with ECS and Jobs (~7.000 boids)
4. More complex implementation with ECS and Jobs (~250.000 boids)

The last one includes parts of code originally made by Bogdan Codreanu: [original code](https://github.com/BogdanCodreanu/ECS-Boids-Murmuration_Unity_2019.1)
