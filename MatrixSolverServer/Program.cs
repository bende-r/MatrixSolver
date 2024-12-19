using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using OpenTelemetry;

namespace MatrixSolverServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<ISolverService, SolverService>();

            var app = builder.Build();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.Map("/ws", WebSocketHandler);

            string serverUrl = "http://localhost:5145";
            Console.WriteLine($"Сервер запущен по адресу: {serverUrl}");
            app.Run(serverUrl);
        }

        private static readonly TaskQueue TaskQueue = new TaskQueue(5); // Пулинг задач с ограничением на 5 одновременных задач

        private static async Task WebSocketHandler(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("WebSocket соединение установлено.");

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // Таймаут на 5 минут для сеанса
                try
                {
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    using var ms = new MemoryStream();

                    // Получение данных
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, cts.Token);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    var requestJson = Encoding.UTF8.GetString(ms.ToArray());
                    var solverService = context.RequestServices.GetRequiredService<ISolverService>();

                    // Валидация JSON
                    if (string.IsNullOrWhiteSpace(requestJson))
                    {
                        throw new ArgumentException("Получен пустой запрос.");
                    }

                    // Добавление задачи в очередь
                    await TaskQueue.Enqueue(async () =>
                    {
                        try
                        {
                            var request = JsonSerializer.Deserialize<MatrixRequest>(requestJson);
                            solverService.ValidateMatrixRequest(request);

                            // Выполняем вычисления
                            var stripeSolutionTask = Task.Run(() => solverService.SolveSLAUWithStripeMultiplication(request.Matrix, request.Vector));
                            //  var gaussSolutionTask = Task.Run(() => solverService.SolveSLAUWithGauss(request.Matrix, request.Vector));

                            await Task.WhenAll(stripeSolutionTask);
                            // await Task.WhenAll(stripeSolutionTask, gaussSolutionTask);

                            var response = new
                            {
                                StripeSolution = stripeSolutionTask.Result,
                                //  GaussSolution = gaussSolutionTask.Result
                            };
                            var responseJson = JsonSerializer.Serialize(response);
                            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                            await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cts.Token);
                            logger.LogInformation("Результаты отправлены клиенту.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Ошибка при обработке задачи.");
                            var errorBytes = Encoding.UTF8.GetBytes($"{{\"Error\": \"{ex.Message}\"}}");
                            await webSocket.SendAsync(new ArraySegment<byte>(errorBytes), WebSocketMessageType.Text, true, cts.Token);
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("WebSocket сеанс завершён из-за таймаута.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сеанс завершён из-за таймаута", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Необработанная ошибка.");
                }
                finally
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сеанс завершён", CancellationToken.None);
                    }
                    logger.LogInformation("WebSocket соединение закрыто.");
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
    }
}
