using System.Net.WebSockets;
using System.Net.Http.Json;
using System.Text.Json;

public static class WebSocketChecks
{
    public static async Task Run(HttpClient candidate, HttpClient employer, Guid conversation, Action<bool, string> check)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var ct = deadline.Token;
        async Task<Uri> Ticket(HttpClient client, string role)
        {
            var response = await client.PostAsync($"/api/{role}/messages/socket-ticket", null, ct);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return new UriBuilder(client.BaseAddress!) { Scheme = "ws", Path = $"/api/{role}/messages/socket", Query = "ticket=" + data.GetProperty("ticket").GetString() }.Uri;
        }
        async Task<JsonElement> Receive(ClientWebSocket socket, string type)
        {
            while (true)
            {
                var bytes = new byte[32768];
                var length = 0;
                WebSocketReceiveResult result;
                do { result = await socket.ReceiveAsync(new ArraySegment<byte>(bytes, length, bytes.Length - length), ct); length += result.Count; }
                while (!result.EndOfMessage);
                var value = JsonDocument.Parse(bytes.AsMemory(0, length)).RootElement.Clone();
                if (value.GetProperty("type").GetString() == type) return value;
            }
        }
        async Task Send(ClientWebSocket socket, string type, Guid conversationId, object data) =>
            await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { id = Guid.NewGuid().ToString(), type, conversationId, data }), WebSocketMessageType.Text, true, ct);
        using var first = new ClientWebSocket();
        using var second = new ClientWebSocket();
        var ticket = await Ticket(candidate, "candidate");
        await first.ConnectAsync(ticket, ct);
        await second.ConnectAsync(await Ticket(employer, "employer"), ct);
        check(first.State == WebSocketState.Open && second.State == WebSocketState.Open, "candidate and employer connect over real WebSockets");
        using var replay = new ClientWebSocket();
        try { await replay.ConnectAsync(ticket, ct); check(false, "ticket reuse"); }
        catch (WebSocketException) { check(true, "WebSocket connection tickets are single-use"); }
        var messageId = Guid.NewGuid();
        await Send(first, "send", conversation, new { text = "WebSocket message", clientMessageId = messageId });
        var pushed = await Receive(second, "message");
        check(pushed.GetProperty("data").GetProperty("text").GetString() == "WebSocket message", "peer receives persisted message without polling");
        check((await Receive(first, "result")).GetProperty("ok").GetBoolean(), "sender receives message acknowledgement");
        await Send(first, "send", conversation, new { text = "WebSocket message", clientMessageId = messageId });
        check((await Receive(first, "result")).GetProperty("ok").GetBoolean(), "WebSocket send retries remain idempotent");
        await Send(first, "send", Guid.NewGuid(), new { text = "forbidden", clientMessageId = Guid.NewGuid() });
        check(!(await Receive(first, "result")).GetProperty("ok").GetBoolean(), "WebSocket commands cannot access another conversation");
        await Send(first, "typing", conversation, new { isTyping = true });
        check((await Receive(second, "typing")).GetProperty("data").GetProperty("isTyping").GetBoolean(), "typing is pushed through WebSocket");
        await Send(second, "read", conversation, new { messageIds = new[] { pushed.GetProperty("data").GetProperty("id").GetInt64() } });
        check((await Receive(first, "read")).GetProperty("conversationId").GetGuid() == conversation, "read state is pushed through WebSocket");
        await Receive(second, "result");
        await Send(second, "send", conversation, new { text = "Employer reply", clientMessageId = Guid.NewGuid() });
        check((await Receive(first, "message")).GetProperty("data").GetProperty("text").GetString() == "Employer reply", "employer can send a real-time reply to the candidate");
        check((await Receive(second, "result")).GetProperty("ok").GetBoolean(), "employer reply is acknowledged");
        first.Abort(); second.Abort();
    }
}
