using Newtonsoft.Json;
using System.Text.Json;

namespace Mythosia
{
    public static class ObjectExtension
    {
        /// <summary>
        /// Converts the specified object to a JSON string using System.Text.Json.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">Optional JsonSerializerOptions to customize the serialization.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string ToJsonStringS<T>(this T obj, JsonSerializerOptions options = null)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Converts the specified object to a JSON string using Newtonsoft.Json.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="settings">Optional JsonSerializerSettings to customize the serialization.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string ToJsonStringN<T>(this T obj, JsonSerializerSettings settings = null)
        {
            return settings == null ? JsonConvert.SerializeObject(obj) : JsonConvert.SerializeObject(obj, settings);
        }



    }
}
