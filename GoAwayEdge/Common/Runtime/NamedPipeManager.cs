using System.IO.Pipes;
using System.Text;
using GoAwayEdge.Common.Debugging;

namespace GoAwayEdge.Common.Runtime
{
    public class NamedPipeManager(string pipeName, int maxRetryAttempts = 3)
    {
        private bool _isServerRunning;
        private Thread? _pipeThread;
        private readonly object _lock = new();

        public event Action<string>? MessageReceived;
        public event Action<Exception>? ErrorOccurred;

        public void StartServer()
        {
            _isServerRunning = true;

            _pipeThread = new Thread(() =>
            {
                while (_isServerRunning)
                {
                    try
                    {
                        using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                        {
                            Logging.Log("Waiting for client connection...");
                            pipeServer.WaitForConnection();
                            
                            Logging.Log("Client connected.");
                            var buffer = new byte[256];
                            while (pipeServer.IsConnected)
                            {
                                try
                                {
                                    int bytesRead = pipeServer.Read(buffer, 0, buffer.Length);
                                    if (bytesRead > 0)
                                    {
                                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                        MessageReceived?.Invoke(message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    HandleError(ex);
                                    break;
                                }
                            }

                            Logging.Log("Client disconnected. Resetting pipe...");
                            pipeServer.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleError(ex);
                        Thread.Sleep(2000);
                    }
                }
            });

            _pipeThread.IsBackground = true;
            _pipeThread.Start();
        }

        public void SendMessage(string message)
        {
            int attempt = 0;

            while (attempt < maxRetryAttempts)
            {
                try
                {
                    using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                    {
                        Logging.Log("Attempting to connect to server...");
                        Logging.Log($"Reaching to pipe: {pipeName}");
                        pipeClient.Connect(10000);
                        Logging.Log("Connected to server.");

                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        pipeClient.Write(messageBytes, 0, messageBytes.Length);
                        pipeClient.Flush();
                        break;
                    }
                }
                catch (TimeoutException ex)
                {
                    attempt++;
                    HandleError(new TimeoutException("Connection timed out.", ex));
                    Thread.Sleep(2000);
                }
                catch (UnauthorizedAccessException ex)
                {
                    HandleError(ex);
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    HandleError(ex);
                    Thread.Sleep(1000);
                }
            }

            if (attempt == maxRetryAttempts)
            {
                Logging.Log("Unable to send message after multiple attempts.", Logging.LogLevel.ERROR);
            }
        }


        public void StopServer()
        {
            _isServerRunning = false;
            _pipeThread?.Join(); // Wait for ending tasks
        }

        private void HandleError(Exception ex)
        {
            Logging.Log($"Pipe error: {ex.Message}", Logging.LogLevel.ERROR);
            ErrorOccurred?.Invoke(ex);
        }
    }
}
