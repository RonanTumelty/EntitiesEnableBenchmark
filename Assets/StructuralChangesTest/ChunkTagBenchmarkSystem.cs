using Unity.Burst;
using Unity.Entities;

namespace StructuralChangesTest
{
    [BurstCompile]
    [UpdateInGroup(typeof(SystemGroups.ChunkTestSystemGroup))]
    public partial struct ChunkTagBenchmarkSystem : ISystem
    {
        private EntityManager _entityManager;
        
        public void OnCreate(ref SystemState state)
        {
            _entityManager = state.EntityManager;
            
            for (int i = 0; i < 5; i++)
            {
                var sharedComponent = new SharedComponentB() {chunkId = i};

                for (int j = 0; j < 10000; j++)
                {
                    var newEntity = _entityManager.CreateEntity(new ComponentType[] {typeof(SharedComponentB)});
                    _entityManager.SetSharedComponent(newEntity, sharedComponent);
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {}

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SharedComponentB>().Build();
            _entityManager.AddComponent<ComponentD>(query);
            _entityManager.RemoveComponent<ComponentD>(query);
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SystemGroups.ChunkTestSystemGroup))]
    [UpdateAfter(typeof(ChunkTagBenchmarkSystem))]
    public partial struct ChunkValueBenchmarkSystem : ISystem
    {
        private EntityManager _entityManager;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _entityManager = state.EntityManager;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {}

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SharedComponentB>().Build();
            _entityManager.AddComponent<NonTagComponentC>(query);
            _entityManager.RemoveComponent<NonTagComponentC>(query);
        }
    }
}