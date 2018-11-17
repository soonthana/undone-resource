using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
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
  [ApiVersion("1.0")]
  [Route("api/[controller]")]
  public class EmployeesController : Controller
  {
    private IConfiguration _config;
    private Firebase _fireObj;
    private const int DEFAULT_LIMIT = 10;

    public EmployeesController(IConfiguration config)
    {
      _config = config;
      _fireObj = new Firebase(_config);
    }

    #region PUBLIC METHODS
    // GET api/employees
    // GET api/employees?offset=0&limit=5
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? offset, int? limit)
    {
      IActionResult response = Unauthorized();

      if (CheckIsValidJwt())
      {
        // Data Payload
        var employees = new List<EmployeesPayload>();

        var resp = await _fireObj.GetEmployees();
        var jsonString = resp.Content.ReadAsStringAsync().Result;

        if (resp.StatusCode == HttpStatusCode.OK && (jsonString != "null" && jsonString != null))
        {
          var jObj = JObject.Parse(jsonString);

          foreach (var item in jObj)
          {
            var emp = item.Value.ToObject<EmployeesPayload>();

            var listTypes = new List<string>();
            listTypes.Add("application/json");

            emp.links = new EmployeesPayloadLinksDetail
            {
              rel = "Employee information of " + emp.Name + " " + emp.Surname,
              href = Request.Scheme + "://" + Request.Host + Request.Path.Value + "/" + emp.Id,
              action = "GET",
              types = listTypes
            };

            if (emp.Status == true)
            {
              employees.Add(emp);
            }
          }

          // Pagination Payload
          var pTotal = employees.Count;

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
              employees = employees.OrderBy(u => u.Id).Skip(offset.Value).Take(limit.Value).ToList();

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
              employees = employees.OrderBy(u => u.Id).Skip(0).Take(pTotal).ToList();

              pLimit = pTotal;

              pNext = 0;
              pPrev = 0;
              pLast = 0;
            }
            else
            {
              employees = employees.OrderBy(u => u.Id).Skip(0).Take(DEFAULT_LIMIT).ToList();

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

          response = Ok(new { employees, pagination });
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

    // GET api/employees/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
      IActionResult response = Unauthorized();

      if (id.Length == 6)
      {
        if (CheckIsValidJwt())
        {
          var resp = await _fireObj.GetEmployeesById(id);
          var jsonString = resp.Content.ReadAsStringAsync().Result;

          if (resp.StatusCode == HttpStatusCode.OK && (jsonString != "null" && jsonString != null))
          {
            var emp = JsonConvert.DeserializeObject<Employees>(jsonString);

            response = Ok(emp);
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

  [ApiVersion("2.0")]
  [Route("api/employees/v{ver:ApiVersion}")]
  public class EmployeesV2Controller : Controller
  {
    private IConfiguration _config;
    private Firebase _fireObj;
    private const int DEFAULT_LIMIT = 10;

    public EmployeesV2Controller(IConfiguration config)
    {
      _config = config;
      _fireObj = new Firebase(_config);
    }

    #region PUBLIC METHODS
    // GET api/employees
    [HttpGet]
    public async Task<IActionResult> Get()
    {
      IActionResult response = Unauthorized();

      if (CheckIsValidJwt())
      {
        var user = GetUserIdFromJwt();

        if (user != null && user != "")
        {
          var resp = await _fireObj.GetEmployeesById(user);
          var jsonString = resp.Content.ReadAsStringAsync().Result;

          if (resp.StatusCode == HttpStatusCode.OK && (jsonString != "null" && jsonString != null))
          {
            var emp = JsonConvert.DeserializeObject<Employees>(jsonString);

            response = Ok(emp);
          }
          else
          {
            response = CustomHttpResponse.Error(HttpStatusCode.NotFound, "UND002", "No Data Found.", "ไม่พบข้อมูล", "No data found.");
          }
        }
        else
        {
          response = CustomHttpResponse.Error(HttpStatusCode.BadRequest, "UND001", "Invalid Request (Id '" + user + "').", "คุณไม่มีสิทธิ์ใช้งาน เนื่องจากส่งคำร้องขอมาไม่ถูกต้อง", "The request is invalid.");
        }
      }
      else
      {
        response = CustomHttpResponse.Error(HttpStatusCode.Unauthorized, "UND999", "Unauthorized, Invalid AccessToken.", "แอพฯ ของคุณไม่มีสิทธิ์ใช้งาน เนื่องจาก AccessToken ไม่ถูกต้อง หรือหมดอายุแล้ว, กรุณาติดต่อผู้ดูแลแอพฯ ของคุณ", "The AccessToken is invalid or expired, please contact your Application Administrator.");
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

    private string GetUserIdFromJwt()
    {
      Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorization);
      var token = authorization.First();
      token = token.Replace("Bearer ", "");
      string[] jwtArray = token.Split('.');

      var payload = JwtPayload.Base64UrlDeserialize(jwtArray[1]);

      return payload.Sub;
    }
    #endregion
  }
}