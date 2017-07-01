using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Dwolla.Client.Models;
using Dwolla.Client.Rest;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Dwolla.Client.Tests
{
    public class DwollaClientShould
    {
        private const string JsonV1 = "application/vnd.dwolla.v1.hal+json";
        private const string RequestId = "some-id";
        private const string UserAgent = "dwolla-v2-csharp/1.0.0";
        private static readonly Uri RequestUri = new Uri("https://api-sandbox.dwolla.com/foo");
        private static readonly Uri AuthRequestUri = new Uri("https://sandbox.dwolla.com/oauth/v2/foo");
        private static readonly Headers Headers = new Headers {{"key1", "value1"}, {"key2", "value2"}};
        private static readonly TestRequest Request = new TestRequest {Message = "requestTest"};
        private static readonly TestResponse Response = new TestResponse {Message = "responseTest"};

        private readonly Mock<IRestClient> _restClient;
        private readonly DwollaClient _client;

        public DwollaClientShould()
        {
            _restClient = new Mock<IRestClient>();
            _client = new DwollaClient(_restClient.Object, true);
        }

        [Fact]
        public void ConfigureHttpClient()
        {
            var client = DwollaClient.CreateHttpClient();
            Assert.Equal(UserAgent, client.DefaultRequestHeaders.UserAgent.ToString());
            Assert.Equal(JsonV1, client.DefaultRequestHeaders.Accept.ToString());
        }

        [Fact]
        public async void CreatePostAuthRequestAndPassToClient()
        {
            var response = CreateRestResponse(HttpMethod.Post, Response);
            var httpRequest = CreateAuthHttpRequest();
            _restClient.Setup(x => x.SendAsync<TestResponse>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => AppTokenCallback(httpRequest, y)).ReturnsAsync(response);

            var actual = await _client.PostAuthAsync<TestRequest, TestResponse>(AuthRequestUri, Request);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void ThrowOnPostAuthAsyncException()
        {
            var e = CreateRestException();
            var response = CreateRestResponse(HttpMethod.Post, null, e);
            var httpRequest = CreateAuthHttpRequest();
            _restClient.Setup(x => x.SendAsync<TestResponse>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => AppTokenCallback(httpRequest, y)).ReturnsAsync(response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.PostAuthAsync<TestRequest, TestResponse>(AuthRequestUri, Request));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
        }

        [Fact]
        public async void CreateGetRequestAndPassToClient()
        {
            var response = CreateRestResponse(HttpMethod.Get, Response);
            SetupForGet(CreateRequest(HttpMethod.Get), response);

            var actual = await _client.GetAsync<TestResponse>(RequestUri, Headers);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void ThrowOnGetAsyncException()
        {
            var e = CreateRestException();
            var response = CreateRestResponse(HttpMethod.Get, null, e);
            SetupForGet(CreateRequest(HttpMethod.Get), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(
                () => _client.GetAsync<TestResponse>(RequestUri, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
        }

        [Fact]
        public async void CreatePostRequestAndPassToClient()
        {
            var response = CreateRestResponse(HttpMethod.Post, Response);
            SetupForPost(CreatePostRequest(), response);

            var actual = await _client.PostAsync<TestRequest, TestResponse>(RequestUri, Request, Headers);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void ThrowOnPostAsyncException()
        {
            var e = CreateRestException();
            var response = CreateRestResponse(HttpMethod.Post, null, e);
            SetupForPost(CreatePostRequest(), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.PostAsync<TestRequest, TestResponse>(RequestUri, Request, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
        }

        private static HttpRequestMessage CreatePostRequest()
        {
            var r = CreateRequest(HttpMethod.Post);
            r.Content = new StringContent(JsonConvert.SerializeObject(Request), Encoding.UTF8, JsonV1);
            return r;
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method)
        {
            var request = new HttpRequestMessage(method, RequestUri);
            foreach (var header in Headers) request.Headers.Add(header.Key, header.Value);
            return request;
        }

        private static RestResponse<TestResponse> CreateRestResponse(
            HttpMethod method,
            TestResponse content = null,
            RestException ex = null)
        {
            var response = new HttpResponseMessage
            {
                RequestMessage = new HttpRequestMessage {RequestUri = RequestUri, Method = method}
            };
            response.Headers.Add("x-request-id", RequestId);
            return new RestResponse<TestResponse>(response, content, ex);
        }

        private static HttpRequestMessage CreateAuthHttpRequest()
        {
            return new HttpRequestMessage(HttpMethod.Post, AuthRequestUri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(Request), Encoding.UTF8, "application/json")
            };
        }

        private static RestException CreateRestException() =>
            new RestException("RestMessage", null, HttpStatusCode.InternalServerError, "Content");

        private void SetupForGet(HttpRequestMessage req, RestResponse<TestResponse> res) =>
            _restClient.Setup(x => x.SendAsync<TestResponse>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => GetCallback(req, y)).ReturnsAsync(res);

        private void SetupForPost(HttpRequestMessage req, RestResponse<TestResponse> res) =>
            _restClient.Setup(x => x.SendAsync<TestResponse>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => PostCallback(req, y)).ReturnsAsync(res);

        private static async void PostCallback(HttpRequestMessage expected, HttpRequestMessage actual)
        {
            GetCallback(expected, actual);
            Assert.Equal("{\"message\":\"requestTest\"}", await actual.Content.ReadAsStringAsync());
            Assert.Equal("application/vnd.dwolla.v1.hal+json; charset=utf-8",
                actual.Content.Headers.ContentType.ToString());
        }

        private static void GetCallback(HttpRequestMessage expected, HttpRequestMessage actual)
        {
            Assert.Equal(expected.Method, actual.Method);
            Assert.Equal(expected.RequestUri, actual.RequestUri);
            foreach (var key in Headers.Keys) Assert.True(AssertHeader(expected, actual, key));
        }

        private static async void AppTokenCallback(HttpRequestMessage expected, HttpRequestMessage actual)
        {
            Assert.Equal(expected.Method, actual.Method);
            Assert.Equal(expected.RequestUri, actual.RequestUri);
            Assert.Equal("{\"message\":\"requestTest\"}", await actual.Content.ReadAsStringAsync());
            Assert.Equal("application/json; charset=utf-8", actual.Content.Headers.ContentType.ToString());
        }

        private static bool AssertHeader(HttpRequestMessage expected, HttpRequestMessage actual, string key) =>
            expected.Headers.GetValues(key).ToString() == actual.Headers.GetValues(key).ToString();

        private static string GetMessage(HttpResponseMessage response) =>
            $"Dwolla API Error, Resource=\"{response.RequestMessage.Method} {response.RequestMessage.RequestUri}\", RequestId=\"{RequestId}\"";

        private class TestRequest
        {
            public string Message { get; set; }
        }

        private class TestResponse
        {
            public string Message { get; set; }
        }
    }
}