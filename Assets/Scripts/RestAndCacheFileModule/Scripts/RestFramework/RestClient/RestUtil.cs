#define DEBUG_REST_CALLS

using Common.Utils.Rest;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Common.Api;
using Common;
using static Common.Utils.Rest.WebRequestBuilder;

namespace Common.Utils
{
    public class RestUtil
    {
        private const long HttpOk = 200;
        private const long HttpCreated = 201;
        private const long HttpNoContent = 204;

        public UnityWebRequest CurrentRequest => _currentCall.Request;

        private bool Uploading => _currentCall.Request.method.Equals("POST");

        public float Progress
            => Uploading ? _currentCall.Request.uploadProgress : _currentCall.Request.downloadProgress;

        public ulong TransmittedBytes
            => Uploading ? _currentCall.Request.uploadedBytes : _currentCall.Request.downloadedBytes;

        private MonoBehaviour _monoBehaviour;
        private readonly Queue<Call> _callQueue = new Queue<Call>();
        private int _callCounter;
        private Coroutine _coroutine;
        private Call _currentCall;
        private bool _endGracefully;

        private RestUtil(MonoBehaviour monoBehaviour, bool autoStart = true)
        {
            _monoBehaviour = monoBehaviour;

            if (autoStart)
                Start();
        }

        public static RestUtil Initialize(MonoBehaviour monoBehaviour)
        {
            return new RestUtil(monoBehaviour);
        }

        public void Start()
        {
            if (_coroutine == null)
                _coroutine = _monoBehaviour.StartCoroutine(Run());
        }

        /// <summary>
        /// The utility will stop sending requests when the current request has finished. 
        /// </summary>
        public void StopGracefully()
        {
            _endGracefully = true;
        }

        public void ForceStop()
        {
            if (_coroutine != null)
                _monoBehaviour.StopCoroutine(_coroutine);
        }

        public void ForceDispose()
        {
            if (_currentCall != null)
            {
                _currentCall.Request.Dispose();
                _currentCall = null;
            }
        }

        /// <summary>
        /// Sends a web request over the network.
        /// </summary>
        /// <param name="builder">The builder that contains the web request data.</param>
        /// <param name="onCompletion">Function to be called when the request is completed successfully.</param>
        /// <param name="onError">Function to be called when the request fails.</param>
        /// <returns>An integer representing the number of the queued call.</returns>
        public int Send(WebRequestBuilder builder,
            Action<DownloadHandler> onCompletion, Action<RestCallError> onError)
        {
            Debug.Log($"Call In Queue {_callQueue.Count}");
            _callQueue.Enqueue(new Call()
            {
                Builder = builder,
                OnCompletion = onCompletion,
                OnError = onError
            });

            return _callCounter++;
        }

        public void SendAsync(WebRequestBuilder builder,
            Action<DownloadHandler> onCompletion, Action<RestCallError> onError)
        {
            AsyncCall asyncCall = new AsyncCall(_monoBehaviour, builder, onCompletion, onError);
            asyncCall.Start();
        }
        public void SendUploadAsync(UnityWebRequest request,
            Action<DownloadHandler> onCompletion, Action<RestCallError> onError, Action<float> uploadPercentage)
        {
            AsyncUploadCall asyncCall = new AsyncUploadCall(_monoBehaviour, request, onCompletion, onError, uploadPercentage);
            asyncCall.Start();
        }

