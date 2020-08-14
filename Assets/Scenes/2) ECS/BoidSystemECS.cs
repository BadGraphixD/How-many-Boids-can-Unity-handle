using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public class BoidSystemECS : ComponentSystem {

    private BoidControllerECS controller;

    protected override void OnUpdate() {

        // This runs only if there exists a BoidControllerECS instance.
        if (!controller) {
            controller = BoidControllerECS.Instance;
        }
        if (controller) {
            EntityQuery boidQuery = GetEntityQuery(ComponentType.ReadOnly<BoidECS>(), ComponentType.ReadOnly<LocalToWorld>());
            NativeArray<float4x4> newBoidPositions = new NativeArray<float4x4>(boidQuery.CalculateEntityCount(), Allocator.Temp);

            int boidIndex = 0;
            Entities.WithAll<BoidECS>().ForEach((Entity boid, ref LocalToWorld localToWorld) => {
                float3 boidPosition = localToWorld.Position;
                
                float3 seperationSum = float3.zero;
                float3 positionSum = float3.zero;
                float3 headingSum = float3.zero;

                int boidsNearby = 0;
                
                Entities.WithAll<BoidECS>().ForEach((Entity otherBoid, ref LocalToWorld otherLocalToWorld) => {
                    if (boid != otherBoid) {
                        
                        float distToOtherBoid = math.length(boidPosition - otherLocalToWorld.Position);
                        if (distToOtherBoid < controller.boidPerceptionRadius) {

                            seperationSum += -(otherLocalToWorld.Position - boidPosition) * (1f / math.max(distToOtherBoid, .0001f));
                            positionSum += otherLocalToWorld.Position;
                            headingSum += otherLocalToWorld.Forward;

                            boidsNearby++;
                        }
                    }
                });

                float3 force = float3.zero;

                if (boidsNearby > 0) {
                    force += (seperationSum / boidsNearby)                * controller.separationWeight;
                    force += ((positionSum / boidsNearby) - boidPosition) * controller.cohesionWeight;
                    force += (headingSum / boidsNearby)                   * controller.alignmentWeight;
                }
                if (math.min(math.min(
                    (controller.cageSize / 2f) - math.abs(boidPosition.x),
                    (controller.cageSize / 2f) - math.abs(boidPosition.y)),
                    (controller.cageSize / 2f) - math.abs(boidPosition.z))
                        < controller.avoidWallsTurnDist) {
                    force += -math.normalize(boidPosition) * controller.avoidWallsWeight;
                }

                float3 velocity = localToWorld.Forward * controller.boidSpeed;
                velocity += force * Time.DeltaTime;
                velocity = math.normalize(velocity) * controller.boidSpeed;

                newBoidPositions[boidIndex] = float4x4.TRS(
                    localToWorld.Position + velocity * Time.DeltaTime,
                    quaternion.LookRotationSafe(velocity, localToWorld.Up),
                    new float3(1f)
                );
                boidIndex++;
            });
            
            boidIndex = 0;
            Entities.WithAll<BoidECS>().ForEach((Entity boid, ref LocalToWorld localToWorld) => {
                localToWorld.Value = newBoidPositions[boidIndex];
                boidIndex++;
            });
        }
    }
}