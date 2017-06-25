using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Extensions.MonoHttp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Switch
{
    public class SwitchDBClient
    {
        internal readonly DatabaseOptions Options;
        private readonly string Location;
        private readonly string ConnectionAddress;

        private string AccessToken;

        #region WebSocket Clients
        private WebSocket wsListClient;
        private WebSocket wsAddClient;
        private WebSocket wsSetClient;
        private WebSocket wsDeleteClient;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchDBClient"/> class.
        /// </summary>
        /// <param name="baseUrl">Database connection URL</param>
        /// <param name="options">Database options</param>
        public SwitchDBClient (string location, DatabaseOptions options)
        {
            this.Location = location;
            this.Options = options ?? throw new Exception("Database options required.");

            switch (Options.ConnectionType)
            {
                case ConnectionType.HTTP:
                    ConnectionAddress = string.Format("http://{0}.switchapi.com/", Location);
                    break;
                case ConnectionType.HTTPS:
                    ConnectionAddress = string.Format("https://{0}.switchapi.com/", Location);
                    break;
                case ConnectionType.WebSocket:
                    ConnectionAddress = string.Format("ws://127.0.0.1:8000/", Location);
                    break;
            }
        }

        public void Connect()
        {
            GenerateAccessToken();
        }

        public void Abort()
        {
            if (Options.ConnectionType == ConnectionType.WebSocket)
            {
                if (wsListClient.State == WebSocketState.Open)
                {
                    wsListClient.Close();
                }
                if (wsAddClient.State == WebSocketState.Open)
                {
                    wsAddClient.Close();
                }
                if (wsSetClient.State == WebSocketState.Open)
                {
                    wsSetClient.Close();
                }
                if (wsDeleteClient.State == WebSocketState.Open)
                {
                    wsDeleteClient.Close();
                }
            }
        }

        private void GenerateAccessToken()
        {
            // ConnectionAddress kullanılmamalı. AccessToken her zaman HTTPS ile alınmalı.
            RestClient restClient = new RestClient(string.Format("https://{0}.switchapi.com/", Location));
            string hashedSignature;

            using (var md5 = MD5.Create())
            {
                var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0}{1}", Options.APISecret, GetUnixTimestampMillis(Options.ConnectionExpire))));
                hashedSignature = ToHex(hashedBytes);
            }

            RestRequest request = new RestRequest("Token", Method.GET);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("APIKey", Options.APIKey);
            request.AddHeader("Signature", hashedSignature);
            request.AddHeader("Expire", GetUnixTimestampMillis(Options.ConnectionExpire).ToString());

            EventWaitHandle handle = new AutoResetEvent(false);
            restClient.ExecuteAsync<dynamic>(request, r => {
                if (r.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("API Error: List");
                }

                AccessToken = JsonConvert.DeserializeObject<dynamic>(r.Content)["AccessToken"].Value;
                handle.Set();
            });
            handle.WaitOne();

            if (Options.ConnectionType == ConnectionType.WebSocket)
            {
                wsListClient = InitSocket(wsListClient, "List");
                wsAddClient = InitSocket(wsAddClient, "Add");
                wsSetClient = InitSocket(wsSetClient, "Set");
                wsDeleteClient = InitSocket(wsDeleteClient, "Delete");
            }
        }

        public List<dynamic> List(Query query)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                throw new Exception("Database not connected.");
            }

            switch (Options.ConnectionType)
            {
                case ConnectionType.HTTP:
                case ConnectionType.HTTPS:
                    RestClient restClient = new RestClient(ConnectionAddress);

                    RestRequest request = new RestRequest("List", Method.POST);
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("APIKey", Options.APIKey);
                    request.AddHeader("AccessToken", AccessToken);
                    request.AddParameter("application/json", QueryBuilder.Build(query), ParameterType.RequestBody);

                    var cancellationTokenSource = new CancellationTokenSource();

                    var tcs = new TaskCompletionSource<List<dynamic>>();

                    restClient.ExecuteAsync(request, r =>
                    {
                        if (r.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            tcs.SetException(new Exception("API Error: List"));
                        }
                        else
                        {
                            tcs.SetResult(JsonConvert.DeserializeObject<List<dynamic>>(r.Content));
                        }
                    });

                    return tcs.Task.Result;
                case ConnectionType.WebSocket:
                    if (wsListClient == null || wsListClient.State != WebSocketState.Open)
                    {
                        wsListClient = InitSocket(wsListClient, "List");
                    }

                    string response = string.Empty;
                    AutoResetEvent waitHandle = new AutoResetEvent(false);

                    EventHandler<MessageReceivedEventArgs> messageHandler = delegate (object sender, MessageReceivedEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Message;
                    };

                    EventHandler<SuperSocket.ClientEngine.ErrorEventArgs> errorHandler = delegate (object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Exception.Message;
                        throw e.Exception;
                    };

                    wsListClient.MessageReceived += messageHandler;
                    wsListClient.Error += errorHandler;
                    wsListClient.Send(QueryBuilder.Build(query));
                    waitHandle.WaitOne();

                    return JsonConvert.DeserializeObject<List<dynamic>>(response);
                default:
                    throw new Exception("Connection type null.");
            }
        }

        public dynamic Add(string ListName, object item)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                throw new Exception("Database not connected.");
            }

            switch (Options.ConnectionType)
            {
                case ConnectionType.HTTP:
                case ConnectionType.HTTPS:
                    RestClient restClient = new RestClient(ConnectionAddress);

                    RestRequest request = new RestRequest("Add", Method.POST);
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("APIKey", Options.APIKey);
                    request.AddHeader("AccessToken", AccessToken);
                    request.AddHeader("List", ListName);
                    request.AddParameter("application/json", JsonConvert.SerializeObject(item), ParameterType.RequestBody);

                    var cancellationTokenSource = new CancellationTokenSource();

                    var tcs = new TaskCompletionSource<List<dynamic>>();

                    restClient.ExecuteAsync(request, r =>
                    {
                        if (r.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            tcs.SetException(new Exception("API Error: Add"));
                        }
                        else
                        {
                            tcs.SetResult(JsonConvert.DeserializeObject<dynamic>(r.Content));
                        }
                    });

                    return tcs.Task.Result;
                case ConnectionType.WebSocket:
                    if (wsAddClient == null || wsAddClient.State != WebSocketState.Open)
                    {
                        wsAddClient = InitSocket(wsAddClient, "Add");
                    }

                    string response = string.Empty;
                    AutoResetEvent waitHandle = new AutoResetEvent(false);

                    EventHandler<MessageReceivedEventArgs> messageHandler = delegate (object sender, MessageReceivedEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Message;
                    };

                    EventHandler<SuperSocket.ClientEngine.ErrorEventArgs> errorHandler = delegate (object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Exception.Message;
                        throw e.Exception;
                    };

                    wsAddClient.MessageReceived += messageHandler;
                    wsAddClient.Error += errorHandler;
                    wsAddClient.Send(JsonConvert.SerializeObject(item));
                    waitHandle.WaitOne();

                    return JsonConvert.DeserializeObject<dynamic>(response);
                default:
                    throw new Exception("Connection type null.");
            }
        }

        public dynamic Set(string ListName, object item)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                throw new Exception("Database not connected.");
            }

            switch (Options.ConnectionType)
            {
                case ConnectionType.HTTP:
                case ConnectionType.HTTPS:
                    RestClient restClient = new RestClient(ConnectionAddress);

                    RestRequest request = new RestRequest("Set", Method.POST);
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("APIKey", Options.APIKey);
                    request.AddHeader("AccessToken", AccessToken);
                    request.AddHeader("List", ListName);
                    request.AddParameter("application/json", JsonConvert.SerializeObject(item), ParameterType.RequestBody);

                    var cancellationTokenSource = new CancellationTokenSource();

                    var tcs = new TaskCompletionSource<List<dynamic>>();

                    restClient.ExecuteAsync(request, r =>
                    {
                        if (r.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            tcs.SetException(new Exception("API Error: Set"));
                        }
                        else
                        {
                            tcs.SetResult(JsonConvert.DeserializeObject<dynamic>(r.Content));
                        }
                    });

                    return tcs.Task.Result;
                case ConnectionType.WebSocket:
                    if (wsSetClient == null || wsSetClient.State != WebSocketState.Open)
                    {
                        wsSetClient = InitSocket(wsSetClient, "Set");
                    }

                    string response = string.Empty;
                    AutoResetEvent waitHandle = new AutoResetEvent(false);

                    EventHandler<MessageReceivedEventArgs> messageHandler = delegate (object sender, MessageReceivedEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Message;
                    };

                    EventHandler<SuperSocket.ClientEngine.ErrorEventArgs> errorHandler = delegate (object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Exception.Message;
                        throw e.Exception;
                    };

                    wsSetClient.MessageReceived += messageHandler;
                    wsSetClient.Error += errorHandler;
                    wsSetClient.Send(JsonConvert.SerializeObject(item));
                    waitHandle.WaitOne();

                    return JsonConvert.DeserializeObject<dynamic>(response);
                default:
                    throw new Exception("Connection type null.");
            }
        }

        public dynamic Delete(string ListName, string ListItemId)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                throw new Exception("Database not connected.");
            }

            switch (Options.ConnectionType)
            {
                case ConnectionType.HTTP:
                case ConnectionType.HTTPS:
                    RestClient restClient = new RestClient(ConnectionAddress);

                    RestRequest request = new RestRequest("Set", Method.DELETE);
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("APIKey", Options.APIKey);
                    request.AddHeader("AccessToken", AccessToken);
                    request.AddHeader("List", ListName);
                    request.AddHeader("ListItemId", ListItemId);

                    var cancellationTokenSource = new CancellationTokenSource();

                    var tcs = new TaskCompletionSource<List<dynamic>>();

                    restClient.ExecuteAsync(request, r =>
                    {
                        if (r.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            tcs.SetException(new Exception("API Error: Set"));
                        }
                        else
                        {
                            tcs.SetResult(JsonConvert.DeserializeObject<dynamic>(r.Content));
                        }
                    });

                    return tcs.Task.Result;
                case ConnectionType.WebSocket:
                    if (wsDeleteClient == null || wsDeleteClient.State != WebSocketState.Open)
                    {
                        wsDeleteClient = InitSocket(wsDeleteClient, "Delete");
                    }

                    string response = string.Empty;
                    AutoResetEvent waitHandle = new AutoResetEvent(false);

                    EventHandler<MessageReceivedEventArgs> messageHandler = delegate (object sender, MessageReceivedEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Message;
                    };

                    EventHandler<SuperSocket.ClientEngine.ErrorEventArgs> errorHandler = delegate (object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
                    {
                        waitHandle.Set();
                        response = e.Exception.Message;
                        throw e.Exception;
                    };

                    wsDeleteClient.MessageReceived += messageHandler;
                    wsDeleteClient.Error += errorHandler;
                    wsDeleteClient.Send(ListItemId);
                    waitHandle.WaitOne();

                    return JsonConvert.DeserializeObject<dynamic>(response);
                default:
                    throw new Exception("Connection type null.");
            }
        }

        #region Third Party Services

        public class ThirdPartyServices
        {
            public class SendGrid
            {
                public static dynamic SendMail(SwitchDBClient dbClient, SendGridMail email)
                {
                    if (string.IsNullOrEmpty(dbClient.AccessToken))
                    {
                        throw new Exception("SwitchDBClient not connected.");
                    }

                    RestClient restClient = new RestClient(dbClient.ConnectionAddress);

                    RestRequest request = new RestRequest("SendGrid/Send", Method.POST);
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("APIKey", dbClient.Options.APIKey);
                    request.AddHeader("AccessToken", dbClient.AccessToken);
                    request.AddParameter("application/json", JsonConvert.SerializeObject(email), ParameterType.RequestBody);

                    var cancellationTokenSource = new CancellationTokenSource();

                    var tcs = new TaskCompletionSource<List<dynamic>>();

                    restClient.ExecuteAsync(request, r =>
                    {
                        if (r.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            tcs.SetException(new Exception("API Error: Set"));
                        }
                        else
                        {
                            tcs.SetResult(JsonConvert.DeserializeObject<dynamic>(r.Content));
                        }
                    });

                    return tcs.Task.Result;
                }
            }
        }

        #endregion

        #region Switch Helpers
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long GetUnixTimestampMillis(DateTime dateTime)
        {
            return (long)(dateTime - UnixEpoch).TotalMilliseconds;
        }

        private static string ToHex(byte[] bytes, bool upperCase = false)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        private WebSocket InitSocket(WebSocket socket, string Method)
        {
            socket = new WebSocket(string.Format("{0}{3}?apiKey={1}&token={2}", ConnectionAddress, Options.APIKey, HttpUtility.UrlEncode(AccessToken), Method));
            socket.AutoSendPingInterval = 10000;
            socket.EnableAutoSendPing = true;

            AutoResetEvent waitHandle = new AutoResetEvent(false);

            socket.Opened += delegate (object sender, EventArgs e) { waitHandle.Set(); };
            socket.Open();
            waitHandle.WaitOne();

            return socket;
        }
        #endregion
    }
}