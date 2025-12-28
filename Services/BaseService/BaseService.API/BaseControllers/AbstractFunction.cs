using System.Text.RegularExpressions;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql;

namespace BaseService.API.BaseControllers;

public abstract class AbstractFunction<TResponse, TEntityResponse>
    where TResponse : AbstractApiResponse<TEntityResponse>
{
    /// <summary>
    /// Return value
    /// </summary>
    public static TResponse GetReturnValue(TResponse returnValue, LoggingUtil loggingUtil, Exception e)
    {
        switch (e)
        {
            case AggregateException:
                loggingUtil.FatalLog($"Report API connection error: {e.Message}");
                returnValue.SetMessage(MessageId.E99001);
                break;

            case InvalidOperationException:
                if (e.InnerException?.HResult == -2146233088)
                {
                    loggingUtil.ErrorLog($"Concurrency conflict: {e.Message}");
                    returnValue.SetMessage(MessageId.E99002);
                }
                else
                {
                    loggingUtil.FatalLog($"System error: {e.Message} {e.StackTrace} {e.InnerException}");
                    returnValue.SetMessage(MessageId.E99999);
                }

                break;

            case PostgresException ex:
                if (ex.SqlState == "57014")
                {
                    var stackTrace = ex.StackTrace ?? "No stack trace";
                    loggingUtil.ErrorLog($"PostgresSQL timeout error: {ex.Message} {stackTrace}");
                    returnValue.SetMessage(MessageId.E99003);
                }
                else if (ex.SqlState == "42P01")
                {
                    var stackTrace = ex.StackTrace ?? "No stack trace";
                    loggingUtil.ErrorLog($"Schema/view changed during execution: {ex.Message} {stackTrace}");
                    returnValue.SetMessage(MessageId.E99004);
                }
                else
                {
                    var stackTrace = ex.StackTrace ?? "No stack trace";
                    loggingUtil.ErrorLog($"PostgresSQL system error: {ex.Message} {stackTrace}");
                    returnValue.SetMessage(MessageId.E99005);
                }

                break;

            case not null:
                loggingUtil.ErrorLog($"Unhandled exception: {e.Message} {e.StackTrace} {e.InnerException}");
                returnValue.SetMessage(MessageId.E99999);
                break;
        }

        returnValue.Success = false;
        loggingUtil.EndLog(returnValue);
        return returnValue;
    }


    /// <summary>
    /// Error check
    /// </summary>
    /// <param name="modelState"></param>
    /// <returns></returns>
    public static List<DetailError> ErrorCheck(ModelStateDictionary modelState)
    {
        var detailErrorList = new List<DetailError>();

        // If there is no error, return
        if (modelState.IsValid)
            return detailErrorList;

        foreach (var entry in modelState)
        {
            var key = entry.Key;
            var modelStateEntity = entry.Value;

            if (modelStateEntity.ValidationState == ModelValidationState.Valid)
                continue;

            // Remove the prefix "Value." from the key
            var keyReplace = Regex.Replace(key, @"^Value\.", "");
            keyReplace = Regex.Replace(keyReplace, @"^Value\[\d+\]\.", "");

            // Get error message
            var errorMessage = string.Join("; ", modelStateEntity.Errors.Select(e => e.ErrorMessage));

            var detailError = new DetailError();
            Match matchesKey;

            // Extract information from the key in the structure: object[index].property
            if ((matchesKey = new Regex(@"^(.*?)\[(\d+)\]\.(.*?)$").Match(keyReplace)).Success)
            {
                // In the case of a list
                detailError.Field = matchesKey.Groups[1].Value;
            }
            else
            {
                // In the case of a single item
                detailError.Field = keyReplace.Split('.').LastOrDefault();
            }

            // Convert the field name to lowercase
            detailError.Field = StringUtil.ToLowerCase(detailError.Field);

            // Set the error message
            detailError.ErrorMessage = errorMessage;
            detailError.MessageId = MessageId.E10000;
            detailErrorList.Add(detailError);
        }

        return detailErrorList;
    }
}