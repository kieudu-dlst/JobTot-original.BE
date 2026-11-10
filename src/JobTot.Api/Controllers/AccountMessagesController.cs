using System.Security.Claims;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/candidate/messages"), Route("api/employer/messages")]
[Authorize(
    AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme
)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountMessagesController(RecruitmentDbContext db, IMemoryCache typing)
    : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<ChatConversation> Mine =>
        db.ChatConversations.Where(c =>
            c.FirstAccountId == AccountId || c.SecondAccountId == AccountId
        );

    private static bool Duplicate(DbUpdateException ex)
    {
        try
        {
            return ex.InnerException is SqlException { Number: 2601 or 2627 };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.Duplicate"
            );
            throw;
        }
    }

    [HttpGet("people")]
    public async Task<IActionResult> People([FromQuery] string? q, CancellationToken ct)
    {
        try
        {
            q = q?.Trim();
            if (q is null || q.Length < 2)
                return Ok(Array.Empty<object>());
            if (q.Length > 100)
                return BadRequest();
            var people = await db
                .AccountRecords.AsNoTracking()
                .Where(a =>
                    a.Id != AccountId
                    && a.Status == EntityStatus.Active
                    && a.AccountStatus == "Active"
                    && (a.AccountType == "Candidate" || a.AccountType == "Employer")
                    && a.FullName.Contains(q)
                )
                .OrderBy(a => a.FullName)
                .ThenBy(a => a.Id)
                .Take(20)
                .Select(a => new ChatPerson(a.Id, a.FullName, a.AccountType, null))
                .ToListAsync(ct);
            var avatars = await ChatAvatars.ReadAsync(db, people.Select(p => p.Id), ct);
            return Ok(people.Select(p => p with { AvatarUrl = avatars.GetValueOrDefault(p.Id) }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.People"
            );
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            var conversations = await ConversationQuery(
                    Mine.AsNoTracking()
                        .OrderByDescending(c =>
                            c.Messages.Select(m => (DateTimeOffset?)m.SentAt).Max() ?? c.CreatedAt
                        )
                        .ThenBy(c => c.Id)
                        .Take(100)
                )
                .ToListAsync(ct);
            var avatars = await ChatAvatars.ReadAsync(db, conversations.Select(c => c.Peer.Id), ct);
            return Ok(
                conversations.Select(c =>
                    c with
                    {
                        Peer = c.Peer with { AvatarUrl = avatars.GetValueOrDefault(c.Peer.Id) },
                    }
                )
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.List"
            );
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> Start(StartInput input, CancellationToken ct)
    {
        try
        {
            if (input.CompanyId is { } companyId)
            {
                var recruiterId = await db
                    .RecruiterProfileRecords.Where(p =>
                        p.CompanyId == companyId
                        && p.Status == EntityStatus.Active
                        && p.Company.Status == EntityStatus.Active
                        && p.Account.Status == EntityStatus.Active
                        && p.Account.AccountStatus == "Active"
                        && p.Account.AccountType == "Employer"
                    )
                    .OrderByDescending(p => p.IsCompanyAdmin)
                    .ThenBy(p => p.JoinedAt)
                    .ThenBy(p => p.Id)
                    .Select(p => p.AccountId)
                    .FirstOrDefaultAsync(ct);
                if (recruiterId == Guid.Empty)
                    return NotFound(new { detail = "Doanh nghiệp chưa có HR để trò chuyện." });
                input = input with { AccountId = recruiterId };
            }
            if (!string.IsNullOrWhiteSpace(input.RecruiterEmail))
            {
                var email = input.RecruiterEmail.Trim().ToLowerInvariant();
                var recruiterId = await db
                    .AccountRecords.Where(a =>
                        a.Email == email
                        && a.AccountType == "Employer"
                        && a.Status == EntityStatus.Active
                        && a.AccountStatus == "Active"
                    )
                    .Select(a => a.Id)
                    .SingleOrDefaultAsync(ct);
                if (recruiterId == Guid.Empty)
                    return NotFound(new { detail = "Không tìm thấy tài khoản HR để trò chuyện." });
                input = input with { AccountId = recruiterId };
            }
            if (input.AccountId == AccountId || input.AccountId == Guid.Empty)
                return BadRequest();
            if (
                !await db.AccountRecords.AnyAsync(
                    a =>
                        a.Id == input.AccountId
                        && a.Status == EntityStatus.Active
                        && a.AccountStatus == "Active"
                        && (a.AccountType == "Candidate" || a.AccountType == "Employer"),
                    ct
                )
            )
                return NotFound();
            var pair = string.Join(
                ':',
                new[] { AccountId.ToString(), input.AccountId.ToString() }.Order(
                    StringComparer.Ordinal
                )
            );
            var existing = await db.ChatConversations.SingleOrDefaultAsync(
                c => c.PairKey == pair,
                ct
            );
            if (existing is not null)
                return await ConversationResult(existing.Id, ct);
            var conversation = new ChatConversation
            {
                PairKey = pair,
                FirstAccountId = AccountId,
                SecondAccountId = input.AccountId,
            };
            db.Add(conversation);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (Duplicate(ex))
            {
                db.Entry(conversation).State = EntityState.Detached;
                existing = await db.ChatConversations.SingleAsync(c => c.PairKey == pair, ct);
                return await ConversationResult(existing.Id, ct);
            }

            return await ConversationResult(conversation.Id, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.Start"
            );
            throw;
        }
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, MessageReadInput input, CancellationToken ct)
    {
        if (
            input.MessageIds is null
            || input.MessageIds.Length > 100
            || input.MessageIds.Any(x => x <= 0)
        )
            return BadRequest();
        if (!await Mine.AnyAsync(c => c.Id == id, ct))
            return NotFound();
        var ids = await db
            .ChatMessages.Where(m =>
                m.ConversationId == id
                && m.SenderAccountId != AccountId
                && input.MessageIds.Contains(m.Id)
            )
            .Select(m => m.Id)
            .ToListAsync(ct);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var existing = await db
                .AccountMessageReads.Where(r =>
                    r.AccountId == AccountId && ids.Contains(r.MessageId)
                )
                .Select(r => r.MessageId)
                .ToListAsync(ct);
            db.AccountMessageReads.AddRange(
                ids.Except(existing)
                    .Select(messageId => new AccountMessageRead
                    {
                        AccountId = AccountId,
                        MessageId = messageId,
                    })
            );
            try
            {
                await db.SaveChangesAsync(ct);
                return NoContent();
            }
            catch (DbUpdateException ex) when (attempt < 2 && Duplicate(ex))
            {
                db.ChangeTracker.Clear();
            }
        }
        return Conflict();
    }

    public sealed record MessageReadInput(long[] MessageIds);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Messages(
        Guid id,
        [FromQuery] long? before,
        [FromQuery] long? after,
        CancellationToken ct
    )
    {
        try
        {
            if (before is <= 0 || after is < 0 || (before.HasValue && after.HasValue))
                return BadRequest();
            if (!await Mine.AnyAsync(c => c.Id == id, ct))
                return NotFound();
            var query = db.ChatMessages.AsNoTracking().Where(m => m.ConversationId == id);
            if (before.HasValue)
                query = query.Where(m => m.Id < before.Value);
            if (after.HasValue)
                query = query.Where(m => m.Id > after.Value);
            var ordered = after.HasValue
                ? query.OrderBy(m => m.Id)
                : query.OrderByDescending(m => m.Id);
            var rows = await ordered
                .Take(51)
                .Select(m => new
                {
                    m.Id,
                    m.SenderAccountId,
                    m.ClientMessageId,
                    m.Text,
                    m.SentAt,
                })
                .ToListAsync(ct);
            return Ok(
                new
                {
                    items = after.HasValue ? rows.Take(50) : rows.Take(50).Reverse(),
                    hasMore = !after.HasValue && rows.Count > 50,
                }
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.Messages"
            );
            throw;
        }
    }

    [HttpGet("{id:guid}/typing")]
    public async Task<IActionResult> Typing(Guid id, CancellationToken ct)
    {
        // A poll may be aborted during navigation before the EF query starts.
        if (HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            var peerId = await Mine.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c =>
                    (Guid?)(c.FirstAccountId == AccountId ? c.SecondAccountId : c.FirstAccountId)
                )
                .SingleOrDefaultAsync(ct);
            if (peerId is null)
                return NotFound();
            var isTyping =
                typing.TryGetValue<DateTimeOffset>((id, peerId.Value), out var expiresAt)
                && expiresAt > DateTimeOffset.UtcNow;
            return Ok(new { isTyping, expiresAt = isTyping ? (DateTimeOffset?)expiresAt : null });
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted))
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [HttpPost("{id:guid}/typing")]
    public async Task<IActionResult> UpdateTyping(Guid id, TypingInput input, CancellationToken ct)
    {
        // A poll may be aborted during navigation before the EF query starts.
        if (HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            if (!await Mine.AnyAsync(c => c.Id == id, ct))
                return NotFound();
            if (input.IsTyping)
            {
                var expiresAt = DateTimeOffset.UtcNow.AddSeconds(6);
                typing.Set((id, AccountId), expiresAt, expiresAt);
            }
            else
                typing.Remove((id, AccountId));
            return NoContent();
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted))
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Send(Guid id, SendInput input, CancellationToken ct)
    {
        try
        {
            var text = input.Text?.Trim();
            if (
                string.IsNullOrEmpty(text)
                || text.Length > 4000
                || input.ClientMessageId == Guid.Empty
            )
                return BadRequest();
            if (
                !await Mine.AnyAsync(
                    c =>
                        c.Id == id
                        && c.FirstAccount.Status == EntityStatus.Active
                        && c.SecondAccount.Status == EntityStatus.Active
                        && c.FirstAccount.AccountStatus == "Active"
                        && c.SecondAccount.AccountStatus == "Active",
                    ct
                )
            )
                return NotFound();
            var previous = await db
                .ChatMessages.AsNoTracking()
                .SingleOrDefaultAsync(
                    m =>
                        m.SenderAccountId == AccountId
                        && m.ClientMessageId == input.ClientMessageId,
                    ct
                );
            if (previous is not null)
                return previous.ConversationId == id && previous.Text == text
                    ? Ok(previousDto(previous))
                    : Conflict();
            var message = new ChatMessage
            {
                ConversationId = id,
                SenderAccountId = AccountId,
                ClientMessageId = input.ClientMessageId,
                Text = text,
            };
            db.Add(message);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (Duplicate(ex))
            {
                db.Entry(message).State = EntityState.Detached;
                previous = await db
                    .ChatMessages.AsNoTracking()
                    .SingleAsync(
                        m =>
                            m.SenderAccountId == AccountId
                            && m.ClientMessageId == input.ClientMessageId,
                        ct
                    );
                return previous.ConversationId == id && previous.Text == text
                    ? Ok(previousDto(previous))
                    : Conflict();
            }

            typing.Remove((id, AccountId));
            return Ok(previousDto(message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.Send"
            );
            throw;
        }
    }

    private static object previousDto(ChatMessage m)
    {
        try
        {
            return new
            {
                m.Id,
                m.SenderAccountId,
                m.ClientMessageId,
                m.Text,
                m.SentAt,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AccountMessagesController.previousDto"
            );
            throw;
        }
    }

    private async Task<IActionResult> ConversationResult(Guid id, CancellationToken ct)
    {
        var conversation = await ConversationQuery(Mine.AsNoTracking().Where(c => c.Id == id))
            .SingleAsync(ct);
        var avatars = await ChatAvatars.ReadAsync(db, [conversation.Peer.Id], ct);
        return Ok(
            conversation with
            {
                Peer = conversation.Peer with
                {
                    AvatarUrl = avatars.GetValueOrDefault(conversation.Peer.Id),
                },
            }
        );
    }

    private IQueryable<ChatConversationDto> ConversationQuery(IQueryable<ChatConversation> query) =>
        query.Select(c => new ChatConversationDto(
            c.Id,
            new ChatPerson(
                c.FirstAccountId == AccountId ? c.SecondAccountId : c.FirstAccountId,
                c.FirstAccountId == AccountId ? c.SecondAccount.FullName : c.FirstAccount.FullName,
                c.FirstAccountId == AccountId
                    ? c.SecondAccount.AccountType
                    : c.FirstAccount.AccountType,
                null
            ),
            c.Messages.OrderByDescending(m => m.Id).Select(m => m.Text).FirstOrDefault()
        ));

    public sealed record ChatPerson(Guid Id, string Name, string? Role, string? AvatarUrl);

    public sealed record ChatConversationDto(Guid Id, ChatPerson Peer, string? LastText);

    public sealed record StartInput(
        Guid AccountId,
        string? RecruiterEmail = null,
        Guid? CompanyId = null
    );

    public sealed record TypingInput(bool IsTyping);

    public sealed record SendInput(string? Text, Guid ClientMessageId);
}
