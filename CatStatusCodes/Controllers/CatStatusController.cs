using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace CatStatusCodes.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        public CatController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> GetCatImage([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return await GetCatImageByStatusCode(400);
            }

            HttpClient httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync(url);
            }
            catch (HttpRequestException e)
            {
                return await GetCatImageByStatusCode(400, e.Message);
            }
            catch (Exception e)
            {
                return await GetCatImageByStatusCode(500, e.Message);
            }
            var statusCode = (int)response.StatusCode;
            return await GetCatImageByStatusCode(statusCode);
        }

        private async Task<IActionResult> GetCatImageByStatusCode(int statusCode, string errorMessage = null)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            if (!_memoryCache.TryGetValue(statusCode, out byte[] catImage))
            {
                var catApiUrl = $"https://http.cat/{statusCode}";
                try
                {
                    catImage = await httpClient.GetByteArrayAsync(catApiUrl);
                }
                catch (HttpRequestException e)
                {
                    return StatusCode(500, $"Error while fetching the cat image: {e.Message} - {e.InnerException?.Message}");
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                _memoryCache.Set(statusCode, catImage, cacheEntryOptions);
            }
            return File(catImage, "image/jpeg");
        }
    }
}
