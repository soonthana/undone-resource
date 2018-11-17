using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Undone.Resource.Framework;
using Undone.Resource.Models;
using Undone.Resource.Utils;

namespace Undone.Resource.Services
{
  public class Firebase
  {
    private IConfiguration _config;
    private string accessToken = string.Empty;
    private string googleApiUrl = string.Empty;
    private string projectUrl = string.Empty;
    private string requestTokenUrl = string.Empty;
    private string serviceAccount = string.Empty;
    private string scope = string.Empty;
    private string rs256PrivateKeyXml = string.Empty;
    private string storageProject = string.Empty;
    private string storageBucket = string.Empty;

    public Firebase(IConfiguration config)
    {
      _config = config;
      googleApiUrl = _config["GoogleApi:ApiUrl"];
      projectUrl = _config["GoogleApi:Firebase:UndoneResources:ProjectUrl"];
      requestTokenUrl = _config["GoogleApi:Firebase:UndoneResources:RequestTokenUrl"];
      serviceAccount = _config["GoogleApi:Firebase:UndoneResources:ServiceAccount"];
      scope = _config["GoogleApi:Firebase:UndoneResources:Scope"];
      rs256PrivateKeyXml = _config["GoogleApi:Firebase:UndoneResources:Key:RS256:PrivateKeyXml"];
      storageProject = _config["GoogleApi:Firebase:UndoneResources:Storage:Project"];
      storageBucket = _config["GoogleApi:Firebase:UndoneResources:Storage:Bucket"];

      accessToken = GetAccessToken().Result;
    }

    #region PUBLIC METHODS
    public string TestGetAccessToken()
    {
      return GetAccessToken().Result;
    }

    #region Firebase UndoneResources.Employees
    // GET https://stn-resource.firebaseio.com/Employees.json?access_token=<ACCESS_TOKEN>
    public async Task<HttpResponseMessage> GetEmployees()
    {
      var client = new HttpClient();
      client.BaseAddress = new Uri(projectUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var response = await client.GetAsync("Employees.json?access_token=" + accessToken);

      return response;
    }

    // GET https://stn-resource.firebaseio.com/Employees/<SPECIFIC_NODE>.json?access_token=<ACCESS_TOKEN>
    public async Task<HttpResponseMessage> GetEmployeesById(string node)
    {
      var client = new HttpClient();
      client.BaseAddress = new Uri(projectUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var response = await client.GetAsync("Employees/" + node + ".json?access_token=" + accessToken);

      return response;
    }

    // PUT https://stn-resource.firebaseio.com/Employees.json?access_token=<ACCESS_TOKEN>
    public async Task<HttpResponseMessage> PutEmployees(Employees emp)
    {
      var client = new HttpClient();
      client.BaseAddress = new Uri(projectUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var jsonString = JsonConvert.SerializeObject(emp);
      var uniqueId = emp.Id.ToString();
      var response = await client.PutAsync("Employees/" + uniqueId + ".json?access_token=" + accessToken, new StringContent(jsonString, Encoding.UTF8, "application/json"));

      return response;
    }
    #endregion

    #region Firebase UndoneResources.Teams
    // GET https://stn-resource.firebaseio.com/Teams.json?access_token=<ACCESS_TOKEN>
    public async Task<HttpResponseMessage> GetTeams()
    {
      var client = new HttpClient();
      client.BaseAddress = new Uri(projectUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var response = await client.GetAsync("Teams.json?access_token=" + accessToken);

      return response;
    }

    // GET https://stn-resource.firebaseio.com/Teams/<SPECIFIC_NODE>.json?access_token=<ACCESS_TOKEN>
    public async Task<HttpResponseMessage> GetTeamsById(string node)
    {
      var client = new HttpClient();
      client.BaseAddress = new Uri(projectUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var response = await client.GetAsync("Teams/" + node + ".json?access_token=" + accessToken);

      return response;
    }

    // PUT https://stn-resource.firebaseio.com/Teams.json?access_token=<ACCESS_TOKEN>
    public async Task<HttpResponseMessage> PutTeams(Teams team)
    {
      var client = new HttpClient();
      client.BaseAddress = new Uri(projectUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var jsonString = JsonConvert.SerializeObject(team);
      var uniqueId = team.Id.ToString();
      var response = await client.PutAsync("Teams/" + uniqueId + ".json?access_token=" + accessToken, new StringContent(jsonString, Encoding.UTF8, "application/json"));

      return response;
    }
    #endregion
    #endregion


    #region PRIVATE METHODS
    // POST https://www.googleapis.com/oauth2/v4/token
    private async Task<string> GetAccessToken()
    {
      var jwtRequest = GenerateJwtRequestByRSAKey();

      var body = "grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer&assertion=" + jwtRequest;

      var client = new HttpClient();
      client.BaseAddress = new Uri(requestTokenUrl);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

      var response = await client.PostAsync("token", new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"));

      if (response.StatusCode == HttpStatusCode.OK)
      {
        var jsonContent = response.Content.ReadAsStringAsync().Result;
        var obj = JsonConvert.DeserializeObject<GoogleAccessToken>(jsonContent);

        return obj.access_token;
      }
      else
      {
        return "";
      }
    }

    private string GenerateJwtRequestByRSAKey()
    {
      var payloadObj = new Payload();
      payloadObj.iss = serviceAccount;
      payloadObj.scope = scope;
      payloadObj.aud = requestTokenUrl + "token";
      payloadObj.exp = Convert.ToInt32(DateTimes.ConvertToUnixTimeByDateTime(DateTime.UtcNow.AddMinutes(60)));
      payloadObj.iat = Convert.ToInt32(DateTimes.ConvertToUnixTimeByDateTime(DateTime.UtcNow));

      SigningCredentials creds;

      using (RSA privateRsa = RSA.Create())
      {
        var privateKeyXml = File.ReadAllText(rs256PrivateKeyXml);
        privateRsa.fromXmlString(privateKeyXml);
        var privateKey = new RsaSecurityKey(privateRsa);
        creds = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
      }

      var claims = new[] {
        new Claim("scope", payloadObj.scope),
        new Claim(JwtRegisteredClaimNames.Iat, payloadObj.iat.ToString()),
        new Claim(JwtRegisteredClaimNames.Exp, payloadObj.exp.ToString())
      };
      var token = new JwtSecurityToken(
        payloadObj.iss,
        payloadObj.aud,
        claims,
        signingCredentials: creds
      );

      var result = new JwtSecurityTokenHandler().WriteToken(token);

      return result;
    }

    private class Payload
    {
      public string iss { get; set; }
      public string scope { get; set; }
      public string aud { get; set; }
      public int exp { get; set; }
      public int iat { get; set; }
    }

    private class GoogleAccessToken
    {
      public string access_token { get; set; }
      public string token_type { get; set; }
      public int expires_in { get; set; }
    }
    #endregion
  }
}