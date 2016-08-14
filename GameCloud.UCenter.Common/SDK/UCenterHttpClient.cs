﻿using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using GameCloud.UCenter.Common.Portable.Contracts;
using GameCloud.UCenter.Common.Portable.Exceptions;

namespace GameCloud.UCenter.Common.SDK
{
    public class UCenterHttpClient
    {
        private readonly HttpClient httpClient = null;

        public UCenterHttpClient()
        {
            httpClient = CreateHttpClient();
        }

        public Task<TResponse> SendAsync<TContent, TResponse>(HttpMethod method, string url, TContent content)
        {
            HttpContent httpContent = null;
            if (content is HttpContent)
            {
                httpContent = content as HttpContent;
            }
            else
            {
                httpContent = new ObjectContent<TContent>(content, new JsonMediaTypeFormatter());
            }

            return this.SentAsync<TResponse>(method, url, httpContent);
        }

        public async Task<TResponse> SentAsync<TResponse>(HttpMethod method, string url, HttpContent content)
        {
            var request = new HttpRequestMessage(method, new Uri(url));
            request.Headers.Clear();
            request.Headers.ExpectContinue = false;
            request.Content = content;

            var response = await this.httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<TResponse>();
        }

        public async Task<TResult> SendAsyncWithException<TContent, TResult>(HttpMethod method, string url,
            TContent content)
        {
            var response = await this.SendAsync<TContent, UCenterResponse<TResult>>(method, url, content);
            if (response.Status == UCenterResponseStatus.Success)
            {
                return response.Result;
            }

            if (response.Error != null)
            {
                throw new UCenterException(response.Error.ErrorCode, response.Error.Message);
            }

            throw new UCenterException(UCenterErrorCode.ClientError, "Error occurred when sending http request");
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            handler.ClientCertificateOptions = ClientCertificateOption.Automatic;

            var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            return httpClient;
        }
    }
}
