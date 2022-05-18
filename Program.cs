using System;

namespace GeminiCurl;

public static class Program {
    public static void Main(string[] args)
    {
        var url = args.FirstOrDefault();
        if(url is null) return;
        if(!url.Contains("//")) url = "gemini://" + url;

        var parameters = args.Skip(1).ToArray();
        var showHeader = parameters.Any(p => p == "-i");

        var baseUrl = new Uri(url);
        var client = new GeminiClient(baseUrl);

        var response = client.Get();

        if(showHeader)
        {
            Console.WriteLine($"{response.StatusCode} {response.Meta}");
        }

        Console.WriteLine(response.Content);
    }
}