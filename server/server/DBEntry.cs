namespace server
{
    internal class DBEntry
    {
        public string role;
        public string publicKey;
        public string password;

        public DBEntry(string role, string publicKey, string password)
        {
            this.role = role;
            this.publicKey = publicKey;
            this.password = password;
        }
    }
}
