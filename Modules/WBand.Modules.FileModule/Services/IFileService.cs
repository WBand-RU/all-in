namespace WBand.Modules.FileModule.Services;

/// <summary>
/// Сервис для работы с файлами.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Получить ссылку на скачивание или просмотр файла.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<string> GetSignedDownloadUrl(string fileName);

    /// <summary>
    /// Получить ссылку на загрузку файла на сервер.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<string> GetSignedUploadUrl(string fileName);
}
