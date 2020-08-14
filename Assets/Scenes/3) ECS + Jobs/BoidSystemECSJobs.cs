using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class BoidSystemECSJobs : JobComponentSystem {
    
    private BoidControllerECSJobs controller;

    protected override JobHandle OnUpdate(JobHandle inputDeps) {

        // This runs only if there exists a BoidControllerECSJobs instance.
        if (!controller) {
            controller = BoidControllerECSJobs.Instance;
        }
        if (controller) {
            EntityQuery boidQuery = GetEntityQuery(ComponentType.ReadOnly<BoidECSJobs>(), ComponentType.ReadOnly<LocalToWorld>());

            NativeArray<Entity> entityArray = boidQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<LocalToWorld> localToWorldArray = boidQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

            // These arrays get deallocated after job completion
            NativeArray<EntityWithLocalToWorld> boidArray = new NativeArray<EntityWithLocalToWorld>(entityArray.Length, Allocator.TempJob);
            NativeArray<float4x4> newBoidTransforms = new NativeArray<float4x4>(entityArray.Length, Allocator.TempJob);

            for (int i = 0; i < entityArray.Length; i++) {
                boidArray[i] = new EntityWithLocalToWorld {
                    entity = entityArray[i],
                    localToWorld = localToWorldArray[i]
                };
            }

            entityArray.Dispose();
            localToWorldArray.Dispose();

            BoidJob boidJob = new BoidJob {
                otherBoids = boidArray,
                newBoidTransforms = newBoidTransforms,
                boidPerceptionRadius = controller.boidPerceptionRadius,
                separationWeight = controller.separationWeight,
                cohesionWeight = controller.cohesionWeight,
                alignmentWeight = controller.alignmentWeight,
                cageSize = controller.cageSize,
                avoidWallsTurnDist = controller.avoidWallsTurnDist,
                avoidWallsWeight = controller.avoidWallsWeight,
                boidSpeed = controller.boidSpeed,
                deltaTime = Time.DeltaTime
            };
            BoidMoveJob boidMoveJob = new BoidMoveJob {
                newBoidTransforms = newBoidTransforms
            };

            JobHandle boidJobHandle = boidJob.Schedule(this, inputDeps);
            return boidMoveJob.Schedule(this, boidJobHandle);
        }
        else {
            return inputDeps;
        }
    }

    private struct EntityWithLocalToWorld {
        public Entity entity;
        public LocalToWorld localToWorld;
    }

    [BurstCompile]
    [RequireComponentTag(typeof(BoidECSJobs))]
    private struct BoidJob : IJobForEachWithEntity<LocalToWorld> {
        
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<EntityWithLocalToWorld> otherBoids;
        [WriteOnly] public NativeArray<float4x4> newBoidTransforms;

        [ReadOnly] public float boidPerceptionRadius;
        [ReadOnly] public float separationWeight;
        [ReadOnly] public float cohesionWeight;
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public float cageSize;
        [ReadOnly] public float avoidWallsTurnDist;
        [ReadOnly] public float avoidWallsWeight;
        [ReadOnly] public float boidSpeed;
        [ReadOnly] public float deltaTime;
        
        public void Execute(Entity boid, int boidIndex, [ReadOnly] ref LocalToWorld localToWorld) {
            float3 boidPosition = localToWorld.Position;
            
            float3 seperationSum = float3.zero;
            float3 positionSum = float3.zero;
            float3 headingSum = float3.zero;

            int boidsNearby = 0;

            for (int otherBoidIndex = 0; otherBoidIndex < otherBoids.Length; otherBoidIndex++) {
                if (boid != otherBoids[otherBoidIndex].entity) {
                    
                    float3 otherPosition = otherBoids[otherBoidIndex].localToWorld.Position;
                    float distToOtherBoid = math.length(boidPosition - otherPosition);

                    if (distToOtherBoid < boidPerceptionRadius) {

                        seperationSum += -(otherPosition - boidPosition) * (1f / math.max(distToOtherBoid, .0001f));
                        positionSum += otherPosition;
                        headingSum += otherBoids[otherBoidIndex].localToWorld.Forward;

                        boidsNearby++;
                    }
                }
            }

            float3 force = float3.zero;

            if (boidsNearby > 0) {
                force += (seperationSum / boidsNearby)                * separationWeight;
                force += ((positionSum / boidsNearby) - boidPosition) * cohesionWeight;
                force += (headingSum / boidsNearby)                   * alignmentWeight;
            }
            if (math.min(math.min(
                (cageSize / 2f) - math.abs(boidPosition.x),
                (cageSize / 2f) - math.abs(boidPosition.y)),
                (cageSize / 2f) - math.abs(boidPosition.z))
                    < avoidWallsTurnDist) {
                force += -math.normalize(boidPosition) * avoidWallsWeight;
            }

            float3 velocity = localToWorld.Forward * boidSpeed;
            velocity += force * deltaTime;
            velocity = math.normalize(velocity) * boidSpeed;

            newBoidTransforms[boidIndex] = float4x4.TRS(
                localToWorld.Position + velocity * deltaTime,
                quaternion.LookRotationSafe(velocity, localToWorld.Up),
                new float3(1f)
            );
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(BoidECSJobs))]
    private struct BoidMoveJob : IJobForEachWithEntity<LocalToWorld> {
        
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float4x4> newBoidTransforms;
        
        public void Execute(Entity boid, int boidIndex, [WriteOnly] ref LocalToWorld localToWorld) {
            localToWorld.Value = newBoidTransforms[boidIndex];
        }
    }
}
