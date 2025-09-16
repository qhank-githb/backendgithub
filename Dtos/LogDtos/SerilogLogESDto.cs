using Nest;

public class SerilogLogESDto
{
    [Date(Name = "@timestamp")]
    public DateTime AtTimestamp { get; set; }

    public string Message { get; set; }
    public string Level { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, object> Fields { get; set; }
}
