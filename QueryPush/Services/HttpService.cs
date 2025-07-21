using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IHttpService
{
    Task SendDataAsync(string endpointName, IEnumerable<Dictionary<string, object>> data, PayloadFormatType format);
}

public class HttpService(
    IOptionsMonitor<QueryPushSettings> options, 
    HttpClient httpClient,
    ILogger<HttpService> logger)
    : IHttpService
{
    public async Task SendDataAsync(string endpointName, IEnumerable<Dictionary<string, object>> data, PayloadFormatType format)
    {
        var endpoint = options.CurrentValue.Endpoints.FirstOrDefault(e => e.Name == endpointName);
        if (endpoint == null)
        {
            logger.LogError("Endpoint '{EndpointName}' not found in configuration", endpointName);
            throw new InvalidOperationException($"Endpoint '{endpointName}' not found");
        }

        var chunks = ChunkData(data, endpoint.PayloadSize);
        var chunkCount = chunks.Count();

        logger.LogInformation("Sending data to endpoint '{EndpointName}' ({Method} {Url}). Data split into {ChunkCount} chunks, payload format: {Format}", 
            endpointName, endpoint.Method, endpoint.Url, chunkCount, format);

        var chunkIndex = 0;
        foreach (var chunk in chunks)
        {
            chunkIndex++;
            await SendChunkWithRetryAsync(endpoint, chunk, format, chunkIndex, chunkCount);
            
            if (endpoint.RequestDelay > 0 && chunkIndex < chunkCount)
            {
                logger.LogDebug("Waiting {RequestDelay}ms before next request", endpoint.RequestDelay);
                await Task.Delay(endpoint.RequestDelay);
            }
        }

        logger.LogInformation("Completed sending all {ChunkCount} chunks to endpoint '{EndpointName}'", chunkCount, endpointName);
    }

    private async Task SendChunkWithRetryAsync(EndpointConfig endpoint, IEnumerable<Dictionary<string, object>> chunk, PayloadFormatType format, int chunkIndex, int chunkCount)
    {
        var chunkSize = chunk.Count();
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= endpoint.RetryAttempts)
        {
            attempt++;
            try
            {
                var content = FormatPayload(chunk, format);
                using var request = CreateHttpRequest(endpoint, content);
                
                logger.LogDebug("Sending chunk {ChunkIndex}/{ChunkCount} with {RecordCount} records to {Url} (attempt {Attempt})", 
                    chunkIndex, chunkCount, chunkSize, endpoint.Url, attempt);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.SendAsync(request);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("HTTP request successful: {StatusCode} in {ElapsedMs}ms (chunk {ChunkIndex}/{ChunkCount}, attempt {Attempt})", 
                        (int)response.StatusCode, stopwatch.ElapsedMilliseconds, chunkIndex, chunkCount, attempt);
                    return;
                }
                else
                {
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt <= endpoint.RetryAttempts)
                {
                    var delay = CalculateDelay(endpoint, attempt);
                    logger.LogWarning(ex, "HTTP request failed for chunk {ChunkIndex}/{ChunkCount} on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms", 
                        chunkIndex, chunkCount, attempt, endpoint.RetryAttempts + 1, delay);
                    await Task.Delay(delay);
                }
                else
                {
                    logger.LogError(ex, "HTTP request failed for chunk {ChunkIndex}/{ChunkCount} on final attempt {Attempt}/{MaxAttempts}", 
                        chunkIndex, chunkCount, attempt, endpoint.RetryAttempts + 1);
                }
            }
        }

        throw lastException!;
    }

    private int CalculateDelay(EndpointConfig endpoint, int attempt)
    {
        var delay = endpoint.RetryStrategy switch
        {
            RetryStrategyType.ExponentialBackoff => (int)(endpoint.BackOffSeconds * 1000 * Math.Pow(2, attempt - 1)),
            _ => endpoint.BackOffSeconds * 1000
        };

        logger.LogDebug("Calculated HTTP retry delay using {RetryStrategy}: {DelayMs}ms", 
            endpoint.RetryStrategy, delay);

        return delay;
    }

    private IEnumerable<IEnumerable<Dictionary<string, object>>> ChunkData(IEnumerable<Dictionary<string, object>> data, int chunkSize)
    {
        if (chunkSize == int.MaxValue)
        {
            yield return data;
            yield break;
        }

        var chunk = new List<Dictionary<string, object>>();
        foreach (var item in data)
        {
            chunk.Add(item);
            if (chunk.Count >= chunkSize)
            {
                yield return chunk.ToList();
                chunk.Clear();
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    private string FormatPayload(IEnumerable<Dictionary<string, object>> data, PayloadFormatType format)
    {
        var payload = format switch
        {
            PayloadFormatType.JsonArray => JsonSerializer.Serialize(data),
            PayloadFormatType.JsonLines => string.Join('\n', data.Select(objects => JsonSerializer.Serialize(objects))),
            _ => JsonSerializer.Serialize(data)
        };

        logger.LogDebug("Formatted payload as {Format}, size: {PayloadSize} bytes", format, payload.Length);
        return payload;
    }

    private HttpRequestMessage CreateHttpRequest(EndpointConfig endpoint, string content)
    {
        var request = new HttpRequestMessage(ConvertToHttpMethod(endpoint.Method), endpoint.Url)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        foreach (var header in endpoint.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Name, header.Value);
        }

        logger.LogDebug("Created HTTP request with {HeaderCount} headers", endpoint.Headers.Length);
        return request;
    }

    private static HttpMethod ConvertToHttpMethod(HttpMethodType methodType) => methodType switch
    {
        HttpMethodType.GET => HttpMethod.Get,
        HttpMethodType.POST => HttpMethod.Post,
        HttpMethodType.PUT => HttpMethod.Put,
        HttpMethodType.PATCH => HttpMethod.Patch,
        HttpMethodType.DELETE => HttpMethod.Delete,
        HttpMethodType.HEAD => HttpMethod.Head,
        HttpMethodType.OPTIONS => HttpMethod.Options,
        _ => HttpMethod.Post
    };
}
