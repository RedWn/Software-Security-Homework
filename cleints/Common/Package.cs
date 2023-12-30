using Newtonsoft.Json;

public class Package
{
    public string encryption;
    public string type;

    public Dictionary<string, string>? body;

    public Package(string encryption, string type, Dictionary<string, string>? body = null)
    {
        this.encryption = encryption;
        this.type = type;
        this.body = body;
    }

    public static Package FromClientData(string jsonData)
    {
        var decodedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
        var decodedBody = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedData["body"].ToString());

        return new Package(decodedData["encryption"].ToString(), decodedData["type"].ToString(), decodedBody);
    }

}