        private IEnumerator Run()
        {
            do
            {
                do
                {
                    yield return new WaitForEndOfFrame();
                } while (_currentCall == null && _callQueue.Count == 0);

                _currentCall = _callQueue.Dequeue();
                _currentCall.Request = _currentCall.Builder.Build();
#if DEBUG_REST_CALLS
                Debug.LogFormat("Making {0} call to: {1} at {2}", _currentCall.Request.method, _currentCall.Request.url, Time.time, _currentCall.Builder.requestHeaders.Count);
#endif
                yield return _currentCall.Request.SendWebRequest();               

#if DEBUG_REST_CALLS
                Debug.LogFormat("Call {0} completed with status {1} at {2} and data {3}", _currentCall.Request.url,
                    _currentCall.Request.responseCode, Time.time, _currentCall.Request.downloadHandler.text);
#endif
                if (_currentCall.Request.responseCode == HttpOk || _currentCall.Request.responseCode == HttpCreated || _currentCall.Request.responseCode == HttpNoContent)
                {
                    _currentCall.OnCompletion(_currentCall.Request.downloadHandler);
                }
                else
                {
#if DEBUG_REST_CALLS
                    Debug.LogFormat("Called: {0}\nResponse: {1}", _currentCall.Request.url,
                        _currentCall.Request.downloadHandler.text);
#endif
                    var restCallError = new RestCallError()
                    {
                        Raw = _currentCall.Request.downloadHandler.text,
                        Code = _currentCall.Request.responseCode,
                        Headers = _currentCall.Request.GetResponseHeaders(),
                    };
                    var oauthResponse =
                        DataConverter.DeserializeObject<ApiResponseFormat<OauthErrorResponse>>(restCallError.Raw);
                    if (oauthResponse == null)
                    {
                        restCallError.Error = "no_connection";
                        restCallError.Description = "No internet connection. Check your network";
                    }
                    else if (oauthResponse.Data != null)
                    {
                        restCallError.Error = oauthResponse.Data.Error;
                        restCallError.Description = oauthResponse.Data.ErrorDescription;
                    }
                    else
                    {
                        var deSerializedData =
                            DataConverter.DeserializeObject<ApiResponseFormat<string>>(restCallError.Raw);
                        restCallError.Error = deSerializedData.Status.ToString();
                        restCallError.Description = deSerializedData.Message;
                    }

                    _currentCall.OnError(restCallError);
                }

                if (_currentCall != null)
                {
                    _currentCall.Request.Dispose();
                    _currentCall = null;
                }
            } while (!_endGracefully);
        }



        public struct RestCallError
        {
            public string Raw;
            public long Code;
            public string Error;
            public string Description;
            public Dictionary<string, string> Headers;
        }

        private class Call
        {
            public WebRequestBuilder Builder;
            public Action<DownloadHandler> OnCompletion;
            public Action<RestCallError> OnError;
            public UnityWebRequest Request;
        }

        public class AsyncCall
        {
            MonoBehaviour _monoBehaviour;
            Action<DownloadHandler> _onCompletion;
            Action<RestCallError> _onError;
            UnityWebRequest _Request;
            public AsyncCall(MonoBehaviour monoBehaviour, WebRequestBuilder builder, Action<DownloadHandler> onCompletion, Action<RestCallError> onError)
            {
                _monoBehaviour = monoBehaviour;
                _onCompletion = onCompletion;
                _onError = onError;
                _Request = builder.Build();
            }

            public void Start()
            {
                _monoBehaviour.StartCoroutine(Run());
            }

            IEnumerator Run()
            {
#if DEBUG_REST_CALLS
                Debug.Log($"Making {_Request.method} call to: {_Request.url} : {_Request.GetRequestHeader("Content-Type")}");
#endif
                yield return _Request.SendWebRequest();

#if DEBUG_REST_CALLS
                Debug.Log($"Call {_Request.url} completed with status {_Request.responseCode} and data {_Request.downloadHandler.text}");
#endif
                if (_Request.responseCode == HttpOk || _Request.responseCode == HttpCreated || _Request.responseCode == HttpNoContent)
                {
                    _onCompletion(_Request.downloadHandler);
                }
                else
                {
#if DEBUG_REST_CALLS
                    Debug.LogFormat("Called: {0}\nResponse: {1}", _Request.url,
                        _Request.downloadHandler.text);
#endif
                    var restCallError = new RestCallError()
                    {
                        Raw = _Request.downloadHandler.text,
                        Code = _Request.responseCode,
                        Headers = _Request.GetResponseHeaders(),
                    };
                    var oauthResponse =
                        DataConverter.DeserializeObject<ApiResponseFormat<OauthErrorResponse>>(restCallError.Raw);
                    if (oauthResponse == null)
                    {
                        restCallError.Error = "no_connection";
                        restCallError.Description = "No internet connection. Check your network";
                    }
                    else if (oauthResponse.Data != null)
                    {
                        restCallError.Error = oauthResponse.Data.Error;
                        restCallError.Description = oauthResponse.Data.ErrorDescription;
                    }
                    else
                    {
                        try
                        {
                            var deSerializedData =
                                DataConverter.DeserializeObject<ApiResponseFormat<string>>(restCallError.Raw);
                            restCallError.Error = deSerializedData.Message.ToString();
                            restCallError.Description = deSerializedData.Message.ToString();
                        } catch
                        {

                        }
                    }

                    _onError(restCallError);
                }

                _Request.Dispose();
            }
        }

