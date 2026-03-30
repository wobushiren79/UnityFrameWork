using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Object = UnityEngine.Object;

[CreateAssetMenu]
public class UnityNewtonsoftJsonSerializer : ScriptableObject
{

    #region Static Section
    //Not expected to work with UnityEditor types as well, since it won't ignore types like Editor, EditorWindow, PropertyDrawer, etc. -- with their fields.
    private class UnityImitatingContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Any data types whose fields we don't want to serialize. When any of these types are encountered during serialization,
        /// all of their fields will be skipped.
        /// </summary>
        private static readonly Type[] IgnoreTypes = new Type[] {
            typeof(Object),
            typeof(MonoBehaviour),
            typeof(ScriptableObject)
        };
        private static bool IsIgnoredType(Type type) => Array.FindIndex(IgnoreTypes, (Type current) => current == type) >= 0;

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            List<FieldInfo> allFields = new List<FieldInfo>();
            Type unityObjType = typeof(Object);

            for (Type t = type; t != null && !IsIgnoredType(t); t = t.BaseType)
            {
                FieldInfo[] currentTypeFields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < currentTypeFields.Length; i++)
                {
                    FieldInfo field = currentTypeFields[i];
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                        continue;
                    if (unityObjType.IsAssignableFrom(field.FieldType))
                    {
                        Debug.LogError("Failed to serialize a Unity object reference -- this is not supported by the " + GetType().Name
                            + ". Ignoring the property. (" + type.Name + "'s \"" + field.Name + "\" field)");
                        continue;
                    }
                    allFields.Add(field);
                }
            }
            //This sorts them based on the order they were actually written in the source code.
            //Beats me why Reflection wouldn't list them in that order to begin with, but whatever, this works
            allFields.Sort((a, b) => a.MetadataToken - b.MetadataToken);

            List<JsonProperty> properties = new List<JsonProperty>(allFields.Count);
            for (int i = 0; i < allFields.Count; i++)
            {
                int index = properties.FindIndex((JsonProperty current) => current.UnderlyingName == allFields[i].Name);
                if (index >= 0)
                    continue;
                JsonProperty property = CreateProperty(allFields[i], memberSerialization);
                property.Writable = true;
                property.Readable = true;
                properties.Add(property);
                //Debug.Log(property.PropertyName + " was added.");
            }
            return properties;
        }
    }

    private class FloatConverter : JsonConverter<float>
    {
        private int decimalPlaces;
        private string format;

        public int DecimalPlaces
        {
            get { return decimalPlaces; }
            set
            {
                decimalPlaces = Mathf.Clamp(value, 0, 8);
                format = "F" + decimalPlaces;
            }
        }

        public FloatConverter(int decimalPlaces)
        {
            DecimalPlaces = decimalPlaces;
        }

        public override void WriteJson(JsonWriter writer, float value, JsonSerializer serializer)
        {
            writer.WriteValue(float.Parse(value.ToString(format)));
        }

        public override float ReadJson(JsonReader reader, Type objectType, float existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            //For some reason, reader.Value is giving back a double and casting to a float did not go so well, from object to float.
            //And I didn't want to hard code 2 consecutive casts, literally, like "(float) (double) reader.Value", so I'm glad this works:
            return Convert.ToSingle(reader.Value);
        }
    }
    #endregion

    [SerializeField] private bool prettyPrint = true;

    private JsonSerializerSettings settings = new JsonSerializerSettings()
    {
        ContractResolver = new UnityImitatingContractResolver(),
        Converters = new JsonConverter[] {
            new FloatConverter(3)
        }

    };

    public string Serialize<T>(T obj)
    {
        string text;
        Formatting formatting = prettyPrint ? Formatting.Indented : Formatting.None;
        settings.Formatting = formatting;
        try
        {
            //For now, as I am unsure how I want to move forward with looping references, I'll just have it try first -- if it comes up, show an error
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
            text = JsonConvert.SerializeObject(obj, settings);
        }
        catch (JsonSerializationException e)
        {
            //and then go to these statements and ignore any looping references. This way, it lets us know if it DOES come across looping references.
            //Which aren't supported as this code is written currently.
            Debug.LogException(e);
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            text = JsonConvert.SerializeObject(obj, settings);
        }
        return text;
    }

    public T Deserialize<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text, settings);
    }
}