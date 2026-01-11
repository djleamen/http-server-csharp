/**
 * HTTP Server - A simple HTTP server implementation in C#
 * From CodeCrafters.io build-your-own-http-server (C#)
 */

using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    Socket client = server.AcceptSocket();
    
    _ = Task.Run(() => HandleClient(client));
}

void HandleClient(Socket client)
{
    // Read the request
    byte[] buffer = new byte[1024];
    int bytesRead = client.Receive(buffer);
    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    
    // Parse the request
    string[] lines = request.Split("\r\n");
    string[] requestLine = lines[0].Split(' ');
    string path = requestLine[1];
    
    // Send the HTTP response
    string response;
    if (path == "/")
    {
        response = "HTTP/1.1 200 OK\r\n\r\n";
    }
    else if (path.StartsWith("/echo/"))
    {
        string echoStr = path.Substring(6);
        int contentLength = Encoding.UTF8.GetByteCount(echoStr);
        
        response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {contentLength}\r\n\r\n{echoStr}";
    }
    else if (path == "/user-agent")
    {
        string userAgent = "";
        foreach (string line in lines)
        {
            if (line.StartsWith("User-Agent:", StringComparison.OrdinalIgnoreCase))
            {
                userAgent = line.Substring("User-Agent:".Length).Trim();
                break;
            }
        }
        
        int contentLength = Encoding.UTF8.GetByteCount(userAgent);
        response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {contentLength}\r\n\r\n{userAgent}";
    }
    else
    {
        response = "HTTP/1.1 404 Not Found\r\n\r\n";
    }
    
    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
    client.Send(responseBytes);
    
    // Close the connection
    client.Close();
}
