namespace Emcore.BuildingBlocks.Messaging;

public class IntegrationEvent { }
public class EventEnvelope<T> { }
public class MessageContext { }
public interface IEventPublisher { }
public interface IMessageConsumer<T> { }
public interface IOutboxWriter { }
public interface IInboxStore { }
public enum OutboxMessageState { Pending, Processed, Failed }
public enum InboxMessageState { Pending, Processed, Failed }
public class MessagingOptions { }
public class MessagingDependencyState { }
public class NoOpEventPublisher : IEventPublisher { }
public static class MessagingRegistrationExtensions { }
