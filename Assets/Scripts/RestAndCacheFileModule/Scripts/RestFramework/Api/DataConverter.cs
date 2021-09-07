using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Common
{
    public static class DataConverter
    {

        private static readonly DefaultContractResolver _contractResolver =
            new DefaultContractResolver
            {
                //NamingStrategy = new SnakeCaseNamingStrategy()
            };

        private static readonly JsonSerializerSettings _serializerSettings =
            new JsonSerializerSettings() 
            {
                ContractResolver = _contractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
        };

       

        private static bool _customConvertersAdded = false;

        static DataConverter()
        {
            addCustomConverters();
        }

        public static string SerializeObject<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None, _serializerSettings);
        }

        public static T DeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
        }

        public static T DeserializeAnonymousType<T>(string json, T anonymousTypeObject)
        {
            return JsonConvert.DeserializeAnonymousType<T>(json, anonymousTypeObject, _serializerSettings);
        }

        private static void addCustomConverters()
        {
            if (_customConvertersAdded)
                return;
            //_serializerSettings.Converters.Add(new BooleanConverter().);
        }
    }
}
