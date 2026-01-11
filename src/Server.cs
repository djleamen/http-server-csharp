/**
 * HTTP Server - A simple HTTP server implementation in C#
 * From CodeCrafters.io build-your-own-http-server (C#)
 */

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO.Compression;

// Parse command line arguments
string? directory = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--directory" && i + 1 < args.Length)
    {
        directory = args[i + 1];
        break;
    }
}

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    Socket client = server.AcceptSocket();
    
    _ = Task.Run(() => HandleClient(client, directory));
}

void HandleClient(Socket client, string? directory)
{
    byte[] buffer = new byte[1024];
    
    while (true)
    {
        // Read the request
        int bytesRead = client.Receive(buffer);
        
        // Client closed the connection
        if (bytesRead == 0)
        {
            break;
        }
        
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        // Parse the request
        string[] lines = request.Split("\r\n");
        string[] requestLine = lines[0].Split(' ');
        string method = requestLine[0];
        string path = requestLine[1];
        
        string acceptEncoding = "";
        string connectionHeader = "";
        
        foreach (string line in lines)
        {
            if (line.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase))
            {
                acceptEncoding = line.Substring("Accept-Encoding:".Length).Trim();
            }
            else if (line.StartsWith("Connection:", StringComparison.OrdinalIgnoreCase))
            {
                connectionHeader = line.Substring("Connection:".Length).Trim();
            }
        }
        
        // Send the HTTP response
        string response;
        if (path == "/")
        {
            response = "HTTP/1.1 200 OK\r\n\r\n";
        }
        else if (path.StartsWith("/echo/"))
        {
            string echoStr = path.Substring(6);
            
            bool supportsGzip = acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase);
            
            if (supportsGzip)
            {
                byte[] uncompressedBytes = Encoding.UTF8.GetBytes(echoStr);
                byte[] compressedBytes;
                
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gzipStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                    }
                    compressedBytes = memoryStream.ToArray();
                }
                
                int compressedLength = compressedBytes.Length;
                string headers = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Encoding: gzip\r\nContent-Length: {compressedLength}\r\n\r\n";
                byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
                
                byte[] fullResponse = new byte[headerBytes.Length + compressedBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, fullResponse, 0, headerBytes.Length);
                Buffer.BlockCopy(compressedBytes, 0, fullResponse, headerBytes.Length, compressedBytes.Length);
                
                client.Send(fullResponse);
                
                // Close connection if requested
                if (connectionHeader.Equals("close", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                continue;
            }
            else
            {
                int contentLength = Encoding.UTF8.GetByteCount(echoStr);
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {contentLength}\r\n\r\n{echoStr}";
            }
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
        else if (path.StartsWith("/files/"))
        {
            string filename = path.Substring(7);
            
            if (directory != null)
            {
                string filePath = Path.Combine(directory, filename);
                
                if (method == "POST")
                {
                    int contentLength = 0;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            string lengthStr = line.Substring("Content-Length:".Length).Trim();
                            contentLength = int.Parse(lengthStr);
                            break;
                        }
                    }
                    
                    int bodyStartIndex = request.IndexOf("\r\n\r\n") + 4;
                    string body = request.Substring(bodyStartIndex, contentLength);
                    
                    File.WriteAllText(filePath, body);
                    
                    response = "HTTP/1.1 201 Created\r\n\r\n";
                }
                // GET request for file
                else if (File.Exists(filePath))
                {
                    byte[] fileContent = File.ReadAllBytes(filePath);
                    int contentLength = fileContent.Length;
                    
                    string headers = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {contentLength}\r\n\r\n";
                    byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
                    
                    byte[] fullResponse = new byte[headerBytes.Length + fileContent.Length];
                    Buffer.BlockCopy(headerBytes, 0, fullResponse, 0, headerBytes.Length);
                    Buffer.BlockCopy(fileContent, 0, fullResponse, headerBytes.Length, fileContent.Length);
                    
                    client.Send(fullResponse);
                    
                    // Close connection if requested
                    if (connectionHeader.Equals("close", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    continue;
                }
                else
                {
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                }
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }
        }
        else
        {
            response = "HTTP/1.1 404 Not Found\r\n\r\n";
        }
        
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        client.Send(responseBytes);
        
        if (connectionHeader.Equals("close", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }
    }
    
    // Close the connection
    client.Close();
}
