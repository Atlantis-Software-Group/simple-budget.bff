
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace simple_budget.bff.tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _code;
    private readonly string _response;
    public MockHttpMessageHandler(HttpStatusCode statusCode, string response)
    {
        _code = statusCode;
        _response = response;
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage() { 
            Content = new StringContent(_response),  
            StatusCode = _code 
        });
    }
}
