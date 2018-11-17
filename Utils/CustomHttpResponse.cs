using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Undone.Resource.Models;

namespace Undone.Resource.Utils
{
  public static class CustomHttpResponse
  {
    #region Public Static Methods
    public static ObjectResult Error(HttpStatusCode httpStatusCode, string code, string messageToDeveloper, string messageToUserTh, string messageToUserEn)
    {
      var error = new ErrorPayload
      {
        errorId = Guid.NewGuid().ToString(),
        code = code,
        messageToDeveloper = messageToDeveloper,
        messageToUser = new ErrorPayloadMessageToUserDetail
        {
          langTh = messageToUserTh,
          langEn = messageToUserEn
        },
        created = DateTimes.GetCurrentUtcDateTimeInThaiTimeZone(DateTimes.DateTimeFormat.YearMonthDayByDashTHourMinuteSecondByColonZ, DateTimes.LanguageCultureName.ENGLISH_UNITED_STATES, DateTimes.DateTimeUtcOffset.HHMM)
      };

      var objResult = new ObjectResult(new { error });
      objResult.StatusCode = (int)httpStatusCode;

      return objResult;
    }
    #endregion
  }
}