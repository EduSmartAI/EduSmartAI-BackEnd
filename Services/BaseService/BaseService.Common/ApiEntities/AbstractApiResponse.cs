using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;

namespace BaseService.Common.ApiEntities;

/// <summary>
/// API Response (Shared.Common) Abstract Class
/// </summary>
public abstract record AbstractApiResponse<T>
{
    /// <summary>
    /// Success
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message ID (basically use SetMessage, not directly assign)
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Error message (basically use SetMessage, not directly assign)
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Error list
    /// </summary>
    public List<DetailError> DetailErrors { get; set; }

    /// <summary>
    /// Response to be implemented on each screen
    /// </summary>
    public abstract T Response { get; set; }
    
    /// <summary>
    /// Set error message (basically use this)
    /// </summary>
    public void SetMessage(string messageId, params string[] args)
    {
        this.MessageId = messageId;
        this.Message = Messages.GetMessage(messageId, args);
    }
}

/// <summary>
/// Error details
/// </summary>
public class DetailError
{
    private string? _fieldValue;
    
    /// <summary>
    /// Field names with errors
    /// </summary>
    public string? Field { get => _fieldValue;
        set => _fieldValue = StringUtil.ToLowerCase(value);
    }

    /// <summary>
    /// Message ID
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Error messages (basically use SetMessage, not directly assign)
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// A set of error messages (use these as a general rule)
    /// </summary>
    public void SetMessage(string messageId, params string[] args)
    {
        this.MessageId = messageId;
        this.ErrorMessage = Messages.GetMessage(messageId, args);
    }
}