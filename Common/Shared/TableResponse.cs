namespace Shared;

public record TableResponse<T>(long Total, IEnumerable<T> Data);
