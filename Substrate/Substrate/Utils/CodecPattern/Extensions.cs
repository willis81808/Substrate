using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace Substrate.Utils.CodecPattern
{
    public static class JsonExtensions
    {
        public static string ToJson<T>(this ICodec<T> codec, T value, bool formatted = true)
        {
            // Step 1: Use the codec to convert to intermediate format
            var intermediate = codec.Encode(value);

            // Step 2: Convert intermediate to JToken (Newtonsoft's JSON representation)
            var jsonObject = ConvertToJToken(intermediate);

            // Step 3: Serialize to JSON string
            return JsonConvert.SerializeObject(jsonObject,
                formatted ? Formatting.Indented : Formatting.None);
        }

        private static JToken ConvertToJToken(dynamic value)
        {
            if (value == null)
                return JValue.CreateNull();

            if (value is string or int or double or bool or byte)
                return new JValue(value);

            if (value is Dictionary<string, dynamic> strDict)
            {
                var obj = new JObject();
                foreach (var pair in strDict)
                {
                    obj[pair.Key] = ConvertToJToken(pair.Value);
                }
                return obj;
            }

            if (value is IAttribute attr)
            {
                if (attr.GetValue() is JValue jValue)
                {
                    return jValue;
                }
                else
                {
                    return new JValue(attr.GetValue());
                }
            }

            // Handle dictionaries with non-string keys
            if (value is IDictionary<dynamic, dynamic> dict)
            {
                // For non-string keys, represent as array of key-value pairs
                var array = new JArray();
                foreach (var pair in dict)
                {
                    var entryObj = new JObject
                    {
                        ["key"] = ConvertToJToken(pair.Key),
                        ["value"] = ConvertToJToken(pair.Value)
                    };
                    array.Add(entryObj);
                }
                return array;
            }

            if (value is IEnumerable<dynamic> list)
            {
                var array = new JArray();
                foreach (var item in list)
                {
                    array.Add(ConvertToJToken(item));
                }
                return array;
            }

            // Other types can be added as needed
            throw new NotSupportedException($"Unsupported type for JSON conversion: {value.GetType()}");
        }
        
        public static T FromJson<T>(this ICodec<T> codec, string json)
        {
            try
            {
                var jToken = JToken.Parse(json);
                var intermediate = ConvertFromJToken(jToken);
                return codec.Decode(intermediate);
            }
            catch (Exception e)
            {
                return default(T);
            }
        }

        private static dynamic ConvertFromJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    // Check if this is a key-value pair representation (for non-string keys)
                    if (token.Children<JProperty>().Count() == 2 &&
                        token.Children<JProperty>().Any(p => p.Name == "key") &&
                        token.Children<JProperty>().Any(p => p.Name == "value"))
                    {
                        // This is a single key-value pair, but we'll let the array handler process collections of these
                        return ConvertFromJToken(token);
                    }

                    var dict = new Dictionary<string, dynamic>();
                    foreach (var prop in token.Children<JProperty>())
                    {
                        dict[prop.Name] = ConvertFromJToken(prop.Value);
                    }
                    return dict;

                case JTokenType.Array:
                    // Check if this is an array of key-value pairs (for dictionaries with non-string keys)
                    if (token.Count() > 0 &&
                        token.Children().All(item =>
                            item.Type == JTokenType.Object &&
                            item.Children<JProperty>().Count() == 2 &&
                            item.Children<JProperty>().Any(p => p.Name == "key") &&
                            item.Children<JProperty>().Any(p => p.Name == "value")))
                    {
                        var nonStringDict = new Dictionary<dynamic, dynamic>();
                        foreach (var item in token.Children())
                        {
                            dynamic key = ConvertFromJToken(item["key"]);
                            dynamic value = ConvertFromJToken(item["value"]);
                            nonStringDict[key] = value;
                        }
                        return nonStringDict;
                    }

                    // Regular array
                    var list = new List<dynamic>();
                    foreach (var item in token.Children())
                    {
                        list.Add(ConvertFromJToken(item));
                    }
                    return list;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                    return ((JValue)token).Value;

                default:
                    throw new NotSupportedException($"Unsupported JSON token type: {token.Type}");
            }
        }
    }

    public static class TreeAttributesExtensions
    {
        public static TreeAttribute ToTreeAttributes<T>(this ICodec<T> codec, T value)
        {
            // Use the codec to convert to intermediate format
            var intermediate = codec.Encode(value);

            // Convert intermediate to TreeAttribute
            return ConvertToTreeAttribute(intermediate);
        }

        public static T FromTreeAttributes<T>(this ICodec<T> codec, TreeAttribute treeAttribute)
        {
            try
            {
                // Convert TreeAttribute to intermediate format
                var intermediate = ConvertFromTreeAttribute(treeAttribute);

                // Use the codec to convert from intermediate format
                return codec.Decode(intermediate);
            }
            catch (Exception e)
            {
                return default(T);
            }
        }

        private static TreeAttribute ConvertToTreeAttribute(dynamic value)
        {
            if (value == null)
                return null;

            var tree = new TreeAttribute();

            if (value is Dictionary<string, dynamic> dict)
            {
                foreach (var pair in dict)
                {
                    tree[pair.Key] = ConvertValueToIAttribute(pair.Value);
                }
            }
            else
            {
                throw new ArgumentException("Root value must be a dictionary for TreeAttribute serialization");
            }

            return tree;
        }

        private static IAttribute ConvertValueToIAttribute(dynamic value)
        {
            if (value == null)
                return null;

            // Handle primitive types
            if (value is int intValue)
                return new IntAttribute(intValue);
            if (value is byte byteValue)
                return new IntAttribute(byteValue);
            if (value is long longValue)
                return new LongAttribute(longValue);
            if (value is float floatValue)
                return new FloatAttribute(floatValue);
            if (value is double doubleValue)
                return new DoubleAttribute(doubleValue);
            if (value is bool boolValue)
                return new BoolAttribute(boolValue);
            if (value is string stringValue)
                return new StringAttribute(stringValue);

            // Handle array types
            if (value is byte[] byteArray)
                return new ByteArrayAttribute(byteArray);
            if (value is bool[] boolArray)
                return new BoolArrayAttribute(boolArray);
            if (value is int[] intArray)
                return new IntArrayAttribute(intArray);
            if (value is long[] longArray)
                return new LongArrayAttribute(longArray);
            if (value is float[] floatArray)
                return new FloatArrayAttribute(floatArray);
            if (value is double[] doubleArray)
                return new DoubleArrayAttribute(doubleArray);
            if (value is string[] stringArray)
                return new StringArrayAttribute(stringArray);

            // Handle nested objects
            if (value is Dictionary<string, dynamic> dict)
            {
                var tree = new TreeAttribute();
                foreach (var pair in dict)
                {
                    tree[pair.Key] = ConvertValueToIAttribute(pair.Value);
                }
                return tree;
            }

            // Handle collections (lists, etc.) by converting to TreeAttribute with numbered keys
            if (value is IEnumerable<dynamic> collection)
            {
                var tree = new TreeAttribute();
                tree["_isCollection"] = new BoolAttribute(true);

                int index = 0;
                foreach (var item in collection)
                {
                    tree[index.ToString()] = ConvertValueToIAttribute(item);
                    index++;
                }

                tree["count"] = new IntAttribute(index);
                return tree;
            }

            // Handle dictionaries with non-string keys
            if (value is IDictionary<dynamic, dynamic> nonStringDict)
            {
                var tree = new TreeAttribute();
                tree["_isDictionary"] = new BoolAttribute(true);

                int index = 0;
                foreach (var pair in nonStringDict)
                {
                    var pairTree = new TreeAttribute();
                    pairTree["key"] = ConvertValueToIAttribute(pair.Key);
                    pairTree["value"] = ConvertValueToIAttribute(pair.Value);
                    tree[index.ToString()] = pairTree;
                    index++;
                }

                tree["count"] = new IntAttribute(index);
                return tree;
            }

            throw new NotSupportedException($"Unsupported type for TreeAttribute conversion: {value.GetType()}");
        }

        private static dynamic ConvertFromTreeAttribute(TreeAttribute tree)
        {
            // Check if this is a specially encoded collection
            if (tree.HasAttribute("_isCollection") && ((BoolAttribute)tree["_isCollection"]).value)
            {
                var count = ((IntAttribute)tree["count"]).value;
                var list = new List<dynamic>();

                for (int i = 0; i < count; i++)
                {
                    list.Add(ConvertFromIAttribute(tree[i.ToString()]));
                }

                return list;
            }

            // Check if this is a specially encoded dictionary with non-string keys
            if (tree.HasAttribute("_isDictionary") && ((BoolAttribute)tree["_isDictionary"]).value)
            {
                var count = ((IntAttribute)tree["count"]).value;
                var dict = new Dictionary<dynamic, dynamic>();

                for (int i = 0; i < count; i++)
                {
                    var pairTree = (TreeAttribute)tree[i.ToString()];
                    dynamic key = ConvertFromIAttribute(pairTree["key"]);
                    dynamic value = ConvertFromIAttribute(pairTree["value"]);
                    dict[key] = value;
                }

                return dict;
            }

            // Regular TreeAttribute
            var result = new Dictionary<string, dynamic>();
            foreach (var entry in tree)
            {
                // Skip metadata attributes
                if (entry.Key == "_isCollection" || entry.Key == "_isDictionary" || entry.Key == "count")
                    continue;

                result[entry.Key] = ConvertFromIAttribute(entry.Value);
            }
            return result;
        }

        private static dynamic ConvertFromIAttribute(IAttribute attribute)
        {
            if (attribute == null)
                return null;

            switch (attribute)
            {
                case IntAttribute intAttr:
                    return intAttr.value;
                case LongAttribute longAttr:
                    return longAttr.value;
                case FloatAttribute floatAttr:
                    return floatAttr.value;
                case DoubleAttribute doubleAttr:
                    return doubleAttr.value;
                case BoolAttribute boolAttr:
                    return boolAttr.value;
                case StringAttribute stringAttr:
                    return stringAttr.value;
                case ByteArrayAttribute byteArrayAttr:
                    return byteArrayAttr.value;
                case BoolArrayAttribute boolArrayAttr:
                    return boolArrayAttr.value;
                case IntArrayAttribute intArrayAttr:
                    return intArrayAttr.value;
                case LongArrayAttribute longArrayAttr:
                    return longArrayAttr.value;
                case FloatArrayAttribute floatArrayAttr:
                    return floatArrayAttr.value;
                case DoubleArrayAttribute doubleArrayAttr:
                    return doubleArrayAttr.value;
                case StringArrayAttribute stringArrayAttr:
                    return stringArrayAttr.value;
                case TreeAttribute treeAttr:
                    return ConvertFromTreeAttribute(treeAttr);
                default:
                    throw new NotSupportedException($"Unsupported IAttribute type: {attribute.GetType()}");
            }
        }
    }
}
