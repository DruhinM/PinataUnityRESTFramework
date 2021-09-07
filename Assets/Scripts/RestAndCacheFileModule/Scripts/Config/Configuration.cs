using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Common
{
    [CreateAssetMenu(menuName = "Api Config")]
    public class Configuration : ScriptableObject
    {
        public ApiConfiguration Api = new ApiConfiguration();


        [Serializable]
        public class ApiConfiguration
        {
           
            public string PinataHost = "htpps://api.pinata.cloud";
            public string SecretKey = "";
            public string PinatatAPIKey = "";
        }
        
    }
}