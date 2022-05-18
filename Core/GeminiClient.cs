using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GeminiCurl;

class GeminiClient
{
    private readonly Uri _baseUrl;
    private IPAddress? _hostAddress;

    public GeminiClient(Uri baseUrl)
    {
        if (baseUrl.Scheme != "gemini") throw new ArgumentException("baseUrl must be a gemini endpoint");

        _baseUrl = baseUrl;
    }

    private void Init()
    {
        if (_hostAddress is not null) return;
        var hostAddresses = Dns.GetHostAddresses(_baseUrl.Host);
        _hostAddress = hostAddresses.FirstOrDefault();
        if (_hostAddress is null) throw new Exception("Could not find host");
    }

    private Stream Open()
    {
        Init();
        var port = _baseUrl.IsDefaultPort ? 1965 : _baseUrl.Port;
        Socket socket = new Socket(_hostAddress!.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(_hostAddress, port);

        NetworkStream networkStream = new NetworkStream(socket);
        SslStream sslStream = new SslStream(networkStream);

        var options = new SslClientAuthenticationOptions();
        options.TargetHost = _baseUrl.Host;
        options.RemoteCertificateValidationCallback = Validate;

        sslStream.AuthenticateAsClient(options);

        return sslStream;
    }

    private bool Validate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        //Console.WriteLine(certificate?.ToString() ?? "null");
        return true;
    }

    public GeminiResponse Get() {
        return Get("");
    }

    public GeminiResponse Get(string path)
    {
        var url = new Uri(_baseUrl, path);

        using var stream = Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.NewLine = "\r\n";

        writer.WriteLine(url.ToString());
        writer.Flush();

        using var reader = new StreamReader(stream, Encoding.UTF8);

        var header = reader.ReadLine();
        var parts = header.Split(" ", 2, StringSplitOptions.None);
        var statusCode = Int32.Parse(parts[0]);
        var meta = parts[1];
        var content = reader.ReadToEnd();

        return new GeminiResponse(statusCode, meta, content);
    }
}