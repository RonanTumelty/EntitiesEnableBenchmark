using Unity.Burst;
using Unity.Entities;
using UnityEngine.Scripting;

namespace StructuralChangesTest
{
    public struct SystemGroups
    {
        [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        [UpdateBefore(typeof(ChunkTestSystemGroup))]
        public partial class EnableDisableTestSystemGroup : ComponentSystemGroup
        {
            [Preserve]
            public EnableDisableTestSystemGroup()
            {
            }
        }
        
        [UpdateInGroup(typeof(SimulationSystemGroup))]
        public partial class ChunkTestSystemGroup : ComponentSystemGroup
        {
            [Preserve]
            public ChunkTestSystemGroup()
            {
            }
        }
        
        [BurstCompile]
        [UpdateInGroup(typeof(ChunkTestSystemGroup), OrderFirst = true)]
        public partial struct CompleteAllJobsSystem : ISystem
        {
            private EntityManager _entityManager;

            public void OnCreate(ref SystemState state)
            {
                _entityManager = state.World.EntityManager;
            }
            
            [BurstCompile]
            public  void OnDestroy(ref SystemState state) {}
            
            [BurstCompile]
            public  void OnUpdate(ref SystemState state)
            {
                _entityManager.CompleteAllTrackedJobs();
            }
        }
    }
}