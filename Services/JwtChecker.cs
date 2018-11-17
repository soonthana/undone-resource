using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Undone.Resource.Services
{
  public class JwtChecker
  {
    private IConfiguration _config;

    public JwtChecker(IConfiguration config)
    {
      _config = config;
    }

    public async Task<bool> Check(string token)
    {
      var result = false;

      try
      {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_config["AuthUrl"]);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Auth-Jwt", token);

        var response = await client.PostAsync("/api/auth", new StringContent(""));

        if (response.StatusCode == HttpStatusCode.OK)
        {
          result = true;
        }

        return result;
      }
      catch
      {
        return result;
      }
    }
  }
}