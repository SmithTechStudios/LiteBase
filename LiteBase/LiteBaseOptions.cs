namespace LiteBase;

public class LiteBaseOptions
{
    public required string DbPath { get; set; }
    public FirebaseConfig? Firebase { get; set; }
}

public class FirebaseConfig
{
    public required string ApiKey { get; set; }
    public required string AuthDomain { get; set; }
    public required string ProjectId { get; set; }
    public required string StorageBucket { get; set; }
    public required string MessagingSenderId { get; set; }
    public required string AppId { get; set; }
}
