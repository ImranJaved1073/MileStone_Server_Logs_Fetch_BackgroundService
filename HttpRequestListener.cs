using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using log4net;
using Newtonsoft.Json;

public class HttpRequestListener
{
    private static readonly ILog Logger = LogManager.GetLogger("RollingFileAppender");
    private readonly HttpListener _listener;
    private HttpListenerContext _currentContext;

    public event Action<string> RequestReceived;

    public HttpRequestListener(string prefix)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
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
            Logger.Error("Failed to stop HTTP listener.");
            MessageBox.Show("Failed to stop HTTP listener.");
            _listener.Stop();
            //_listener.Close();
        }
        else
            Logger.Info("HTTP listener stopped.");
    }
}
