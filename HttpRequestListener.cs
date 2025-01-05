using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using log4net;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Net.Sockets;

public class HttpRequestListener
{
    private static readonly ILog Logger = LogManager.GetLogger("RollingFileAppender");
    private readonly HttpListener _listener;
    private HttpListenerContext _currentContext;
    private readonly string _jwtSecretKey;

    public event Action<string> RequestReceived;

    public HttpRequestListener(string prefix, string jwtSecretKey)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _jwtSecretKey = jwtSecretKey;
    }
    public void Start()
    {
        try
        {
            _listener.Start();
            Logger.Info("HTTP listener started.");
            MessageBox.Show("Listening for incoming HTTP requests...");

            while (true)
            {
                _currentContext = _listener.GetContext();
                HandleRequest(_currentContext);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error in Start method.", ex);
            MessageBox.Show($"Error in Start method: {ex.Message}");
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        string responseString;

        try
        {
            string path = context.Request.Url.AbsolutePath;
            Logger.Info($"Received request: {context.Request.HttpMethod} {path}");

            if (!ValidateToken(context))
            {
                SendError("Unauthorized access.");
                return;
            }

            if (context.Request.HttpMethod == "POST")
            {
                if (path.Equals("/logFetch/", StringComparison.OrdinalIgnoreCase))
                {
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        string requestBody = reader.ReadToEnd();
                        Logger.Debug($"Received request body: {requestBody}");


                        RequestReceived?.Invoke(requestBody);
                        responseString = "Request processed successfully.";
                    }
                }
                else if (path.Equals("/connect/", StringComparison.OrdinalIgnoreCase))
                {
                    responseString = "Connected.";
                    SendResponse(responseString);
                }
                else
                {
                    responseString = "Invalid route or HTTP method.Only /logFetch/ is supported.";
                    Logger.Warn($"Invalid request: {context.Request.HttpMethod} {path}");
                }
            }
            else
            {
                responseString = "Only POST requests are supported.";
                Logger.Warn($"Invalid request: {context.Request.HttpMethod} {path}");
            }

            //SendResponse(responseString);
        }
        catch (Exception ex)
        {
            Logger.Error("Error handling request.", ex);
            SendError("Internal server error.");
        }
    }

    private bool ValidateToken(HttpListenerContext context)
    {
        try
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                Logger.Warn("Authorization header missing or invalid.");
                return false;
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();


            return token == _jwtSecretKey;
        }
        catch (Exception ex)
        {
            Logger.Error("Token validation failed.", ex);
            return false;
        }
    }

    public void SendResponse(string responseBody)
    {
        if (_currentContext != null)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
            _currentContext.Response.ContentLength64 = buffer.Length;
            _currentContext.Response.OutputStream.Write(buffer, 0, buffer.Length);
            _currentContext.Response.OutputStream.Close();
            Logger.Info("Response sent to client.");
        }
    }

    public void SendError(string errorMessage)
    {
        if (_currentContext != null)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(errorMessage);
            _currentContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            _currentContext.Response.ContentLength64 = buffer.Length;
            _currentContext.Response.OutputStream.Write(buffer, 0, buffer.Length);
            _currentContext.Response.OutputStream.Close();
            Logger.Error($"Error response sent to client: {errorMessage}");
        }
    }

    public void Stop()
    {

        if (_listener.IsListening)
        {
            _listener.Stop();
            Logger.Info("HTTP listener stopped.");
            MessageBox.Show("HTTP listener stopped.");
        }

    }
}
