using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Vintagestory.API.Common;

namespace Substrate.Utils.CodecPattern
{
    // Base Codec interface
    public interface ICodec<T>
    {
        T Decode(dynamic input);
        dynamic Encode(T value);
    }

    // Common primitive codecs
    public static class Codec
    {
        public static readonly ICodec<int> INT = new PrimitiveCodec<int>();
        public static readonly ICodec<byte> BYTE = new PrimitiveCodec<byte>();
        public static readonly ICodec<string> STRING = new PrimitiveCodec<string>();
        public static readonly ICodec<bool> BOOL = new PrimitiveCodec<bool>();
        public static readonly ICodec<float> FLOAT = new PrimitiveCodec<float>();
        public static readonly ICodec<double> DOUBLE = new PrimitiveCodec<double>();


        public static ICodec<AssetLocation> ASSET_LOCATION = RecordCodecBuilder.Create<AssetLocation>()
            .Apply(Codec.STRING, "domain", location => location.Domain)
            .Apply(Codec.STRING, "path", location => location.Path)
            .Build((domain, path) => new AssetLocation(domain, path));


        public static ICodec<List<T>> ListOf<T>(ICodec<T> elementCodec)
        {
            return new ListCodec<T>(elementCodec);
        }

        public static ICodec<Dictionary<TKey, TValue>> DictionaryOf<TKey, TValue>(
            ICodec<TKey> keyCodec,
            ICodec<TValue> valueCodec)
        {
            return new DictionaryCodec<TKey, TValue>(keyCodec, valueCodec);
        }

        // Helper method to create a field
        public static FieldCodec<TObj, TField> Field<TObj, TField>(
            ICodec<TField> codec,
            string name,
            Expression<System.Func<TObj, TField>> getter)
        {
            return new FieldCodec<TObj, TField>(codec, name, getter.Compile());
        }
    }

    // Simple codec for primitive types
    public class PrimitiveCodec<T> : ICodec<T>
    {
        public T Decode(dynamic input) => (T)input;
        public dynamic Encode(T value) => value;
    }

    // A codec that applies a transformation to another codec
    public class DerivedCodec<TSource, TTarget> : ICodec<TTarget>
    {
        private readonly ICodec<TSource> sourceCodec;
        private readonly System.Func<TSource, TTarget> map;
        private readonly System.Func<TTarget, TSource> contramap;

        public DerivedCodec(ICodec<TSource> sourceCodec, System.Func<TSource, TTarget> map, System.Func<TTarget, TSource> contramap)
        {
            this.sourceCodec = sourceCodec;
            this.map = map;
            this.contramap = contramap;
        }

        public TTarget Decode(dynamic input)
        {
            TSource source = sourceCodec.Decode(input);
            return map(source);
        }

        public dynamic Encode(TTarget value)
        {
            TSource source = contramap(value);
            return sourceCodec.Encode(source);
        }
    }

    // Dictionary codec
    public class DictionaryCodec<TKey, TValue> : ICodec<Dictionary<TKey, TValue>>
    {
        private readonly ICodec<TKey> keyCodec;
        private readonly ICodec<TValue> valueCodec;

        public DictionaryCodec(ICodec<TKey> keyCodec, ICodec<TValue> valueCodec)
        {
            this.keyCodec = keyCodec;
            this.valueCodec = valueCodec;
        }

        public Dictionary<TKey, TValue> Decode(dynamic input)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            // Input should be a collection of key-value pairs
            foreach (var pair in input)
            {
                TKey key = keyCodec.Decode(pair.Key);
                TValue value = valueCodec.Decode(pair.Value);
                dictionary[key] = value;
            }

            return dictionary;
        }

        public dynamic Encode(Dictionary<TKey, TValue> value)
        {
            var result = new Dictionary<dynamic, dynamic>();

            foreach (var pair in value)
            {
                dynamic encodedKey = keyCodec.Encode(pair.Key);
                dynamic encodedValue = valueCodec.Encode(pair.Value);
                result[encodedKey] = encodedValue;
            }

            return result;
        }
    }

    // List codec
    public class ListCodec<T> : ICodec<List<T>>
    {
        private readonly ICodec<T> elementCodec;

        public ListCodec(ICodec<T> elementCodec)
        {
            this.elementCodec = elementCodec;
        }

        public List<T> Decode(dynamic input)
        {
            var list = new List<T>();
            foreach (var item in input)
            {
                list.Add(elementCodec.Decode(item));
            }
            return list;
        }

        public dynamic Encode(List<T> value)
        {
            var result = new List<dynamic>();
            foreach (var item in value)
            {
                result.Add(elementCodec.Encode(item));
            }
            return result;
        }
    }

    // Field codec for a specific field in an object
    public class FieldCodec<TObj, TField>
    {
        private readonly ICodec<TField> codec;
        private readonly string fieldName;
        private readonly System.Func<TObj, TField> getter;

        public FieldCodec(ICodec<TField> codec, string fieldName, System.Func<TObj, TField> getter)
        {
            this.codec = codec;
            this.fieldName = fieldName;
            this.getter = getter;
        }

        public ICodec<TField> Codec => codec;
        public string FieldName => fieldName;
        public System.Func<TObj, TField> Getter => getter;
    }

    // Codec for complete objects
    public class ObjectCodec<T> : ICodec<T>
    {
        private readonly List<object> fields;
        private readonly Delegate constructor;

        public ObjectCodec(List<object> fields, Delegate constructor)
        {
            this.fields = fields;
            this.constructor = constructor;
        }

        public T Decode(dynamic input)
        {
            var args = new List<object>();
            foreach (dynamic field in fields)
            {
                string fieldName = field.FieldName;
                dynamic fieldCodec = field.Codec;
                args.Add(fieldCodec.Decode(input[fieldName]));
            }

            // Call the constructor with the right number of args
            return (T)constructor.DynamicInvoke(args.ToArray());
        }

        public dynamic Encode(T value)
        {
            var result = new Dictionary<string, dynamic>();
            foreach (dynamic field in fields)
            {
                string fieldName = field.FieldName;
                dynamic fieldCodec = field.Codec;
                Delegate getter = field.Getter;

                dynamic fieldValue = getter.DynamicInvoke(value);
                result[fieldName] = fieldCodec.Encode(fieldValue);
            }
            return result;
        }
    }
}