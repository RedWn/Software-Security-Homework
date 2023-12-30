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
}
