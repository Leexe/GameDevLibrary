#pragma kernel UpdateBoids

struct BoidData
{
    float3 position;
    float3 velocity;
};

RWStructuredBuffer<BoidData> boidsBuffer;

uint numBoids;
float deltaTime;
float maxSpeed;

float separationDistanceSqr;
float alignmentDistanceSqr;
float cohesionDistanceSqr;

float separationWeight;
float alignmentWeight;
float cohesionWeight;

[numthreads(64, 1, 1)]
void UpdateBoids(uint3 id : SV_DispatchThreadID)
{
    uint myIndex = id.x;
    BoidData myBoid = boidsBuffer[myIndex];

    float3 separationForce = float3(0, 0, 0);
    float3 averageVelocity = float3(0, 0, 0);
    float3 center = float3(0, 0, 0);

    int separationCount = 0;
    int alignmentCount = 0;
    int cohesionCount = 0;

    for (uint i = 0; i < numBoids; i++)
    {
        if (i == myIndex)
        {
            continue;
        }

        BoidData otherBoid = boidsBuffer[i];
        float3 offset = myBoid.position - otherBoid.position;
        float sqrDistance = dot(offset, offset);

        // Seperation
        if (sqrDistance < separationDistanceSqr && sqrDistance > 0)
        {
            separationForce += offset / sqrDistance;
            separationCount++;
        }

        // Alignment
        if (sqrDistance < alignmentDistanceSqr)
        {
            averageVelocity += otherBoid.velocity;
            alignmentCount++;
        }

        // Cohesion
        if (sqrDistance < cohesionDistanceSqr)
        {
            center += otherBoid.position;
            cohesionCount++;
        }
    }

    float3 finalSteering = float3(0, 0, 0);

    if (separationCount > 0)
    {
        finalSteering += separationForce / separationCount * separationWeight;
    }

    if (alignmentCount > 0)
    {
        finalSteering += normalize(averageVelocity / alignmentCount) * alignmentWeight;
    }

    if (cohesionCount > 0)
    {
        finalSteering += normalize(center / cohesionCount - myBoid.position) * cohesionWeight;
    }

    myBoid.velocity += finalSteering * deltaTime;
    float speed = length(myBoid.velocity);
    if (speed > maxSpeed)
    {
        myBoid.velocity = (myBoid.velocity / speed) * maxSpeed;
    }

    myBoid.position += myBoid.velocity * deltaTime;

    boidsBuffer[myIndex] = myBoid;
}
