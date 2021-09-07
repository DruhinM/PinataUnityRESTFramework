using Newtonsoft.Json;
//using Socket.Newtonsoft.Json;

namespace Common.Api
{
    public class ApiResponseFormat<T>
    {
        [JsonProperty("status")]
        public string Status;
        [JsonProperty("message")]
        public string Message;
        [JsonProperty("data")]
        public T Data;
    }
}