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

    private bool _allowInsecure;

    public GeminiClient(Uri baseUrl, bool allowInsecure)
    {
        if (baseUrl.Scheme != "gemini") throw new ArgumentException("baseUrl must be a gemini endpoint");

        _baseUrl = baseUrl;
        _allowInsecure = allowInsecure;
    }

    private void Init()
    {
        if (_hostAddress is not null) return;
        var hostAddresses = Dns.GetHostAddresses(_baseUrl.Host);
        _hostAddress = hostAddresses.FirstOrDefault();
        if (_hostAddress is null) throw new GeminiException("Could not find host");
    }

    private Stream Open()
    {
        Init();
        var port = _baseUrl.IsDefaultPort ? 1965 : _baseUrl.Port;
        Socket socket = new Socket(_hostAddress!.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(_hostAddress, port);

        NetworkStream networkStream = new NetworkStream(socket);
        var sslStream = new SslStream(networkStream);

        var options = new SslClientAuthenticationOptions();
        options.TargetHost = _baseUrl.Host;
        options.RemoteCertificateValidationCallback = Validate;

        sslStream.AuthenticateAsClient(options);

        return sslStream;
    }

    private bool Validate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        return _allowInsecure || sslPolicyErrors == SslPolicyErrors.None;
    }

    public GeminiResponse Get()
    {
        return Get("");
    }

    public GeminiResponse Get(string path)
    {
        var url = new Uri(_baseUrl, path);

        var stream = Open();

        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.NewLine = "\r\n";
        writer.WriteLine(url.ToString());
        writer.Flush();

        var u = new UTF8Encoding(false);

        // StreamReader can advance the stream too far so we'll just poor man it here.
        var headerStream = new MemoryStream();
        int state = 0;
        while (state < 2)
        {
            var b = stream.ReadByte();

            switch (b)
            {
                case -1: throw new GeminiException("No header or content was received");
                case 13: state = state == 0 ? 1 : 0; break;
                case 10: state = state == 1 ? 2 : 0; break;
                default: headerStream.WriteByte((byte)b); break;
            }
        }

        var header = Encoding.UTF8.GetString(headerStream.ToArray());

        var parts = header.Split(" ", 2, StringSplitOptions.None);
        if (parts.Length != 2) throw new GeminiException("Protocol error: header malformed.");
        if (!int.TryParse(parts[0], out int status)) throw new GeminiException("Protocol error: header malformed.");
        var meta = parts[1];

        return new GeminiResponse(status, meta, stream);
    }
}