using GamingEventStreaming.Contracts.Domain;
using GamingEventStreaming.Contracts.Grpc;
using GamingEventStreaming.Contracts.Validation;
using Grpc.Core;

namespace PersonalisationEngine.Api.Grpc;

public sealed class EventIngestService : EventIngest.EventIngestBase
{
    private readonly ILogger<EventIngestService> _logger;

    public EventIngestService(ILogger<EventIngestService> logger) => _logger = logger;

    public override Task<IngestAck> SendEvent(GamingEventMessage request, ServerCallContext context)
    {
        var (valid, reason) = MessageValidator.Validate(request);
        if (!valid)
            return Task.FromResult(new IngestAck { EventId = request.EventId, Accepted = false, Message = reason });

        if (!Enum.TryParse<GamingEventType>(request.EventType, out var eventType)
            || eventType != GamingEventType.Login)
            return Task.FromResult(new IngestAck
            {
                EventId = request.EventId,
                Accepted = false,
                Message = $"Discarded: event type '{request.EventType}' not handled"
            });

        _logger.LogInformation(
            "Login event received — PlayerId={PlayerId} EventId={EventId} OccurredAt={OccurredAt}",
            request.PlayerId, request.EventId, request.OccurredAt.ToDateTimeOffset());

        return Task.FromResult(new IngestAck { EventId = request.EventId, Accepted = true, Message = "Login event logged" });
    }

    public override async Task<IngestSummary> SendEvents(
        IAsyncStreamReader<GamingEventMessage> requestStream,
        ServerCallContext context)
    {
        int received = 0, accepted = 0, rejected = 0;
        await foreach (var msg in requestStream.ReadAllAsync())
        {
            received++;
            var ack = await SendEvent(msg, context);
            if (ack.Accepted) accepted++; else rejected++;
        }
        return new IngestSummary { Received = received, Accepted = accepted, Rejected = rejected };
    }
}
