using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class ChainOptions
{

    public string PrivateKeyForCallTx = "838183d5cf676d17a3aa8daff3c70952d27285101509fcb686c74b7e9d200d62";
    public Dictionary<string, ChainInfo> ChainInfos { get; set; } = new();
    
    
    public class ChainInfo
    {
        public string BaseUrl { get; set; }
        public bool IsMainChain { get; set; }
        public Dictionary<string, Dictionary<string, string>> ContractAddress { get; set; } = new();
    }
}