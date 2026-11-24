using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SmintIo.Portals.SDK.Core.Http.Prefab.Exceptions;
using SmintIo.Portals.SDK.Core.Http.Prefab.Extensions;
using SmintIo.Portals.SDK.Core.Http.Prefab.RequestFailedHandlers;
using SmintIo.Portals.SDK.Core.Models.Context;
using SmintIo.Portals.SDK.Core.Rest.Prefab.Exceptions;

namespace SmintIo.Portals.Connector.SharePoint.Client.Impl
{
    public class SharepointDefaultRequestFailedHandler : DefaultRequestFailedHandler
    {
        private const string _retryAfter = "Retry-After";

        private readonly ILogger _logger;

        public SharepointDefaultRequestFailedHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override Task<RequestFailedHandlerResult> HandleOtherExceptionAsync(string requestUri, int tryCount, IPortalsContextModel portalsContextModel, Exception exception)
        {
            if (exception is ServiceException serviceException)
            {
                // translate service exception to HttpStatusException, handle other Sharepoint states

                var message = serviceException.Message;

                if (!string.IsNullOrEmpty(message))
                {
                    if (message.Contains("VideoProcessing_ByteStreamTypeNotSupported") ||
                        message.Contains("SubStreamCached_GeneralFailure") ||
                        message.Contains("SubStreamCached_FormatNotSupported") ||
                        message.Contains("SubStreamCached_FileTooBig") ||
                        message.Contains("SubStreamCached_SandboxTimeout") ||
                        message.Contains("SubStreamCached_SandBoxFailure") ||
                        message.Contains("SubStreamCached_BadRequest") ||
                        message.Contains("SubStreamCached_ImageProcessingFailure") ||
                        message.Contains("SubStreamCached_VideoThumbnailProcessingFailure") ||
                        message.Contains("SubStreamCached_Fatal") ||
                        message.Contains("Sandbox_VideoProcessing_CorruptInput") ||
                        message.Contains("Service_InvalidInput_FileTooBigToConvert") ||
                        message.Contains("OfficeConversion_BadRequest") ||
                        message.Contains("VideoBitrateUnsupported_BitrateTooHigh") ||
                        message.Contains("Web_416RequestedRangeNotSatisfiable") ||
                        message.Contains("RequestedRangeNotSatisfiable"))
                    {
                        // permanent error, occuring when reading streams
                        // this will cause the returned stream to be NULL

                        return Task.FromResult(RequestFailedHandlerResult.Ignore);
                    }
                }

                var errorCode = serviceException.Error?.Code;
                var statusCode = (int)serviceException.StatusCode;

                // https://learn.microsoft.com/en-us/graph/errors
                // https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/dev/src/Microsoft.Graph/Enums/GraphErrorCode.cs

                if (!string.IsNullOrEmpty(errorCode))
                {
                    if (string.Equals(errorCode, "itemNotFound"))
                    {
                        throw new ExternalDependencyException(ExternalDependencyStatusEnum.GetNotFound, "The desired record was not found", SharepointConnectorStartup.SharepointConnector, innerException: serviceException);
                    }
                    else if (serviceException.StatusCode == HttpStatusCode.TooManyRequests ||
                        statusCode == 509 || // Bandwidth Limit Exceeded
                        string.Equals(errorCode, "activityLimitReached") || 
                        string.Equals(errorCode, "tooManyRetries") || 
                        string.Equals(errorCode, "tooManyRedirects"))
                    {
                        var retryAfter = GetRetryAfter(serviceException.ResponseHeaders);

                        throw new TooManyRequestsExternalDependencyException("Quota exceeded", retryAfter, SharepointConnectorStartup.SharepointConnector, serviceException);
                    }
                    else if (string.Equals(errorCode, "notSupported") ||
                        string.Equals(errorCode, "invalidRequest"))
                    {
                        // permanent error, occuring when reading streams
                        // this will cause the returned stream to be NULL

                        return Task.FromResult(RequestFailedHandlerResult.Ignore);
                    }
                    else if (string.Equals(errorCode, "generalException"))
                    {
                        var innerErrorCode = serviceException.Error?.InnerError?.Code;

                        if (!string.IsNullOrEmpty(innerErrorCode))
                        {
                            if (string.Equals(innerErrorCode, "SubStreamCached_SandBoxFailure") ||
                                string.Equals(innerErrorCode, "SubStreamCached_BadRequest") ||
                                string.Equals(innerErrorCode, "SubStreamCached_VideoThumbnailProcessingFailure") ||
                                string.Equals(innerErrorCode, "Sandbox_VideoProcessing_CorruptInput"))
                            {
                                // permanent error

                                return Task.FromResult(RequestFailedHandlerResult.Ignore);
                            }
                        }
                    }
                }

                throw new HttpStatusException(
                    requestUri,
                    tryCount,
                    portalsContextModel,
                    statusCode,
                    statusMessage: $"{serviceException.Message} (error code {errorCode})",
                    responseBody: null,
                    innerException: serviceException);
            }

            return base.HandleOtherExceptionAsync(requestUri, tryCount, portalsContextModel, exception);
        }

