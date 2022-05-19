using System;
using System.Text;

namespace GeminiCurl;

public static class Program
{
    public static void Main(string[] args)
    {
        var url = args.FirstOrDefault();
        if (url is null) return;
        if (!url.Contains("//")) url = "gemini://" + url;

        var parameters = args.Skip(1).ToArray();
        var showHeader = parameters.Any(p => p == "-i" || p.StartsWith("--include-header"));
        var forceBinary = parameters.Any(p => p == "-b" || p == "--binary");
        var requireSecure = parameters.Any(p => p == "-s" || p == "--secure");

        var baseUrl = new Uri(url);
        var client = new GeminiClient(baseUrl, allowInsecure: !requireSecure);

        using var response = client.Get();

        if (showHeader)
        {
            Console.WriteLine($"{response.StatusCode} {response.Meta}");
        }

        if (response.Meta.StartsWith("text") && !forceBinary)
        {
            using var reader = new StreamReader(response.Content, Encoding.UTF8);
            while (!reader.EndOfStream) Console.WriteLine(reader.ReadLine());
        }
        else
        {
            using var console = Console.OpenStandardOutput();
            response.Content.CopyTo(console);
        }
    }
}