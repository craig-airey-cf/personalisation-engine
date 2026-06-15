using GamingEventStreaming.Contracts.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PersonalisationEngine.Api.Grpc;

namespace PersonalisationEngine.Tests.Grpc;

public class EventIngestServiceTests
{
    private readonly EventIngestService _sut = new(NullLogger<EventIngestService>.Instance);
    private static readonly ServerCallContext Context = new Mock<ServerCallContext>().Object;

    private static GamingEventMessage ValidLogin() => new()
    {
        EventId = "evt-001",
        PlayerId = "P001",
        EventType = "Login",
        AmountMinor = 0,
        Currency = "GBP",
        OccurredAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
    };

    // --- Validation failures ---

    [Fact]
    public async Task SendEvent_MissingEventId_ReturnsRejected()
    {
        var msg = ValidLogin();
        msg.EventId = "";

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Equal("eventId is required", ack.Message);
    }

    [Fact]
    public async Task SendEvent_MissingPlayerId_ReturnsRejected()
    {
        var msg = ValidLogin();
        msg.PlayerId = "";

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Equal("playerId is required", ack.Message);
    }

    [Fact]
    public async Task SendEvent_MissingEventType_ReturnsRejected()
    {
        var msg = ValidLogin();
        msg.EventType = "";

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Equal("eventType is required", ack.Message);
    }

    [Fact]
    public async Task SendEvent_NegativeAmount_ReturnsRejected()
    {
        var msg = ValidLogin();
        msg.AmountMinor = -1;

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Equal("amount must be non-negative", ack.Message);
    }

    [Fact]
    public async Task SendEvent_UnsupportedCurrency_ReturnsRejected()
    {
        var msg = ValidLogin();
        msg.Currency = "USD";

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Contains("currency must be one of:", ack.Message);
    }

    // --- Event type handling ---

    [Fact]
    public async Task SendEvent_KnownNonLoginEventType_ReturnsDiscarded()
    {
        var msg = ValidLogin();
        msg.EventType = "BetPlaced";

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Contains("not handled", ack.Message);
    }

    [Fact]
    public async Task SendEvent_UnknownEventType_ReturnsDiscarded()
    {
        var msg = ValidLogin();
        msg.EventType = "SomethingUnknown";

        var ack = await _sut.SendEvent(msg, Context);

        Assert.False(ack.Accepted);
        Assert.Contains("not handled", ack.Message);
    }

    [Fact]
    public async Task SendEvent_ValidLogin_ReturnsAcceptedWithEventId()
    {
        var ack = await _sut.SendEvent(ValidLogin(), Context);

        Assert.True(ack.Accepted);
        Assert.Equal("evt-001", ack.EventId);
    }

    // --- Streaming ---

    [Fact]
    public async Task SendEvents_MixedStream_ReturnsCorrectSummary()
    {
        var notHandled = ValidLogin();
        notHandled.EventId = "evt-002";
        notHandled.EventType = "Deposit";

        var invalid = ValidLogin();
        invalid.EventId = "evt-003";
        invalid.PlayerId = "";

        var stream = new FakeStreamReader<GamingEventMessage>([ValidLogin(), notHandled, invalid]);

        var summary = await _sut.SendEvents(stream, Context);

        Assert.Equal(3, summary.Received);
        Assert.Equal(1, summary.Accepted);
        Assert.Equal(2, summary.Rejected);
    }

    private sealed class FakeStreamReader<T>(IEnumerable<T> items) : IAsyncStreamReader<T>
    {
        private readonly IEnumerator<T> _enumerator = items.GetEnumerator();
        public T Current => _enumerator.Current;
        public Task<bool> MoveNext(CancellationToken cancellationToken) =>
            Task.FromResult(_enumerator.MoveNext());
    }
}