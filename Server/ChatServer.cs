using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Server.Info;

namespace Server
{
    internal class ChatServer : IDisposable
    {
        TcpListener? listener;

        CuncurrentHashSet<ClientInfo> clients; // добавляет только уникальных пользователей

        ConcurrentDictionary<string, ConcurrentQueue<MessageInfo>> messageCache; // у каждого пользователя своя очередь сообщений

        public ChatServer(IPEndPoint? endPoint)
        {
            if (endPoint != null)
                listener = new TcpListener(endPoint);

            messageCache = new ConcurrentDictionary<string, ConcurrentQueue<MessageInfo>>();

            clients = new CuncurrentHashSet<ClientInfo>();
        }

        /// <summary>
        /// Запуск сервера
        /// </summary>
        /// <returns></returns>
        public void Run(object? state, bool timeOut)
        {
            try
            {
                if (listener != null)
                    listener.Start();

                Console.Out.WriteLineAsync("Запущен");

                if (listener != null)
                    while (true)
                    {
                        TcpClient? tcpClient = listener.AcceptTcpClient();

                        ClientInfo newClient = new ClientInfo(Guid.NewGuid().ToString(), tcpClient);

                        clients.Add(newClient);

                        Task entry = ProcessClient(newClient);

                        Console.WriteLine($"Клиент {tcpClient.GetHashCode()} Успешно подключен");

                        using (StreamWriter writer = new StreamWriter(tcpClient.GetStream(), leaveOpen: true))
                        {
                            writer.WriteLineAsync("Сервер: соединение установленно");
                            writer.FlushAsync();
                        };
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Прослушивание входящих и отправка всем остальным клиентам сообщений
        /// </summary>
        /// <param name="producer">присоединяющийся клиент</param>
        /// <returns></returns>
        async Task ProcessClient(ClientInfo producer)
        {
            try
            {
                messageCache.TryAdd(producer.Id, new ConcurrentQueue<MessageInfo>());

                using var reader = new StreamReader(producer.Client.GetStream());

                string? message;

                while (!string.IsNullOrEmpty(message = await reader.ReadLineAsync())) // если сообщение не пустое
                {
                    messageCache[producer.Id].Enqueue(new MessageInfo { Id = producer.Id, Message = message, SendTime = DateTime.Now });

                    Console.WriteLine($"{message}");

                    while (messageCache[producer.Id].TryDequeue(out MessageInfo? messageInfo))
                    {
                        foreach (var consumer in clients)
                        {
                            // Создаем StreamWriter для отправки, не сохраняя его
                            using var writer = new StreamWriter(consumer.Client.GetStream(), leaveOpen: true);

                            if (consumer.Id != producer.Id)
                            {
                                try
                                {
                                    await writer.WriteLineAsync($"{producer.Id}\n{messageInfo.SendTime}: {messageInfo.Message}");
                                    await writer.FlushAsync(); // Убедимся, что сообщение отправлено немедленно
                                }
                                catch
                                {
                                    // Обработка ошибок отправки
                                }
                            }
                            else
                            {
                                await writer.WriteLineAsync("Сообщение доставлено.");
                                await writer.FlushAsync();
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке клиента: {ex.Message}");
            }
            finally // высвобождаем ресурсы
            {
                lock (clients)
                {
                    clients.Remove(producer);
                }
                producer.Client.GetStream().Close();
                producer.Client.Close();
            }
        }

        public void Dispose()
        {
            if (listener != null)
            {
                listener.Stop();
                listener.Server.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
