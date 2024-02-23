using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using Unity.Serialization.Binary;

[assembly: InternalsVisibleTo("Unity.Scenes")]

[assembly: GeneratePropertyBagsForTypesQualifiedWith(typeof(Unity.Entities.ISharedComponentData))]
[assembly: GeneratePropertyBagsForTypesQualifiedWith(typeof(Unity.Entities.IComponentData), TypeGenerationOptions.ReferenceType)]

namespace Unity.Entities.Serialization
{
#if !UNITY_DOTSRUNTIME
    struct SerializedKeyFrame
    {
        public float Time;
        public float Value;
        public float InTangent;
        public float OutTangent;
        public float InWeight;
        public float OutWeight;
        public int WeightedMode;

        public SerializedKeyFrame(UnityEngine.Keyframe kf)
        {
            Time = kf.time;
            Value = kf.value;
            InTangent = kf.inTangent;
            OutTangent = kf.outTangent;
            InWeight = kf.inWeight;
            OutWeight = kf.outWeight;
            WeightedMode = (int)kf.weightedMode;
        }

        public static implicit operator UnityEngine.Keyframe(SerializedKeyFrame kf)
        {
            return new UnityEngine.Keyframe(kf.Time, kf.Value, kf.InTangent, kf.OutTangent, kf.InWeight, kf.OutWeight)
            {
                weightedMode = (UnityEngine.WeightedMode) kf.WeightedMode
            };
        }

        public static implicit operator SerializedKeyFrame(UnityEngine.Keyframe kf)
        {
            return new SerializedKeyFrame(kf);
        }
    }
#endif // !UNITY_DOTSRUNTIME

    /// <summary>
    /// Writer to write managed objects to a <see cref="UnsafeAppendBuffer"/> stream.
    /// </summary>
    /// <remarks>
    /// This is used as a wrapper around <see cref="Unity.Serialization.Binary.BinarySerialization"/> with a custom layer for <see cref="UnityEngine.Object"/>.
    /// </remarks>
    unsafe class ManagedObjectBinaryWriter : Unity.Serialization.Binary.IContravariantBinaryAdapter<UnityEngine.Object>
#if !UNITY_DOTSRUNTIME
        , IBinaryAdapter<UnityEngine.AnimationCurve>
#endif
    {
        static readonly UnityEngine.Object[] s_EmptyUnityObjectTable = new UnityEngine.Object[0];

        readonly UnsafeAppendBuffer* m_Stream;
        readonly BinarySerializationParameters m_Params;

        List<UnityEngine.Object> m_UnityObjects;
        Dictionary<UnityEngine.Object, int> m_UnityObjectsMap;

        /// <summary>
        /// Initializes a new instance of <see cref="ManagedObjectBinaryWriter"/> which can be used to write managed objects to the given stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public ManagedObjectBinaryWriter(UnsafeAppendBuffer* stream)
        {
            m_Stream = stream;
            m_Params = new BinarySerializationParameters
            {
                UserDefinedAdapters = new List<IBinaryAdapter> {this},
                State = new BinarySerializationState(),
            };
        }

        /// <summary>
        /// Adds a custom adapter to the writer.
        /// </summary>
        /// <param name="adapter">The custom adapter to add.</param>
        public void AddAdapter(IBinaryAdapter adapter) => m_Params.UserDefinedAdapters.Add(adapter);

        /// <summary>
        /// Gets all <see cref="UnityEngine.Object"/> types encountered during serialization.
        /// </summary>
        /// <returns>A set of all <see cref="UnityEngine.Object"/> types encountered during serialization</returns>
        public UnityEngine.Object[] GetUnityObjects() => m_UnityObjects?.ToArray() ?? s_EmptyUnityObjectTable;

        /// <summary>
        /// Writes the given boxed object to the binary stream.
        /// </summary>
        /// <remarks>
        /// Any <see cref="UnityEngine.Object"/> references are added to the object table and can be retrieved by calling <see cref="GetUnityObjects"/>.
        /// </remarks>
        /// <param name="obj">The object to serialize.</param>
        public void WriteObject(object obj)
        {
            var parameters = m_Params;
            parameters.SerializedType = obj?.GetType();
            BinarySerialization.ToBinary(m_Stream, obj, parameters);
        }

        void Unity.Serialization.Binary.IContravariantBinaryAdapter<UnityEngine.Object>.Serialize(IBinarySerializationContext context, UnityEngine.Object value)
        {
            var index = -1;

            if (value != null)
            {
                if (null == m_UnityObjects)
                    m_UnityObjects = new List<UnityEngine.Object>();

                if (null == m_UnityObjectsMap)
                    m_UnityObjectsMap = new Dictionary<UnityEngine.Object, int>();

                if (!m_UnityObjectsMap.TryGetValue(value, out index))
                {
                    index = m_UnityObjects.Count;
                    m_UnityObjectsMap.Add(value, index);
                    m_UnityObjects.Add(value);
                }
            }

            context.Writer->Add(index);
        }

        object Unity.Serialization.Binary.IContravariantBinaryAdapter<UnityEngine.Object>.Deserialize(IBinaryDeserializationContext context)
        {
            throw new InvalidOperationException($"Deserialize should never be invoked by {nameof(ManagedObjectBinaryWriter)}");
        }

#if !UNITY_DOTSRUNTIME
        void IBinaryAdapter<UnityEngine.AnimationCurve>.Serialize(in BinarySerializationContext<UnityEngine.AnimationCurve> context, UnityEngine.AnimationCurve value)
        {
            if (value != null)
            {
                context.Writer->Add(value.length);
                context.Writer->Add(value.preWrapMode);
                context.Writer->Add(value.postWrapMode);

                for (int i = 0, count = value.length; i < count; ++i)
                {
                    context.Writer->Add((SerializedKeyFrame) value[i]);
                }
            }
            else
            {
                context.Writer->Add(-1);
            }
        }

        UnityEngine.AnimationCurve IBinaryAdapter<UnityEngine.AnimationCurve>.Deserialize(in BinaryDeserializationContext<UnityEngine.AnimationCurve> context)
        {
            throw new InvalidOperationException($"Deserialize should never be invoked by {nameof(ManagedObjectBinaryWriter)}");
        }
#endif // !UNITY_DOTSRUNTIME
    }

