using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Undone.Resource.Models;
using Undone.Resource.Services;
using Undone.Resource.Utils;

namespace Undone.Resource.Controllers
{
  [Route("api/[controller]")]
  public class TeamsController : Controller
  {
    private IConfiguration _config;
    private Firebase _fireObj;
    private const int DEFAULT_LIMIT = 10;

    public TeamsController(IConfiguration config)
    {
      _config = config;
      _fireObj = new Firebase(_config);
    }

    #region PUBLIC METHODS
    // GET api/teams
    // GET api/teams?offset=0&limit=5
    public async Task<IActionResult> Get([FromQuery] int? offset, int? limit)
    {
      IActionResult response = Unauthorized();

      if (CheckIsValidJwt())
      {
        // Data Payload
        var teams = new List<TeamsPayload>();

        var resp = await _fireObj.GetTeams();
        var jsonString = resp.Content.ReadAsStringAsync().Result;

        if (resp.StatusCode == HttpStatusCode.OK && (jsonString != "null" && jsonString != null))
        {
          var jObj = JObject.Parse(jsonString);

          foreach (var item in jObj)
          {
            var team = item.Value.ToObject<TeamsPayload>();

            var listTypes = new List<string>();
            listTypes.Add("application/json");

            team.links = new TeamsPayloadLinksDetail
            {
              rel = "Team information of " + team.Name,
              href = Request.Scheme + "://" + Request.Host + Request.Path.Value + "/" + team.Id,
              action = "GET",
              types = listTypes
            };
            teams.Add(team);
          }

          // Pagination Payload
          var pTotal = teams.Count;

          var pOffset = 0;
          var pLimit = 0;
          var pNext = 0;
          var pPrev = 0;
          var pFirst = 0;
          var pLast = 0;

          if ((offset != null && limit != null)) // With Pagination QueryString
          {
            pOffset = offset.Value;

            if (offset >= 0 && limit >= 1)
            {
              teams = teams.OrderBy(u => u.Id).Skip(offset.Value).Take(limit.Value).ToList();

              pLimit = limit.Value;

              pNext = pOffset + pLimit >= pTotal ? pOffset : pOffset + pLimit;
              pPrev = pOffset - pLimit < 0 ? 0 : pOffset - pLimit;
              pLast = (((pTotal - 1) - pOffset) % pLimit) == 0 ? pTotal - 1 : (pTotal - 1) - (((pTotal - 1) - pOffset) % pLimit);
            }
          }
          else // Without Pagination QueryString
          {
            pOffset = 0;

            if (pTotal < DEFAULT_LIMIT)
            {
              teams = teams.OrderBy(u => u.Id).Skip(0).Take(pTotal).ToList();

              pLimit = pTotal;

              pNext = 0;
              pPrev = 0;
              pLast = 0;
            }
            else
            {
              teams = teams.OrderBy(u => u.Id).Skip(0).Take(DEFAULT_LIMIT).ToList();

              pLimit = DEFAULT_LIMIT;

              pNext = pOffset + pLimit >= pTotal ? pOffset : pOffset + pLimit;
              pPrev = 0;
              pLast = (((pTotal - 1) - pOffset) % pLimit) == 0 ? pTotal - 1 : (pTotal - 1) - (((pTotal - 1) - pOffset) % pLimit);
            }
          }

          var pagination = new PaginationPayload
          {
            offset = pOffset,
            limit = pLimit,
            total = pTotal,
            links = new PaginationPayloadLinksDetail
            {
              next = Request.Scheme + "://" + Request.Host + Request.Path.Value + "?offset=" + pNext + "&limit=" + pLimit,
              prev = Request.Scheme + "://" + Request.Host + Request.Path.Value + "?offset=" + pPrev + "&limit=" + pLimit,
              first = Request.Scheme + "://" + Request.Host + Request.Path.Value + "?offset=" + pFirst + "&limit=" + pLimit,
              last = Request.Scheme + "://" + Request.Host + Request.Path.Value + "?offset=" + pLast + "&limit=" + pLimit
            }
          };

          response = Ok(new { teams, pagination });
        }
        else
        {
          response = CustomHttpResponse.Error(HttpStatusCode.NotFound, "UND002", "No Data Found.", "ไม่พบข้อมูล", "No data found.");
        }
      }
      else
      {
        response = CustomHttpResponse.Error(HttpStatusCode.Unauthorized, "UND999", "Unauthorized, Invalid AccessToken.", "แอพฯ ของคุณไม่มีสิทธิ์ใช้งาน เนื่องจาก AccessToken ไม่ถูกต้อง หรือหมดอายุแล้ว, กรุณาติดต่อผู้ดูแลแอพฯ ของคุณ", "The AccessToken is invalid or expired, please contact your Application Administrator.");
      }

      return response;
    }

    // GET api/teams/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
      IActionResult response = Unauthorized();

      if (id.Length == 9)
      {
        if (CheckIsValidJwt())
        {
          var resp = await _fireObj.GetTeamsById(id);
          var jsonString = resp.Content.ReadAsStringAsync().Result;

          if (resp.StatusCode == HttpStatusCode.OK && (jsonString != "null" && jsonString != null))
          {
            var team = JsonConvert.DeserializeObject<Teams>(jsonString);

            response = Ok(team);
          }
          else
          {
            response = CustomHttpResponse.Error(HttpStatusCode.NotFound, "UND002", "No Data Found.", "ไม่พบข้อมูล", "No data found.");
          }
        }
        else
        {
          response = CustomHttpResponse.Error(HttpStatusCode.Unauthorized, "UND999", "Unauthorized, Invalid AccessToken.", "แอพฯ ของคุณไม่มีสิทธิ์ใช้งาน เนื่องจาก AccessToken ไม่ถูกต้อง หรือหมดอายุแล้ว, กรุณาติดต่อผู้ดูแลแอพฯ ของคุณ", "The AccessToken is invalid or expired, please contact your Application Administrator.");
        }
      }
      else
      {
        response = CustomHttpResponse.Error(HttpStatusCode.BadRequest, "UND001", "Invalid Request (Id '" + id + "').", "คุณไม่มีสิทธิ์ใช้งาน เนื่องจากส่งคำร้องขอมาไม่ถูกต้อง", "The request is invalid.");
      }

      return response;
    }
    #endregion


    #region PRIVATE METHODS
    private bool CheckIsValidJwt()
    {
      var result = false;

      // Get Authorization header value
      if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorization))
      {
        return result;
      }
      else
      {
        var token = authorization.First();
        token = token.Replace("Bearer ", "");

        var jwt = new JwtChecker(_config);

        return jwt.Check(token).Result;
      }
    }
    #endregion
  }
}