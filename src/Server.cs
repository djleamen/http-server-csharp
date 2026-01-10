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
    client.Receive(buffer);
    
    // Send the HTTP response
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
    client.Send(responseBytes);
    
    // Close the connection
    client.Close();
}