    /// <summary>
    /// Reader to read managed objects from a <see cref="UnsafeAppendBuffer.Reader"/> stream.
    /// </summary>
    /// <remarks>
    /// This is used as a wrapper around <see cref="Unity.Serialization.Binary.BinarySerialization"/> with a custom layer for <see cref="UnityEngine.Object"/>.
    /// </remarks>
    unsafe class ManagedObjectBinaryReader : Unity.Serialization.Binary.IContravariantBinaryAdapter<UnityEngine.Object>
#if !UNITY_DOTSRUNTIME
        , IBinaryAdapter<UnityEngine.AnimationCurve>
#endif
    {
        readonly UnsafeAppendBuffer.Reader* m_Stream;
        readonly BinarySerializationParameters m_Params;
        readonly UnityEngine.Object[] m_UnityObjects;

        /// <summary>
        /// Initializes a new instance of <see cref="ManagedObjectBinaryReader"/> which can be used to read managed objects from the given stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="unityObjects">The table containing all <see cref="UnityEngine.Object"/> references. This is produce by the <see cref="ManagedObjectBinaryWriter"/>.</param>
        public ManagedObjectBinaryReader(UnsafeAppendBuffer.Reader* stream, UnityEngine.Object[] unityObjects)
        {
            m_Stream = stream;
            m_Params = new BinarySerializationParameters
            {
                UserDefinedAdapters = new List<IBinaryAdapter> {this},
                State = new BinarySerializationState(),
            };
            m_UnityObjects = unityObjects;
        }

        /// <summary>
        /// Adds a custom adapter to the reader.
        /// </summary>
        /// <param name="adapter">The custom adapter to add.</param>
        public void AddAdapter(IBinaryAdapter adapter) => m_Params.UserDefinedAdapters.Add(adapter);

        /// <summary>
        /// Reads from the binary stream and returns the next object.
        /// </summary>
        /// <remarks>
        /// The type is given as a hint to the serializer to avoid writing root type information.
        /// </remarks>
        /// <param name="type">The root type.</param>
        /// <returns>The deserialized object value.</returns>
        public object ReadObject(Type type)
        {
            var parameters = m_Params;
            parameters.SerializedType = type;
            return BinarySerialization.FromBinary<object>(m_Stream, parameters);
        }

        void Unity.Serialization.Binary.IContravariantBinaryAdapter<UnityEngine.Object>.Serialize(IBinarySerializationContext context, UnityEngine.Object value)
        {
            throw new InvalidOperationException($"Serialize should never be invoked by {nameof(ManagedObjectBinaryReader)}.");
        }

        object Unity.Serialization.Binary.IContravariantBinaryAdapter<UnityEngine.Object>.Deserialize(IBinaryDeserializationContext context)
        {
            var index = context.Reader->ReadNext<int>();

            if (index == -1)
                return null;

            if (m_UnityObjects == null)
                throw new ArgumentException("We are reading a UnityEngine.Object however no ObjectTable was provided to the ManagedObjectBinaryReader.");

            if ((uint)index >= m_UnityObjects.Length)
                throw new ArgumentException("We are reading a UnityEngine.Object but the deserialized index is out of range for the given object table.");

            return m_UnityObjects[index];
        }

#if !UNITY_DOTSRUNTIME
        void IBinaryAdapter<UnityEngine.AnimationCurve>.Serialize(in BinarySerializationContext<UnityEngine.AnimationCurve> context, UnityEngine.AnimationCurve value)
        {
            throw new InvalidOperationException($"Serialize should never be invoked by {nameof(ManagedObjectBinaryReader)}.");
        }

        UnityEngine.AnimationCurve IBinaryAdapter<UnityEngine.AnimationCurve>.Deserialize(in BinaryDeserializationContext<UnityEngine.AnimationCurve> context)
        {
            var length = context.Reader->ReadNext<int>();
            if (length >= 0)
            {
                var preMode = context.Reader->ReadNext<UnityEngine.WrapMode>();
                var postMode = context.Reader->ReadNext<UnityEngine.WrapMode>();
                var ac = new UnityEngine.AnimationCurve()
                {
                    preWrapMode = preMode,
                    postWrapMode = postMode
                };
                for (int i = 0; i < length; ++i)
                    ac.AddKey(context.Reader->ReadNext<SerializedKeyFrame>());

                return ac;
            }

            return null;
        }
#endif // !UNITY_DOTSRUNTIME
    }
}
