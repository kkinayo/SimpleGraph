using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleGraph
{
    internal class BinanceWebSocketService : IDisposable
    {
        private bool _isSubscribed;
        private string _tickerSymbol = string.Empty;
        private readonly Uri _binanceWebSocketUri = new Uri("wss://testnet.binance.vision/ws");
        private ClientWebSocket _webSocket;

        public delegate void MessageReceivedHandler(string message, DateTime receivedTime);

        public event MessageReceivedHandler? MessageReceived;

        public BinanceWebSocketService()
        {
            _webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                    return;

                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(_binanceWebSocketUri, CancellationToken.None);
                Debug.WriteLine("Connected to Binance WebSocket.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error connecting to WebSocket: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket.State != WebSocketState.Open)
                return;

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                Debug.WriteLine("Disconnected from Binance WebSocket.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disconnecting from WebSocket: {ex.Message}");
            }
        }

        public async Task SubscribeAsync(string tickerSymbol)
        {
            if (_webSocket.State != WebSocketState.Open || string.IsNullOrEmpty(tickerSymbol))
                return;

            string subscriptionMessage = $"{{\"method\": \"SUBSCRIBE\", \"params\": [\"{tickerSymbol.ToLower()}@bookTicker\"], \"id\": 1}}";
            await SendMessageAsync(subscriptionMessage);

            if (await ReceiveConfirmationAsync())
            {
                _isSubscribed = true;
                _tickerSymbol = tickerSymbol;
                Debug.WriteLine($"Subscribed to {tickerSymbol}.");
            }
        }

        public async Task UnsubscribeAsync()
        {
            if (_webSocket.State != WebSocketState.Open || !_isSubscribed)
                return;

            string unsubscriptionMessage = $"{{\"method\": \"UNSUBSCRIBE\", \"params\": [\"{_tickerSymbol.ToLower()}@bookTicker\"], \"id\": 1}}";
            await SendMessageAsync(unsubscriptionMessage);

            if (await ReceiveConfirmationAsync())
            {
                _isSubscribed = false;
                _tickerSymbol = string.Empty;
                Debug.WriteLine("Unsubscribed successfully.");
            }
        }

        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            if (!_isSubscribed)
                return;

            var buffer = new byte[4096];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.WriteLine("WebSocket closed by server.");
                        await DisconnectAsync();
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(message, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving messages: {ex.Message}");
                Dispose();
            }
        }

        private async Task SendMessageAsync(string message)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        private async Task<bool> ReceiveConfirmationAsync()
        {
            var buffer = new byte[1024];
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                return response.Contains("\"result\":null");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving confirmation: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_webSocket != null)
            {
                _webSocket.Dispose();
                Debug.WriteLine("WebSocket disposed.");
            }
        }
    }
}