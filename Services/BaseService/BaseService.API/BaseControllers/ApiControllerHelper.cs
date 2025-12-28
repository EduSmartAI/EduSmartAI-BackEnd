using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NLog;

namespace BaseService.API.BaseControllers;

public class ApiControllerHelper
{
    /// <summary>
    /// Template Method
    /// </summary>
    /// <param name="request"></param>
    /// <param name="logger"></param>
    /// <param name="returnValue"></param>
    /// <param name="modelState"></param>
    /// <param name="exec"></param>
    /// <param name="identityService"></param>
    /// <param name="identityEntity"></param>
    /// <param name="httpContextAccessor"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TEntityResponse"></typeparam>
    /// <returns></returns>
    public static async Task<TResponse> HandleRequest<TRequest, TResponse, TEntityResponse>
    (TRequest request,
        Logger logger,
        ModelStateDictionary modelState,
        Func<Task<TResponse>> exec,
        IIdentityService identityService,
        IdentityEntity identityEntity,
        IHttpContextAccessor httpContextAccessor,
        TResponse returnValue
    )
        where TResponse : AbstractApiResponse<TEntityResponse>
    {
        var user = httpContextAccessor.HttpContext?.User;
        // Get identity information 
        identityEntity = identityService.GetIdentity(user);

        var loggingUtil = new LoggingUtil(logger, identityEntity?.Email!);
        loggingUtil.StartLog(request);

        // Check authentication information
        if (identityEntity == null)
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
            // Validate ModelState
            var detailErrors = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(modelState);
            if (detailErrors.Count > 0)
            {
                returnValue.Success = false;
                returnValue.SetMessage(MessageId.E10000);
                returnValue.DetailErrors = detailErrors;
                return returnValue;
            }

            returnValue = await exec();
            return returnValue;
            ;
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

    /// <summary>
    /// Template Method not authentication
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
    public static async Task<TResponse> HandleRequest<TRequest, TResponse, TEntityResponse>
    (TRequest request,
        Logger logger,
        ModelStateDictionary modelState,
        Func<Task<TResponse>> exec,
        TResponse returnValue
    )
        where TResponse : AbstractApiResponse<TEntityResponse>
    {
        var loggingUtil = new LoggingUtil(logger, "User do not authenticated");
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

            returnValue = await exec();
            return returnValue;
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