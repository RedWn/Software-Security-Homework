using Newtonsoft.Json;

class User
{
    public string Name;
    public string Password;
    public string Message;

    internal String ToJSON()
    {
        String outPut = JsonConvert.SerializeObject(this);
        Console.WriteLine(outPut);
        return outPut;
    }

}