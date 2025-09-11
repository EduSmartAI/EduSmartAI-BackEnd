using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using NLog;

namespace BaseService.Common.Utils;

public class LoggingUtil
{
    private readonly Logger _logger;

    private readonly string _userName;

    private readonly string _executeId;

    /// <summary>
    /// 
    /// </summary>
    private readonly JsonSerializerOptions? _jsonSerializerOptions = new JsonSerializerOptions()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="userName"></param>
    public LoggingUtil(Logger logger, string userName)
    {
        var folderPath = Path.GetFileName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory));
        logger.Factory.Configuration?.Variables.TryAdd("folderPath", folderPath);
        logger.Factory.ReconfigExistingLoggers();

        _logger = logger;
        _userName = userName;
        _executeId = DateTime.UtcNow.ToString("yyyyMMddHHmmss_") + Guid.NewGuid().ToString("N").Substring(0, 10);
    }

    /// <summary>
    /// Debug log output
    /// </summary>
    /// <param name="message"></param>
    public void DebugLog(string message)
    {
        _logger.Debug($"{_executeId} | {_logger.Name} | {_userName} | Debug Logs：{message}");
    }

    /// <summary>
    /// Information log output
    /// </summary>
    /// <param name="message"></param>
    public void InfoLog(string message)
    {
        _logger.Info($"{_executeId} | {_logger.Name} | {_userName} | Information Logs：{message}");
    }

    /// <summary>
    /// Input check error log output
    /// </summary>
    /// <param name="message"></param>
    public void ErrorLog(string message)
    {
        _logger.Error($"{_executeId} | {_logger.Name} | {_userName} | Error Logs：{message}");
    }

    /// <summary>
    /// Warning log output
    /// </summary>
    /// <param name="message"></param>
    public void WarningLog(string message)
    {
        _logger.Warn($"{_executeId} | {_logger.Name} | {_userName} | Warning Logs：{message}");
    }

    /// <summary>
    /// Fatal log output
    /// </summary>
    /// <param name="message"></param>
    public void FatalLog(string message)
    {
        _logger.Fatal($"{_executeId} | {_logger.Name} | {_userName} | Fatal Logs：{message}");
    }

    /// <summary>
    /// Log output for API start (not required for each API)
    /// </summary>
    /// <param name="argument"></param>
    public void StartLog(object? argument = null)
    {
        string message = argument != null
            ? JsonSerializer.Serialize(argument, _jsonSerializerOptions)
            : "No argument provided";

        _logger.Info($"{_executeId} | {_logger.Name} | {_userName} | Start Logs: {message}");
    }

    /// <summary>
    /// Log output for API start (not required for each API)
    /// </summary>
    /// <param name="response"></param>
    public void EndLog(object response)
    {
        var message = JsonSerializer.Serialize(response, _jsonSerializerOptions);
        var maxLength = 5000;
        if (message.Length > maxLength) message = message.Substring(0, maxLength) + "【Omitted】";

        _logger.Info($"{_executeId} | {_logger.Name} | {_userName} | End Logs：{message}");
    }
}