using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Dwolla.Client.Models;
using Dwolla.Client.Models.Requests;
using Dwolla.Client.Models.Responses;
using Dwolla.Client.Rest;
using Moq;
using Newtonsoft.Json;
using Xunit;
using File = Dwolla.Client.Models.File;

namespace Dwolla.Client.Tests
{
    public class DwollaClientShould
    {
        private const string JsonV1 = "application/vnd.dwolla.v1.hal+json";
        private const string RequestId = "some-id";
        private const string UserAgent = "dwolla-v2-csharp/4.0.11";
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
            var response = CreateRestResponse<TestResponse>(HttpMethod.Post, null, ex: e);
            var httpRequest = CreateAuthHttpRequest();
            _restClient.Setup(x => x.SendAsync<TestResponse>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => AppTokenCallback(httpRequest, y)).ReturnsAsync(response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.PostAuthAsync<TestRequest, TestResponse>(AuthRequestUri, Request));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Null(ex.Error);
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
            var response = CreateRestResponse<TestResponse>(HttpMethod.Get, null, ex: e);
            SetupForGet(CreateRequest(HttpMethod.Get), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(
                () => _client.GetAsync<TestResponse>(RequestUri, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Null(ex.Error);
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
            var response = CreateRestResponse<TestResponse>(HttpMethod.Post, null, ex: e);
            SetupForPost(CreatePostRequest(), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.PostAsync<TestRequest, TestResponse>(RequestUri, Request, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Null(ex.Error);
        }

        [Fact]
        public async void CreateNoModelResponsePostRequestAndPassToClient()
        {
            var response = CreateRestResponse<object>(HttpMethod.Post);
            SetupForPost(CreatePostRequest(), response);

            var actual = await _client.PostAsync(RequestUri, Request, Headers);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void ThrowOnNoModelResponsePostAsyncException()
        {
            var e = CreateRestException();
            var response = CreateRestResponse<object>(HttpMethod.Post, null, ex: e);
            SetupForPost(CreatePostRequest(), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.PostAsync(RequestUri, Request, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Null(ex.Error);
        }

        [Fact]
        public async void CreateUploadRequestAndPassToClient()
        {
            var request = CreateUploadRequest();
            var response = CreateRestResponse<object>(HttpMethod.Post);
            SetupForUpload(CreateUploadRequest(request), response);

            var actual = await _client.UploadAsync(RequestUri, request, Headers);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void ThrowOnUploadAsyncException()
        {
            var e = CreateRestException();
            var request = CreateUploadRequest();
            var response = CreateRestResponse<object>(HttpMethod.Post, null, ex: e);
            SetupForUpload(CreateUploadRequest(request), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.UploadAsync(RequestUri, request, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Null(ex.Error);
        }

        [Fact]
        public async void CreateDeleteRequestAndPassToClient()
        {
            var response = CreateRestResponse<object>(HttpMethod.Delete);
            SetupForDelete(CreateDeleteRequest(Request), response);

            var actual = await _client.DeleteAsync(RequestUri, Request, Headers);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void CreateNoContentDeleteRequestAndPassToClient()
        {
            var response = CreateRestResponse<object>(HttpMethod.Delete);
            SetupForDelete(CreateDeleteRequest(null), response);

            var actual = await _client.DeleteAsync<object>(RequestUri, null, Headers);

            Assert.Equal(response, actual);
        }

        [Fact]
        public async void ThrowOnDeleteAsyncException()
        {
            var e = CreateRestException();
            var response = CreateRestResponse<object>(HttpMethod.Delete, null, ex: e);
            SetupForDelete(CreateDeleteRequest(Request), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(() =>
                _client.DeleteAsync(RequestUri, Request, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Null(ex.Error);
        }

        [Fact]
        public async void DeserializeError()
        {
            var error = new ErrorResponse {Code = "ExpiredAccessToken", Message = "Access token expired."};
            var e = CreateRestException(JsonConvert.SerializeObject(error));
            var response = CreateRestResponse<TestResponse>(HttpMethod.Get, null, ex: e);
            SetupForGet(CreateRequest(HttpMethod.Get), response);

            var ex = await Assert.ThrowsAsync<DwollaException>(
                () => _client.GetAsync<TestResponse>(RequestUri, Headers));

            Assert.Equal(GetMessage(response.Response), ex.Message);
            Assert.Equal(e.Content, ex.Content);
            Assert.Equal(response.Response, ex.Response);
            Assert.Equal(error.Code, ex.Error.Code);
            Assert.Equal(error.Message, ex.Error.Message);
        }

        private static HttpRequestMessage CreatePostRequest() => CreateContentRequest(HttpMethod.Post, Request);

        private static UploadDocumentRequest CreateUploadRequest() => new UploadDocumentRequest
        {
            DocumentType = "idCard",
            Document = new File
            {
                ContentType = "image/png",
                Filename = "test.png",
                Stream = new Mock<Stream>().Object
            }
        };

        private static HttpRequestMessage CreateUploadRequest(UploadDocumentRequest request)
        {
            var r = CreateRequest(HttpMethod.Post);
            r.Content = new MultipartFormDataContent($"----------Upload")
            {
                {new StringContent(request.DocumentType), "\"documentType\""},
                GetFileContent(request.Document)
            };
            return r;
        }

        private static HttpRequestMessage CreateDeleteRequest(TestRequest content) =>
            CreateContentRequest(HttpMethod.Delete, content);

        private static HttpRequestMessage CreateContentRequest(HttpMethod method, TestRequest content)
        {
            var r = CreateRequest(method);
            r.Content = content != null
                ? new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, JsonV1)
                : null;
            return r;
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method)
        {
            var r = new HttpRequestMessage(method, RequestUri);
            foreach (var header in Headers) r.Headers.Add(header.Key, header.Value);
            return r;
        }

        private static RestResponse<T> CreateRestResponse<T>(HttpMethod method, T content = null,
            string rawContent = null, RestException ex = null) where T : class
        {
            var r = new HttpResponseMessage
            {
                RequestMessage = new HttpRequestMessage {RequestUri = RequestUri, Method = method}
            };
            r.Headers.Add("x-request-id", RequestId);
            return new RestResponse<T>(r, content, rawContent, ex);
        }

        private static HttpRequestMessage CreateAuthHttpRequest() =>
            new HttpRequestMessage(HttpMethod.Post, AuthRequestUri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(Request), Encoding.UTF8, "application/json")
            };

        private static RestException CreateRestException(string content = "Content") =>
            new RestException("RestMessage", null, HttpStatusCode.InternalServerError, content);

        private void SetupForGet(HttpRequestMessage req, RestResponse<TestResponse> res) =>
            _restClient.Setup(x => x.SendAsync<TestResponse>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => GetCallback(req, y)).ReturnsAsync(res);

        private void SetupForPost<T>(HttpRequestMessage req, RestResponse<T> res) =>
            _restClient.Setup(x => x.SendAsync<T>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => PostCallback(req, y)).ReturnsAsync(res);

        private void SetupForUpload(HttpRequestMessage r, RestResponse<object> response) =>
            _restClient.Setup(x => x.SendAsync<object>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => UploadCallback(r, y)).ReturnsAsync(response);

        private void SetupForDelete(HttpRequestMessage req, RestResponse<object> res) =>
            _restClient.Setup(x => x.SendAsync<object>(It.IsAny<HttpRequestMessage>()))
                .Callback<HttpRequestMessage>(y => DeleteCallback(req, y)).ReturnsAsync(res);

        private static async void PostCallback(HttpRequestMessage expected, HttpRequestMessage actual)
        {
            GetCallback(expected, actual);
            Assert.Equal("{\"message\":\"requestTest\"}", await actual.Content.ReadAsStringAsync());
            Assert.Equal("application/vnd.dwolla.v1.hal+json; charset=utf-8",
                actual.Content.Headers.ContentType.ToString());
        }

        private static async void UploadCallback(HttpRequestMessage expected, HttpRequestMessage actual)
        {
            GetCallback(expected, actual);
            var content = await actual.Content.ReadAsStringAsync();
            Assert.Contains("----------Upload", content);
            Assert.Contains("documentType", content);
            Assert.Contains("file", content);
            Assert.Equal("multipart/form-data; boundary=\"----------Upload\"",
                actual.Content.Headers.ContentType.ToString());
        }

        private static void DeleteCallback(HttpRequestMessage expected, HttpRequestMessage actual) =>
            GetCallback(expected, actual);

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

        private static StreamContent GetFileContent(File file)
        {
            var fc = new StreamContent(file.Stream);
            fc.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            fc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = $"\"{file.Filename}\""
            };
            return fc;
        }

        private class TestRequest
        {
            public string Message { get; set; }
        }

        private class TestResponse : IDwollaResponse
        {
            public string Message { get; set; }
        }
    }
}