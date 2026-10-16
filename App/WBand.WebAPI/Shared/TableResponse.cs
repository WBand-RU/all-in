namespace WBand.WebAPI.Shared;

public record TableResponse<T>(long Total, IEnumerable<T> Data);
