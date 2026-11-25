using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using JobTot.Application.Features;

namespace JobTot.Api;

public sealed class ChatSockets(ILogger<ChatSockets> logger) : IChatEvents
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, (ClaimsPrincipal User, DateTimeOffset Expires)> tickets = new();
    private readonly ConcurrentDictionary<Guid, (Guid Account, Channel<object> Queue)> clients = new();
    public string Ticket(ClaimsPrincipal user)
    {
        foreach (var item in tickets.Where(x => x.Value.Expires < DateTimeOffset.UtcNow))
            tickets.TryRemove(item.Key, out _);
        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        tickets[ticket] = (new ClaimsPrincipal(user), DateTimeOffset.UtcNow.AddSeconds(30));
        return ticket;
    }
    public ClaimsPrincipal? Consume(string? ticket) => ticket is not null && tickets.TryRemove(ticket, out var entry)
        && entry.Expires > DateTimeOffset.UtcNow ? entry.User : null;

    public void Publish(IEnumerable<Guid> accounts, ChatEvent notification)
    {
        var recipients = accounts.ToHashSet();
        foreach (var client in clients.Values.Where(c => recipients.Contains(c.Account)))
            if (!client.Queue.Writer.TryWrite(notification))
                client.Queue.Writer.TryComplete();
    }

    public async Task Run(HttpContext context, ClaimsPrincipal user)
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var account = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(user.FindFirstValue("exp")!));
        lifetime.CancelAfter(expiry > DateTimeOffset.UtcNow ? expiry - DateTimeOffset.UtcNow : TimeSpan.Zero);
        var ct = lifetime.Token;
        var queue = Channel.CreateBounded<object>(new BoundedChannelOptions(128) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
        var id = Guid.NewGuid();
        clients[id] = (account, queue);
        var writer = Write();
        try
        {
            var buffer = new byte[24 * 1024];
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var size = 0;
                WebSocketReceiveResult frame;
                do
                {
                    frame = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, size, buffer.Length - size), ct);
                    if (frame.MessageType == WebSocketMessageType.Close)
                        return;
                    size += frame.Count;
                    if (frame.MessageType != WebSocketMessageType.Text || size >= buffer.Length)
                        return;
                } while (!frame.EndOfMessage);
                string? requestId = null;
                try
                {
                    var command = JsonSerializer.Deserialize<Command>(buffer.AsSpan(0, size), Json)!;
                    if (command is null || string.IsNullOrWhiteSpace(command.Id) || command.Data.ValueKind != JsonValueKind.Object)
                        throw new JsonException("Invalid chat command.");
                    requestId = command.Id;
                    await using var scope = context.RequestServices.CreateAsyncScope();
                    var valid = user.IsInRole("Employer")
                        ? await scope.ServiceProvider.GetRequiredService<EmployerTokens>().ValidateAsync(user, ct)
                        : await scope.ServiceProvider.GetRequiredService<CandidateTokens>().ValidateAsync(user, ct);
                    if (!valid)
                        return;
                    var service = scope.ServiceProvider.GetRequiredService<AccountMessagesService>();
                    var result = command.Type switch
                    {
                        "send" => await service.Send(command.ConversationId, command.Data.Deserialize<AccountMessagesService.SendInput>(Json)!, ct),
                        "read" => await service.MarkRead(command.ConversationId, command.Data.Deserialize<AccountMessagesService.MessageReadInput>(Json)!, ct),
                        "typing" => await service.UpdateTyping(command.ConversationId, command.Data.Deserialize<AccountMessagesService.TypingInput>(Json)!, ct),
                        _ => UseCaseResult.Invalid(),
                    };
                    await queue.Writer.WriteAsync(new
                    {
                        type = "result",
                        id = requestId,
                        ok = result.Kind is ResultKind.Success or ResultKind.Completed,
                        data = result.Value
                    }, ct);
                }
                catch (Exception e) when (e is JsonException or ArgumentException)
                {
                    await queue.Writer.WriteAsync(new
                    {
                        type = "result",
                        id = requestId,
                        ok = false
                    }, ct);
                }
            }
        }
        catch (Exception e) when (e is OperationCanceledException or WebSocketException or ChannelClosedException) { }
        catch (Exception e) { logger.LogError(e, "Chat WebSocket failed for account {AccountId}", account); }
        finally
        {
            clients.TryRemove(id, out _);
            queue.Writer.TryComplete();
            lifetime.Cancel();
            try
            {
                await writer;
            }
            catch (Exception e) when (e is OperationCanceledException or WebSocketException) { }
            catch (Exception e) { logger.LogError(e, "Chat WebSocket writer failed for account {AccountId}", account); }
            socket.Abort();
        }
        async Task Write()
        {
            try
            {
                await foreach (var item in queue.Reader.ReadAllAsync(ct))
                {
                    await using var scope = context.RequestServices.CreateAsyncScope();
                    var valid = user.IsInRole("Employer")
                        ? await scope.ServiceProvider.GetRequiredService<EmployerTokens>().ValidateAsync(user, ct)
                        : await scope.ServiceProvider.GetRequiredService<CandidateTokens>().ValidateAsync(user, ct);
                    if (!valid)
                        return;
                    await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(item, Json), WebSocketMessageType.Text, true, ct);
                }
            }
            finally { lifetime.Cancel(); }
        }
    }
    private sealed record Command(string Id, string Type, Guid ConversationId, JsonElement Data);
}
