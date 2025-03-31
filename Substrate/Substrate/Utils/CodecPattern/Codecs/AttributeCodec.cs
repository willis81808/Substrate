using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace Substrate.Utils.CodecPattern.Codecs
{
    public class AttributeCodec<T> : ICodec<T> where T : IAttribute
    {
        public T Decode(dynamic input)
        {
            if (input is T attribute)
                return attribute;

            throw new InvalidCastException($"Cannot decode {input?.GetType()} to {typeof(T)}");
        }

        public dynamic Encode(T value)
        {
            return value;
        }
    }

    public class IAttributeCodec : ICodec<IAttribute>
    {
        public IAttribute Decode(dynamic input)
        {
            if (input is IAttribute attribute)
                return attribute;

            // Handle conversion from primitives/collections to appropriate attributes
            if (input is int intValue)
                return new IntAttribute(intValue);
            if (input is long longValue)
                return new LongAttribute(longValue);
            if (input is float floatValue)
                return new FloatAttribute(floatValue);
            if (input is double doubleValue)
                return new DoubleAttribute(doubleValue);
            if (input is bool boolValue)
                return new BoolAttribute(boolValue);
            if (input is string stringValue)
                return new StringAttribute(stringValue);
            if (input is byte[] byteArray)
                return new ByteArrayAttribute(byteArray);
            if (input is bool[] boolArray)
                return new BoolArrayAttribute(boolArray);
            if (input is int[] intArray)
                return new IntArrayAttribute(intArray);
            if (input is long[] longArray)
                return new LongArrayAttribute(longArray);
            if (input is float[] floatArray)
                return new FloatArrayAttribute(floatArray);
            if (input is double[] doubleArray)
                return new DoubleArrayAttribute(doubleArray);
            if (input is string[] stringArray)
                return new StringArrayAttribute(stringArray);

            // Handle dictionaries as TreeAttributes
            if (input is Dictionary<string, dynamic> dict)
            {
                var tree = new TreeAttribute();
                foreach (var pair in dict)
                {
                    tree[pair.Key] = Decode(pair.Value);
                }
                return tree;
            }

            throw new InvalidCastException($"Cannot decode {input?.GetType()} to IAttribute");
        }

        public dynamic Encode(IAttribute value)
        {
            return value;
        }
    }

    public static class AttributeCodecs
    {
        public static readonly ICodec<IAttribute> ATTRIBUTE = new IAttributeCodec();
        public static readonly ICodec<ITreeAttribute> TREE_ATTRIBUTE = RecordCodecBuilder.Create<ITreeAttribute>()
            .Apply(Codec.DictionaryOf(Codec.STRING, ATTRIBUTE), "attributes", i => new Dictionary<string, IAttribute>(i))
            .Build((attributes) =>
            {
                var tree = new TreeAttribute();
                foreach (var key in attributes.Keys)
                {
                    tree[key] = attributes[key];
                }
                return tree;
            });


        // Helper method to create an attribute codec that extracts the IAttribute value
        public static ICodec<T> FromAttribute<TAttr, T>(
            ICodec<TAttr> attributeCodec,
            Func<TAttr, T> extractor,
            Func<T, TAttr> creator)
            where TAttr : IAttribute
        {
            return new DerivedCodec<TAttr, T>(
                attributeCodec,
                attr => extractor(attr),
                val => creator(val)
            );
        }
    }
}
