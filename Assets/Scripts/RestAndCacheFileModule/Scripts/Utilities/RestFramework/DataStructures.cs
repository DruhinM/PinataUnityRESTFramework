using System.Collections.Generic;
using UnityEngine;
using System;



public struct ApiResponseData
{
    public string Raw;
  
}

public struct PresignUrlDetails
{
    public string uploadurls;
}

public struct ApiResultData
{

}


 // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public struct Meta
    {
    }

    public struct Pin
    {
        public string cid { get; set; }
        public string name { get; set; }
        public Meta meta { get; set; }
        public string status { get; set; }
        public DateTime created { get; set; }
        public int size { get; set; }
    }

    public struct File
    {
        public string name { get; set; }
        public string type { get; set; }
    }

    public struct Deal
    {
        public string batchRootCid { get; set; }
        public DateTime lastChange { get; set; }
        public string miner { get; set; }
        public string network { get; set; }
        public string pieceCid { get; set; }
        public string status { get; set; }
        public string statusText { get; set; }
        public int chainDealID { get; set; }
        public DateTime dealActivation { get; set; }
        public DateTime dealExpiration { get; set; }
    }

    public struct Value
    {
        public string cid { get; set; }
        public int size { get; set; }
        public DateTime created { get; set; }
        public string type { get; set; }
        public string scope { get; set; }
        public Pin pin { get; set; }
        public List<File> files { get; set; }
        public List<Deal> deals { get; set; }
    }

    public struct NFTStorage
    {
        public bool ok { get; set; }
        public Value value { get; set; }
    }
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
public struct Attribute
{
    public string trait_type { get; set; }
    public object value { get; set; }
}

public struct RariableJSON
{
    public string description { get; set; }
    public string external_url { get; set; }
    public string image { get; set; }
    public string name { get; set; }
    public List<Attribute> attributes { get; set; }
}

public struct PinataResponse
{
    public string IpfsHash { get; set; }
    public int PinSize { get; set; }
    public DateTime Timestamp { get; set; }
}


public struct RewardsData
{
    public string points;
}


public struct GamePlayData
{
    public string game_play_avail;
}

