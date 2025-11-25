namespace ChatService.Endpoints.Messages.GetMessages;

public sealed record GetMessagesRequest(string BandId, int Page = 1, int PageSize = 50);
