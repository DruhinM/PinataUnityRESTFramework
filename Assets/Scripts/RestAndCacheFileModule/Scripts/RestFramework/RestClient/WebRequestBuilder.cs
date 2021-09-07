using CarScan.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Networking;

namespace Common.Utils.Rest
{
    public class WebRequestBuilder : IDisposable
    {
        private UnityWebRequest webRequest;

        private string url;
        private string verb;
        public Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
        private Dictionary<string, object> formData = new Dictionary<string, object>();
        public DownloadHandler downloadHandler = new DownloadHandlerBuffer();
        private UploadHandler uploadHandler;
        private bool isMultiPart = false;

        //HACK the old way of doing things until they fix UnityWebRequest.Post

        public WebRequestBuilder()
        {
            Verb(Verbs.GET);
        }

        public WebRequestBuilder Url(string url)
        {
            this.url = url;
            return this;
        }

        /// <summary>
        /// Sets the call's method (GET, POST, etc) a.k.a verb
        /// </summary>
        public WebRequestBuilder Verb(Verbs verb)
        {
            this.verb = verb.ToString();
            return this;
        }

        public WebRequestBuilder Header(string name, string value)
        {
            if (requestHeaders.ContainsKey(name))
                requestHeaders[name] = value;
            else
                requestHeaders.Add(name, value);

            return this;
        }

        public WebRequestBuilder Headers(IDictionary<string, string> headers)
        {
            foreach (var entry in headers)
                Header(entry.Key, entry.Value);
            return this;
        }

        public WebRequestBuilder ContentType(string type)
        {
            return Header("Content-Type", type);
        }

        public WebRequestBuilder IsMultiPart()
        {
            isMultiPart = true;
            return this;
        }

        /// <summary>
        /// Attaches data to the rest call.
        /// </summary>
        /// <param name="data">The data to be sent as a byte array</param>
        /// <param name="mimeType">Leave null to send as binary data or see <code>OmegaTech.Utils.Rest.ContentTypes</code></param>
        public WebRequestBuilder Data(byte[] data, string mimeType = null)
        {
            if (uploadHandler == null)
                uploadHandler = new UploadHandlerRaw(data);

            uploadHandler.contentType = mimeType ?? ContentTypes.BINARY;
            return this;
        }

        /// <summary>
        /// Attaches string data to the rest call.
        /// </summary>
        /// <param name="data">The data to be sent as a string</param>
        /// <param name="mimeType">Leave null to send as plain text data or see <code>OmegaTech.Utils.Rest.ContentTypes</code></param>
        public WebRequestBuilder Data(string data, string mimeType = null)
        {
            return Data(data.GetBytes(), mimeType ?? ContentTypes.JSON);
        }

        /// <summary>
        /// Attaches multipart form data to the rest call.
        /// </summary>
        /// <param name="name">The key of the data value</param>
        /// <param name="data">The actual data</param>
        public WebRequestBuilder FormData(string name, string data)
        {
            if (formData.ContainsKey(name))
                formData[name] = data;
            else
                formData.Add(name, data);
            return this;
        }
        
        public WebRequestBuilder FormData(string name, byte[] data)
        {
            if (formData.ContainsKey(name))
                formData[name] = data;
            else
                formData.Add(name, data);
            return this;
        }

      

        /// <summary>
        /// Attaches multipart form data to the rest call.
        /// </summary>
        /// <param name="name">The key of the data value</param>
        /// <param name="data">The actual data</param>
        public WebRequestBuilder FormData(string name, FileData data)
        {
            if (formData.ContainsKey(name))
                formData[name] = data;
            else
                formData.Add(name, data);
            return this;
        }

        /// <summary>
        /// Attaches multipart form data to the rest call.
        /// </summary>
        /// <param name="name">The key of the data value</param>
        /// <param name="data">The actual data</param>
        public WebRequestBuilder FormData(string name, bool data)
        {
            if (formData.ContainsKey(name))
                formData[name] = data;
            else
                formData.Add(name, data);
            return this;
        }

        /// <summary>
        /// Attaches multipart form data to the rest call.
        /// </summary>
        /// <param name="name">The key of the data value</param>
        /// <param name="data">The actual data</param>
        public WebRequestBuilder FormData(string name, int data)
        {
            if (formData.ContainsKey(name))
                formData[name] = data;
            else
                formData.Add(name, data);
            return this;
        }

        public WebRequestBuilder Handler(DownloadHandler handler)
        {
            downloadHandler = handler;
            return this;
        }

