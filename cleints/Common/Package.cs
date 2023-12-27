public class Package
{
    public string encryption;
    public string type;

    public Dictionary<string, string>? body;

    public Package(string encryption, string type)
    {
        this.encryption = encryption;
        this.type = type;
    }
}
