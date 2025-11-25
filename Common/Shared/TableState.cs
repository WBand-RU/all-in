namespace Shared;

/// <summary>
/// Состояние таблицы.
/// </summary>
/// <param name="Take">Количество элементов на странице.</param>
/// <param name="Skip">Смещение.</param>
public record TableState(int? Take, int? Skip);
