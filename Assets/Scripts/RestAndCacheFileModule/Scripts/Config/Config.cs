using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public class Config : Singleton<Config>
    {

        #region Serialized Fields
#pragma warning disable 649
        [SerializeField]
        private Configuration configuration;
#pragma warning restore
        #endregion


        public class Api
        {
         
            public static string PinataHost { get { return Instance.configuration.Api.PinataHost; } }
   
            public static string PinataSecretKey { get { return Instance.configuration.Api.SecretKey; } }
            
            public static string PinataAPIKey { get { return Instance.configuration.Api.PinatatAPIKey; } }
        }
        
    }
}