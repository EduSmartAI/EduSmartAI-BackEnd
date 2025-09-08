using BaseService.Common.ApiEntities;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NLog;

namespace BaseService.API.BaseControllers;

public class ApiControllerHelperNotAuth
{
    /// <summary>
    /// Template Method
    /// </summary>
    /// <param name="request"></param>
    /// <param name="logger"></param>
    /// <param name="returnValue"></param>
    /// <param name="modelState"></param>
    /// <param name="exec"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TEntityResponse"></typeparam>
    /// <returns></returns>
    public static async Task<TResponse> HandleRequest<TRequest, TResponse, TEntityResponse>(TRequest request, Logger logger, TResponse returnValue, ModelStateDictionary modelState, Func<Task<TResponse>> exec)
        where TResponse : AbstractApiResponse<TEntityResponse>
    {
        var loggingUtil = new LoggingUtil(logger, "Unknown");
        loggingUtil.StartLog(request);

        try
        {
            // Validate ModelState
            var detailErrors = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(modelState);
            if (detailErrors.Count > 0)
            {
                returnValue.Success = false;
                returnValue.SetMessage(MessageId.E10000);
                returnValue.DetailErrors = detailErrors;
                return returnValue;
            }

            return await exec();
        }
        catch (Exception e)
        {
            loggingUtil.ErrorLog(e.Message);
            return AbstractFunction<TResponse, TEntityResponse>.GetReturnValue(returnValue, loggingUtil, e);
        }
        finally
        {
            loggingUtil.EndLog(returnValue);
        }
    }
}