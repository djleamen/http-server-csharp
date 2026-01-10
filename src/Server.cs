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
    else
    {
        response = "HTTP/1.1 404 Not Found\r\n\r\n";
    }
    
    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
    client.Send(responseBytes);
    
    // Close the connection
    client.Close();
}
