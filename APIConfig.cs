using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using NLog;
using JamfMaintainer.Entities;
using JsonContent = JamfMaintainer.Entities.JsonContent;
using Newtonsoft.Json;

namespace JamfMaintainer
{

    public class APIConfig
    {

        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SettingsManager _settingsManager = new SettingsManager();



        private HttpClient GetClient()
        {
            var client = new HttpClient();

            string cred = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settingsManager.JamfUsername}:{_settingsManager.JamfPassword}"));

            client.BaseAddress = new Uri(_settingsManager.JamfBaseAPIUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", cred);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }


        public async Task<string> GetRequestAsync(string url)
        {
            try
            {
                using (var client = GetClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Info("GET request SUCCESS! Status code 200");
                        string responseContent = await response.Content.ReadAsStringAsync();
                        return responseContent;
                    }
                    else
                    {
                        _logger.Error($"GET request failed. Status Code: {response.StatusCode}");
                        _logger.Error($"REASON: {response.ReasonPhrase}");
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return "";
            }
        }

        public async Task<ApiResponse> PutRequestAsync(string url, JObject jobject)
        {
            ApiResponse apiResponse = null;
            try
            {
                using (var client = GetClient())
                {
                    HttpContent content = new JsonContent(jobject);
                    HttpResponseMessage response = await client.PutAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Info("PUT request SUCCESS! Status code 200");
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
                        }
                    }
                    else
                    {
                        _logger.Error($"PUT request failed. Status Code: {response.StatusCode}");
                        _logger.Error($"REASON: {response.ReasonPhrase}");
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return apiResponse;
        }

        public async Task<ApiResponse> PostUserAsync(JamfUser newUser)
        {
            ApiResponse apiResponse = null;

            try
            {
                using (var client = GetClient())
                {
                    JObject postObject = JObject.FromObject(newUser);

                    HttpContent content = new JsonContent(postObject);

                    HttpResponseMessage response = await client.PostAsync("users", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Info("POST request SUCCESS! Status code 200");
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
                        }
                    }
                    else
                    {
                        _logger.Error($"POST request failed. Status Code: {response.StatusCode}");
                        _logger.Error($"REASON: {response.ReasonPhrase}");
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return apiResponse;
        }

        public async Task<ApiResponse> PostGroupAsync(JObject newgroup)
        {
            ApiResponse apiResponse = null;

            try
            {
                using (var client = GetClient())
                {
                    HttpContent content = new JsonContent(newgroup);

                    HttpResponseMessage response = await client.PostAsync("users/groups", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Info("POST request SUCCESS! Status code 200");
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
                        }
                    }
                    else
                    {
                        _logger.Error($"POST request failed. Status Code: {response.StatusCode}");
                        _logger.Error($"REASON: {response.ReasonPhrase}");
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return apiResponse;
        }

        public async Task<ApiResponse> DeleteRequestAsync(string url)
        {
            ApiResponse apiResponse = null;
            try
            {
                using (var client = GetClient())
                {
                    HttpResponseMessage response = await client.DeleteAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Info("DELETE request SUCCESS! Status code 200");
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
                        }
                    }
                    else
                    {
                        _logger.Error($"DELETE request failed. Status Code: {response.StatusCode}");
                        _logger.Error($"REASON: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return apiResponse;
        }


    }
}