        internal UnityWebRequest Build()
        {
            if (Verbs.POST.ToString().Equals(verb) || Verbs.PUT.ToString().Equals(verb))
            {
                var formData = new WWWForm();

                foreach (var item in this.formData)
                {
                    if (item.Value is int)
                    {
                        formData.AddField(item.Key, Convert.ToInt32(item.Value));
                    }
                    else if (item.Value is string)
                    {
                        formData.AddField(item.Key, item.Value.ToString());
                    }
                    else if (item.Value is FileData)
                    {
                        var data = (FileData)item.Value;
                        formData.AddBinaryData(item.Key, data.Bytes, data.Filename, data.Mime);
                    }
                    else if (item.Value is bool)
                    {
                        formData.AddField(item.Key, ((bool)item.Value) ? "1" : "0");
                    }
                }
                if (Verbs.POST.ToString().Equals(verb))
                {
                    webRequest = UnityWebRequest.Post(url, formData);
                }
                else if (Verbs.PUT.ToString().Equals(verb))
                {
                    webRequest = UnityWebRequest.Post(url, formData);
                    webRequest.method = Verbs.PUT.ToString();
                }
            }
            else if (Verbs.DELETE.ToString().Equals(verb))
            {
                webRequest = UnityWebRequest.Delete(url);
            }
            else
            {
                webRequest = new UnityWebRequest();
                webRequest.url = url;
            }

            if (downloadHandler == null)
                downloadHandler = new DownloadHandlerBuffer();

            webRequest.downloadHandler = downloadHandler;

            if (uploadHandler != null)
                webRequest.uploadHandler = uploadHandler;

            //webRequest.SetRequestHeader("Accept", "application/json");
            //webRequest.SetRequestHeader("Access-Control-Allow-Origin", "*");

            foreach (var item in requestHeaders)
                webRequest.SetRequestHeader(item.Key, item.Value);

            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();

            return webRequest;
        }

        internal UnityWebRequest BuildMultipart()
        {
            if (Verbs.POST.ToString().Equals(verb) || Verbs.PUT.ToString().Equals(verb))
            {
                List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

                foreach (var item in this.formData)
                {
                    formData.Add(new MultipartFormDataSection(item.Key, (string)item.Value));
                    Debug.LogError($"Multipart Section {item.Key} : {(string)item.Value}");
                }
                if (Verbs.POST.ToString().Equals(verb))
                {
                    Debug.LogError($"{url} : {formData.ToString()}");
                    webRequest = UnityWebRequest.Post(url, formData);
                }
                else if (Verbs.PUT.ToString().Equals(verb))
                {
                    webRequest = UnityWebRequest.Post(url, formData);
                    webRequest.method = Verbs.PUT.ToString();
                }
            }
            else if (Verbs.DELETE.ToString().Equals(verb))
            {
                webRequest = UnityWebRequest.Delete(url);
            }
            else
            {
                webRequest = new UnityWebRequest();
                webRequest.url = url;
            }

            if (downloadHandler == null)
                downloadHandler = new DownloadHandlerBuffer();

            webRequest.downloadHandler = downloadHandler;

            if (uploadHandler != null)
                webRequest.uploadHandler = uploadHandler;

            foreach (var item in requestHeaders)
                webRequest.SetRequestHeader(item.Key, item.Value);

            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();

            return webRequest;
        }

        public void Dispose()
        {
            webRequest?.Dispose();
        }

        public struct FileData
        {
            public byte[] Bytes;
            public string Filename;
            public string Mime;
        }

        public struct ImageData
        {
            public byte[] Bytes;
        }
    }
}

public class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
{
    // Encoded RSAPublicKey
    //private static string PUB_KEY = "3082010A0282010100B1196A928D280B8A5CF6E1FE46583091461D477999A9073AE664530CBA3EB5498F1DE749C3BD2D9A0EDF7D196C803273196911868EA4AEDF17F1AA3A6488A97014C12D9F27B8F63F3A30938F67DCE131077CEFA2C502BEA916715C133C1E0D9526563D8C5624ED6A6412DD58950C1F537DB0E2AAADE27F5C6B30E0E9A7439D3D3B9D078C94AF41A787965045773D8B7D8D7B3B5F65BC206C1A5A8982C7CFCCE6898D3710712D19E66BE016322BB2177D51EA94500840154BC525BE856EEBAF31E05ECB43344714EDB22CEE6E0E90C66344D6E3BFD71AFD4C32EB0F7121852EE23BF7B52C8332E7C8044B9E0F9C253A12EDA097F0CA5C37FC5EF73E5C69C846070203010001";

    protected override bool ValidateCertificate(byte[] certificateData)
    {
        X509Certificate2 certificate = new X509Certificate2(certificateData);
  
        return true;
    }
}