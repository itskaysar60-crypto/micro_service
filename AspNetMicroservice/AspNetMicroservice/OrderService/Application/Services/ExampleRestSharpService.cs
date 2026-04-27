using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RestSharp;
using OrderService.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Application.Services;

public interface IExampleRestSharpService
{
    Task<bool> PostOrderDataAsync(CreateOrderDto orderData, string bearerToken);
    Task<List<OrderResponseDto>> GetExternalOrdersAsync(string bearerToken);
}

/// <summary>
/// A simple example demonstrating how to use RestSharp directly to POST and GET data
/// to/from an external API using a bearer token, similar to your provided example.
/// </summary>
public class ExampleRestSharpService : IExampleRestSharpService
{
    private readonly IConfiguration _configuration;

    public ExampleRestSharpService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 1. POST example: Post order data to an external API
    /// </summary>
    public async Task<bool> PostOrderDataAsync(CreateOrderDto orderData, string bearerToken)
    {
        // In a real app, define these in appsettings.json
        var baseUrl = _configuration["ExternalApi:BaseUrl"] ?? "https://api.example.com";
        var endpoint = _configuration["ExternalApi:PostOrderEndpoint"] ?? "/api/external-orders";

        var client = new RestClient(baseUrl);
        var request = new RestRequest(endpoint, Method.Post);
        
        // Add Bearer token
        request.AddHeader("Authorization", $"Bearer {bearerToken}");
        
        // Add JSON body (RestSharp will serialize it automatically)
        request.AddJsonBody(orderData);

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new Exception($"External API POST call failed: {response.StatusCode} - {response.Content}");

        return true;
    }

    /// <summary>
    /// 2. GET example: Get order data from an external API
    /// </summary>
    public async Task<List<OrderResponseDto>> GetExternalOrdersAsync(string bearerToken)
    {
        var baseUrl = _configuration["ExternalApi:BaseUrl"] ?? "https://api.example.com";
        var endpoint = _configuration["ExternalApi:GetOrdersEndpoint"] ?? "/api/external-orders";

        var client = new RestClient(baseUrl);
        var request = new RestRequest(endpoint, Method.Get);
        
        // Add Bearer token
        request.AddHeader("Authorization", $"Bearer {bearerToken}");

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new Exception($"External API GET call failed: {response.StatusCode} - {response.Content}");

        if (string.IsNullOrWhiteSpace(response.Content))
            return new List<OrderResponseDto>();

        // Deserialize the response content
        var syncResults = JsonSerializer.Deserialize<List<OrderResponseDto>>(
            response.Content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return syncResults ?? new List<OrderResponseDto>();
    }
}
