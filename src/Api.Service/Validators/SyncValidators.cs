using Api.Contract;
using Api.Service.Helpers;
using Common.Dto;
using Garmin.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Api.Service.Validators;

public static class SyncValidators
{
	public static (bool, ErrorResponse?) IsValid(this SyncPostRequest request, Settings settings, GarminApiAuthentication garminAuth)
	{
		if (request.IsNull("Request", out var result))
			return (false, result);

		if (settings.Garmin.Upload && settings.Garmin.TwoStepVerificationEnabled)
		{
			if (garminAuth is null || !garminAuth.IsValid())
			{
				result = new ErrorResponse("Must initialize Garmin two factor auth token before sync can be preformed.", ErrorCode.NeedToInitGarminMFAAuth);
				return (false, result);
			}
		}

		if (request.WorkoutIds.DoesNotHaveAny(nameof(request.WorkoutIds), out result!))
			return (false, result);

		return (true, result);
	}

	public static (bool, ActionResult?) IsValidHttp(this SyncPostRequest request, Settings settings, GarminApiAuthentication garminAuth)
	{
		ActionResult result = new OkResult();

		if (request.IsNull("Request", out var error))
			return (false, new BadRequestObjectResult(error));

		if (settings.Garmin.Upload && settings.Garmin.TwoStepVerificationEnabled)
		{
			if (garminAuth is null || !garminAuth.IsValid())
			{
				result = new UnauthorizedObjectResult(new ErrorResponse("Must initialize Garmin two factor auth token before sync can be preformed.", ErrorCode.NeedToInitGarminMFAAuth));
				return (false, result);
			}
		}

		if (request.WorkoutIds.DoesNotHaveAny(nameof(request.WorkoutIds), out error!))
			return (false, new BadRequestObjectResult(error));

		return (true, result);
	}

	public static (bool, ActionResult?) IsValidHttp(this SyncRecentPostRequest request, Settings settings, GarminApiAuthentication garminAuth)
	{
		ActionResult result = new OkResult();

		if (request.IsNull("Request", out var error))
			return (false, new BadRequestObjectResult(error));

		if (settings.Garmin.Upload && settings.Garmin.TwoStepVerificationEnabled)
		{
			if (garminAuth is null || !garminAuth.IsValid())
			{
				result = new UnauthorizedObjectResult(new ErrorResponse("Must initialize Garmin two factor auth token before sync can be preformed.", ErrorCode.NeedToInitGarminMFAAuth));
				return (false, result);
			}
		}

		if (request.NumberToSync.CheckIsLessThanOrEqualTo(0 ,nameof(request.NumberToSync), out error!))
			return (false, new BadRequestObjectResult(error));

		return (true, result);
	}
}
