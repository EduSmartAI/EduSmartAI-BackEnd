using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace BaseService.API.AbstractControllers;

public abstract class AbstractApiAsyncController<TRequest, TResponse, TEntityResponse>: ControllerBase
    where TResponse : AbstractApiResponse<TEntityResponse>
    where TRequest : class
{
    protected AbstractApiAsyncController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

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
    /// Authentication API client
    /// </summary>
    protected IIdentityService _identityService;

    /// <summary>
    /// Identities information
    /// </summary>
    protected IdentityEntity _identityEntity;

    /// <summary>
    /// TemplateMethod
    /// </summary>
    /// <param name="request"></param>
    /// <param name="logger"></param>
    /// <param name="returnValue"></param>
    /// <returns></returns>
    protected async Task<TResponse> ProcessRequest(TRequest request, Logger logger, TResponse returnValue)
    {
        // Get identity information 
        _identityEntity = _identityService.GetIdentity(User);

        var loggingUtil = new LoggingUtil(logger, _identityEntity?.Email!);
        loggingUtil.StartLog(request);

        // Check authentication information
        if (_identityEntity == null)
        {
            // Authentication error
            loggingUtil.FatalLog($"Authenticated, but information is missing.");
            returnValue.Success = false;
            returnValue.SetMessage(MessageId.E11006);
            loggingUtil.EndLog(returnValue);
            return returnValue;
        }

        try
        {
            // Error check
            var detailErrors = new List<DetailError>();
            returnValue = ErrorCheck(detailErrors, returnValue);

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
    private TResponse ErrorCheck(List<DetailError> detailErrorList, TResponse returnValue)
    {
        detailErrorList = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(this.ModelState);
        if (detailErrorList.Count > 0)
        {
            returnValue.Success = false;
            returnValue.SetMessage(MessageId.E10000);
            returnValue.DetailErrors = detailErrorList;
        }
        else
        {
            returnValue.Success = true;
        }

        return returnValue;
    }
}