        public class AsyncUploadCall
        {
            MonoBehaviour _monoBehaviour;
            Action<DownloadHandler> _onCompletion;
            Action<RestCallError> _onError;
            Action<float> _onUploadPercentage;
            UnityWebRequest _Request;

            public AsyncUploadCall(MonoBehaviour monoBehaviour, UnityWebRequest webRequest, Action<DownloadHandler> onCompletion, Action<RestCallError> onError,
                Action<float> uploadPercentage)
            {
                _monoBehaviour = monoBehaviour;
                _onCompletion = onCompletion;
                _onError = onError;
                _onUploadPercentage = uploadPercentage;
                _Request = webRequest;
                //_fileData = fileData;
                //_url = url;
            }

            public void Start()
            {
                _monoBehaviour.StartCoroutine(Run());
            }

            IEnumerator Run()
            {

#if DEBUG_REST_CALLS
                Debug.LogFormat("Making {0} call to: {1}", _Request.method, _Request.url);
#endif

                _Request.SendWebRequest();
                while (!_Request.isDone)
                {
                    _onUploadPercentage?.Invoke(_Request.uploadProgress);
                    yield return null;
                    //Debug.LogError($"Progress : {_Request.uploadProgress.ToString("F2")} : {_Request.uploadedBytes}");
                }

#if DEBUG_REST_CALLS
                Debug.LogFormat("Call {0} completed with status {1} at {2} and data {3}", _Request.url,
                    _Request.responseCode, Time.time, _Request.downloadHandler.text);
#endif
                if (_Request.responseCode == HttpOk || _Request.responseCode == HttpCreated || _Request.responseCode == HttpNoContent)
                {
                    _onCompletion(_Request.downloadHandler);
                }
                else
                {
#if DEBUG_REST_CALLS
                    Debug.LogFormat("Called: {0}\nResponse: {1}", _Request.url,
                        _Request.downloadHandler.text);
#endif
                    var restCallError = new RestCallError()
                    {
                        Raw = _Request.downloadHandler.text,
                        Code = _Request.responseCode,
                        Headers = _Request.GetResponseHeaders(),
                    };
                    var oauthResponse =
                        DataConverter.DeserializeObject<ApiResponseFormat<OauthErrorResponse>>(restCallError.Raw);
                    if (oauthResponse == null)
                    {
                        restCallError.Error = "no_connection";
                        restCallError.Description = "No internet connection. Check your network";
                    }
                    else if (oauthResponse.Data != null)
                    {
                        restCallError.Error = oauthResponse.Data.Error;
                        restCallError.Description = oauthResponse.Data.ErrorDescription;
                    }
                    else
                    {
                        var deSerializedData =
                            DataConverter.DeserializeObject<ApiResponseFormat<string>>(restCallError.Raw);
                        restCallError.Error = deSerializedData.Status.ToString();
                        restCallError.Description = "Error In API";
                    }

                    _onError(restCallError);
                }

                _Request.Dispose();
            }
        }
    }
}