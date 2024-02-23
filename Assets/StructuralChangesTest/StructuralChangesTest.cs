using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StructuralChangesTest
{
    [BurstCompile]
    [UpdateInGroup(typeof(SystemGroups.EnableDisableTestSystemGroup))]
    public partial struct TestEnableableComponentsSystem : ISystem
    {
        private EntityManager _entityManager;
        
        public void OnCreate(ref SystemState state)
        {
            _entityManager = state.World.EntityManager;
            for (int i = 0; i < 10000; i++)
                _entityManager.CreateEntity(new ComponentType[]{typeof(ComponentA)});
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {}
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entities = SystemAPI.QueryBuilder().WithAll<ComponentA>().Build();
            _entityManager.SetComponentEnabled<ComponentA>(entities, false);
            _entityManager.SetComponentEnabled<ComponentA>(entities, true);
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SystemGroups.EnableDisableTestSystemGroup))]
    [UpdateAfter(typeof(TestEnableableComponentsSystem))]
    public partial struct TestStructuralChangesSystem : ISystem
    {
        private EntityManager entityManager;
        
        public void OnCreate(ref SystemState state)
        {
            entityManager = state.World.EntityManager;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {}
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            entityManager = state.EntityManager;
            var entityArray = SystemAPI.QueryBuilder().WithAll<ComponentA>().Build().ToEntityArray(Allocator.Temp);

            entityManager.AddComponent<Disabled>(entityArray);
            entityManager.RemoveComponent<Disabled>(entityArray);
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SystemGroups.EnableDisableTestSystemGroup))]
    [UpdateAfter(typeof(TestStructuralChangesSystem))]
    public partial struct TestEnableableComponentsParallelSystem : ISystem
    {
        private ComponentLookup<ComponentA> _lookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lookup = state.GetComponentLookup<ComponentA>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {}
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _lookup.Update(ref state); 

            var disableJob = new ToggleEnableableComponentJobOff()
            {
                lookup = _lookup
            };
            var handle = disableJob.ScheduleParallel(state.Dependency);
            
            var enableJob = new ToggleEnableableComponentJobOn()
            {
                lookup = _lookup
            };
            state.Dependency = enableJob.ScheduleParallel(handle);
        }

        [BurstCompile]
        [WithAll(typeof(ComponentA))]
        public partial struct ToggleEnableableComponentJobOff : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<ComponentA> lookup;
        
            void Execute(in Entity entity)
            {
                lookup.SetComponentEnabled(entity, false);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ComponentA))]
        public partial struct ToggleEnableableComponentJobOn : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<ComponentA> lookup;
        
            void Execute(in Entity entity)
            {
                lookup.SetComponentEnabled(entity, true);
            }
        }
    }
    
    // [BurstCompile]
    // public partial struct TestEnableableEntitySystem : ISystem
    // {
    //     private EntityManager _entityManager;
    //     
    //     public void OnCreate(ref SystemState state)
    //     {
    //         _entityManager = state.World.EntityManager;
    //         var archetype = new EntityArchetype();
    //         for (int i = 0; i < 100000; i++)
    //             _entityManager.CreateEntity(new ComponentType[]{typeof(ComponentA)});
    //     }
    //     
    //     [BurstCompile]
    //     public void OnUpdate(ref SystemState state)
    //     {
    //         var ecb = new EntityCommandBuffer(Allocator.TempJob);
    //
    //         var entities = SystemAPI.QueryBuilder().WithAll<ComponentA>().Build();
    //         _entityManager.SetComponentEnabled<Entity>(entities, false);
    //         _entityManager.SetComponentEnabled<Entity>(entities, true);
    //     }
    // }
}