using Unity.Entities;

namespace StructuralChangesTest
{
    public struct SharedComponentB : ISharedComponentData
    {
        public int chunkId;
    }
}