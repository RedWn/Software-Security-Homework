using Newtonsoft.Json;

public class Package
{
    public string encryption;
    public string type;
    public string? signature;

    public Dictionary<string, string> body = [];


    public Package(string encryption, string type, Dictionary<string, string> body, string? signature = null)
    {
        this.encryption = encryption;
        this.type = type;
        this.body = body;
        this.signature = signature;
    }

    public static Package FromJSON(string jsonData)
    {
        var decodedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData); // This is a package object
        var decodedBody = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedData["body"].ToString());

        var p = new Package(decodedData["encryption"].ToString(), decodedData["type"].ToString(), decodedBody);
        if (decodedData.TryGetValue("signature", out var signature)) p.signature = decodedData["signature"].ToString();

        return p;
    }
}
