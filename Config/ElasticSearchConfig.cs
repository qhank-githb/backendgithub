using Nest;

public static class ElasticSearchConfig
{
    public static IElasticClient CreateClient()
    {
        var settings = new ConnectionSettings(new Uri("http://192.168.150.93:9200"))
            .DefaultIndex("files"); // 默认索引

        return new ElasticClient(settings);
    }
}