        public override async Task<RequestFailedHandlerResult> HandleHttpStatusExceptionAsync(HttpStatusException httpStatusException)
        {
            var httpResponseMessage = httpStatusException.HttpResponseMessage;

            if (httpResponseMessage != null)
            {
                if (httpResponseMessage.Headers != null &&
                    httpResponseMessage.Headers.TryGetValues("x-errorcode", out var values) &&
                    values != null &&
                    values.Any())
                {
                    var errorCode = values.First();

                    if (!string.IsNullOrEmpty(errorCode))
                    {
                        if (string.Equals(errorCode, "VideoProcessing_ByteStreamTypeNotSupported") ||
                            string.Equals(errorCode, "SubStreamCached_GeneralFailure") ||
                            string.Equals(errorCode, "SubStreamCached_FormatNotSupported") ||
                            string.Equals(errorCode, "SubStreamCached_FileTooBig") ||
                            string.Equals(errorCode, "SubStreamCached_SandboxTimeout") ||
                            string.Equals(errorCode, "SubStreamCached_SandBoxFailure") ||
                            string.Equals(errorCode, "SubStreamCached_BadRequest") ||
                            string.Equals(errorCode, "SubStreamCached_ImageProcessingFailure") ||
                            string.Equals(errorCode, "SubStreamCached_VideoThumbnailProcessingFailure") ||
                            string.Equals(errorCode, "SubStreamCached_Fatal") ||
                            // e.g. Sandbox_VideoProcessing_CorruptInput_1894.08ce
                            errorCode.StartsWith("Sandbox_VideoProcessing_CorruptInput") ||
                            string.Equals(errorCode, "Service_InvalidInput_FileTooBigToConvert") ||
                            string.Equals(errorCode, "OfficeConversion_BadRequest") ||
                            string.Equals(errorCode, "VideoBitrateUnsupported_BitrateTooHigh") ||
                            string.Equals(errorCode, "Web_416RequestedRangeNotSatisfiable") ||
                            string.Equals(errorCode, "BadArgument"))
                        {
                            // permanent error, occuring when reading streams
                            // this will cause the returned stream to be NULL

                            return RequestFailedHandlerResult.Ignore;
                        }
                    }
                }

                try
                {
                    var content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                    // a thumbnail download from the media CDN can report a failed video thumbnail in the body only

                    if (!string.IsNullOrEmpty(content) &&
                        (content.Contains("RequestedRangeNotSatisfiable") ||
                         content.Contains("SubStreamCached_VideoThumbnailProcessingFailure")))
                    {
                        // permanent error, occuring when reading streams
                        // this will cause the returned stream to be NULL

                        return RequestFailedHandlerResult.Ignore;
                    }
                }
                catch (Exception)
                {
                    // cannot read content, ignore...
                }
            }

            if (httpStatusException.StatusCode == (int)HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                // permanent error, occuring when reading streams
                // this will cause the returned stream to be NULL

                return RequestFailedHandlerResult.Ignore;
            }

            return await base.HandleHttpStatusExceptionAsync(httpStatusException).ConfigureAwait(false);
        }

        /// <summary>
        /// https://learn.microsoft.com/en-us/graph/throttling
        /// https://learn.microsoft.com/en-us/graph/throttling-limits
        /// https://learn.microsoft.com/en-us/azure/architecture/patterns/throttling
        /// </summary>
        private TimeSpan? GetRetryAfter(HttpResponseHeaders httpResponseHeaders)
        {
            if (httpResponseHeaders == null || !httpResponseHeaders.TryGetValues(_retryAfter, out var retryAfterValues))
            {
                _logger.LogWarning("SharePoint returned too many requests response without retry after header");

                // IL will fall back to the default retry after value

                return null;
            }

            var retryAfterStringValue = retryAfterValues?.FirstOrDefault();

            if (retryAfterStringValue == null || !double.TryParse(retryAfterStringValue, out var retryAfterInSeconds) || retryAfterInSeconds < 0)
            {
                _logger.LogWarning($"SharePoint returned too many requests response with invalid retry after header ({retryAfterStringValue})");

                return null;
            }

            return TimeSpan.FromSeconds(retryAfterInSeconds);
        }
    }
}
