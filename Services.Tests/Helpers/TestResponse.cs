using MassTransit;
using Moq;

namespace Services.Tests.Helpers;

/// <summary>
/// Simple test implementation of MassTransit Response&lt;T&gt; for unit testing
/// Only implements what's needed for tests
/// </summary>
public class TestResponse<T> : Response<T>
    where T : class
{
    private readonly T _message;

    public TestResponse(T message)
    {
        _message = message;
    }

    public T Message => _message;
    
    object Response.Message { get { return _message; } }

    // Implement other required properties with default values
    public Guid? RequestId => null;
    public Guid? MessageId => null;
    public Guid? CorrelationId => null;
    public Guid? ConversationId => null;
    public Guid? InitiatorId => null;
    public DateTime? ExpirationTime => null;
    public Uri? SourceAddress => null;
    public Uri? DestinationAddress => null;
    public Uri? ResponseAddress => null;
    public Uri? FaultAddress => null;
    public DateTime? SentTime => null;
    public Headers Headers => Mock.Of<Headers>();
    public HostInfo Host => null!;
}


