using BaseService.Common.ApiEntities;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace AuthService.API.AbstractControllers;

public abstract class AbstractApiAsyncControllerNotToken
    <TRequest, TResponse, TEntityResponse>: ControllerBase
    where TRequest : class
    where TResponse : AbstractApiResponse<TEntityResponse>
{
    /// <summary>
    /// API entry point
    /// </summary>
    public abstract Task<TResponse> ProcessRequest(TRequest request);
    
    /// <summary>
    /// Main processing
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected abstract Task<TResponse> Exec(TRequest request);

    /// <summary>
    /// TemplateMethod
    /// </summary>
    /// <param name="request"></param>
    /// <param name="logger"></param>
    /// <param name="returnValue"></param>
    /// <returns></returns>
    protected async Task<TResponse> ProcessRequest(TRequest request, Logger logger, TResponse returnValue)
    {
        var loggingUtil = new LoggingUtil(logger, "System");
        loggingUtil.StartLog(request);
        try
        {
            // Error check
            var detailErrorList = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(this.ModelState);
            returnValue = ErrorCheck(detailErrorList, returnValue);

            // If there is no error, execute the main process
            if (returnValue.Success)
                returnValue = await Exec(request);
        }
        catch (Exception e)
        {
            loggingUtil.ErrorLog(e.Message);
            return AbstractFunction<TResponse, TEntityResponse>.GetReturnValue(returnValue, loggingUtil, e);
        }

        // Processing end log
        loggingUtil.EndLog(returnValue);
        return returnValue;
    }
    
    /// <summary>
    /// Error check
    /// </summary>
    private TResponse ErrorCheck(List<DetailError> detailErrors, TResponse returnValue)
    {
        detailErrors = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(this.ModelState);
        if (detailErrors.Count > 0)
        {
            returnValue.Success = false;
            returnValue.SetMessage(MessageId.E10000);
            returnValue.DetailErrors = detailErrors;
        }
        else
        {
            returnValue.Success = true;
        }

        return returnValue;
    }
}
