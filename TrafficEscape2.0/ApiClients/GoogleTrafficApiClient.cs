using NewHorizonLib.Helpers;
using NewHorizonLib.Services;
using Newtonsoft.Json;
using System;
using TrafficEscape2.Models;

namespace TrafficEscape2._0.ApiClients
{
    public class GoogleTrafficApiClient : IGoogleTrafficApiClient
    {
        private readonly ISecretService secretService;
        private readonly ILogger<GoogleTrafficApiClient> logger;
        private string ApiKey;
        private HttpClient httpClient;
        private const int MaxRequestCountPerDay = 2500;
        private RequestThresholdPerDay requestThresholdPerDay;

        public GoogleTrafficApiClient(ISecretService secretService, ILogger<GoogleTrafficApiClient> logger)
        {
            this.secretService = secretService;
            this.logger = logger;
            httpClient = new HttpClient();
            this.requestThresholdPerDay = new RequestThresholdPerDay(MaxRequestCountPerDay);
            this.ApiKey = secretService.GetSecretValue("GOOGLE_PLACE_API_KEY");
        }

        public async Task<int> GetRouteDurationInMins(string fromPlaceId, string toPlaceId)
        {
            if (!this.requestThresholdPerDay.AllowRequest())
            {
                return -1;
            }
            HttpResponseMessage response = await httpClient.GetAsync($"https://maps.googleapis.com/maps/api/distancematrix/json?destinations=place_id:{toPlaceId}&origins=place_id:{fromPlaceId}&key={ApiKey}&departure_time=now");
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = await response.Content.ReadAsStringAsync();
                    this.logger.LogInformation($"Response from google distance API {result}");
                    DistanceApiResponse apiResponse = JsonConvert.DeserializeObject<DistanceApiResponse>(result);
                    int trafficTime = apiResponse?.rows[0]?.elements[0]?.duration_in_traffic.value ?? -1;
                    return (int)(trafficTime / 60);
                } catch (Exception e)
                {
                    this.logger.LogError(e.Message);
                }
            }
            return -1;
        }
    }
}
