using Newtonsoft.Json;

namespace BunnyCDN.Net.Storage
{
    internal class Serializer
    {
        /// <summary>
        /// Deserialize the object with the most optimal serializer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">JSON encoded data that will be deserialized</param>
        /// <returns>The deserialized object</returns>
        public static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        /// <summary>
        /// Serialize the object with the most optimal serializer
        /// </summary>
        /// <param name="value">The object that will be serialized</param>
        /// <returns>JSON serialized object</returns>
        public static string Serialize<T>(object value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}
