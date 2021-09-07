using System;
using Common.Utils.Rest;
using Common.Api;
using UnityEngine;
using RestUtil = Common.Utils.RestUtil;
using RestError = Common.Utils.RestUtil.RestCallError;
using static Common.Utils.Rest.WebRequestBuilder;

namespace Common
{
    public class RestManager : Singleton<RestManager>
    {
        private RestUtil _restUtil;

        private bool IsInternetReachable => (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) || (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork);

        public static bool IsInternetConnected => Instance.IsInternetReachable;


        public static string AuthToken
        {
            get;
            set;
        }

        protected override void Awake()
        {
            base.Awake();
            _restUtil = RestUtil.Initialize(this);
        }

        public static void PinFileToIpfs(FileData file, 
            string pinataMetadata, string pinataOptions,Action<PinataResponse> onCompletion, Action<RestError> onError )
        {
            var builder = new WebRequestBuilder()
                .Url(FormatApiUrlPinata(Urls.PIN_FILE_TO_IPFS))
                .Verb(Verbs.POST)
                .FormData(Attributes.file, file.Bytes)
                .FormData(Attributes.pinataMetadata, pinataMetadata)
                .FormData(Attributes.pinataOptions, pinataOptions);
             

            AddPinataAuthHeader(ref builder); // API key
           
            SendWebRequest(builder, onCompletion, onError);
        }
        
        public static void PinJSONToIpfs(string data, Action<PinataResponse> onCompletion, Action<RestError> onError )
        {
            var builder = new WebRequestBuilder()
                .Url(FormatApiUrlPinata(Urls.PIN_JSON_TO_IPFS))
                .Verb(Verbs.POST)
                .Data(data, ContentTypes.JSON)
                .ContentType(ContentTypes.JSON);

            Debug.Log("JSON is " + data);
            AddPinataAuthHeader(ref builder); // API key
           
            SendWebRequest(builder, onCompletion, onError);
        }

        

        private static void SendWebRequest(WebRequestBuilder builder, Action onCompletion, Action<RestError> onError)
        {
            if (!IsInternetConnected)
            {
                RestError error = new RestError()
                {
                    Error = "No Internet",
                    Description = "No internet connection. Check your network",
                    Code = 502
                };
                onError?.Invoke(error);
                return;
            }
            Instance._restUtil.SendAsync(builder, handler => { onCompletion?.Invoke(); },
                restError => InterceptError(restError, () => onError?.Invoke(restError), onError));
        }

        private static void SendWebRequest<T>(WebRequestBuilder builder, Action<T> onCompletion,
            Action<RestError> onError = null)
        {
            if (!IsInternetConnected)
            {
                RestError error = new RestError()
                {
                    Error = "No Internet",
                    Description = "No internet connection. Check your network",
                    Code = 502
                };
                onError?.Invoke(error);
                return;
            }
            Instance._restUtil.SendAsync(builder,
                handler =>
                {
                    Debug.Log($"data : {handler.text}");
                    //var response = DataConverter.DeserializeObject<ApiResponseFormat<T>>(handler.text);
                    var response = DataConverter.DeserializeObject<T>(handler.text);
                    if (response == null)
                    {
                        Instance._restUtil.ForceDispose();
                    }
                    //onCompletion?.Invoke(response.Data);
                    onCompletion?.Invoke(response);
                },
                restError => InterceptError(restError, () => onError?.Invoke(restError), onError));
        }

        private static void InterceptError(RestError error, Action onSuccess,
            Action<RestError> defaultOnError)
        {
            defaultOnError?.Invoke(error);
        }


        
        protected static string FormatApiUrlPinata(string path, params object[] args)
        {
            try
            {
                return string.Format($"{Config.Api.PinataHost}{path}", args);
            } catch
            {
                throw new Exception("Please add RestManager Prefab into the scene");
            }
        }


        
        private static void AddPinataAuthHeader(ref WebRequestBuilder builder)
        {
            builder.Header("pinata_api_key", $"{Config.Api.PinataAPIKey}");
            builder.Header("pinata_secret_api_key", $"{Config.Api.PinataSecretKey}");
            
        }
    }
}