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

        // a permanent error as well, the one code we have seen with a suffix, e.g. Sandbox_VideoProcessing_CorruptInput_1894.08ce

        private const string _corruptInputErrorCode = "Sandbox_VideoProcessing_CorruptInput";

        // permanent errors, occuring when reading streams: the returned stream is NULL
        // SharePoint reports them in the Graph error, the x-errorcode header or, from the media CDN, in the body only

        private static readonly string[] _permanentErrorCodes =
        [
            "VideoProcessing_ByteStreamTypeNotSupported",
            "SubStreamCached_GeneralFailure",
            "SubStreamCached_FormatNotSupported",
            "SubStreamCached_FileTooBig",
            "SubStreamCached_SandboxTimeout",
            "SubStreamCached_SandBoxFailure",
            "SubStreamCached_BadRequest",
            "SubStreamCached_ImageProcessingFailure",
            "SubStreamCached_VideoThumbnailProcessingFailure",
            "SubStreamCached_Fatal",
            "Sandbox_VideoProcessing_InvalidData",
            "Service_InvalidInput_FileTooBigToConvert",
            "OfficeConversion_BadRequest",
            "VideoBitrateUnsupported_BitrateTooHigh",
            "Web_416RequestedRangeNotSatisfiable",
            "RequestedRangeNotSatisfiable",
            "BadArgument"
        ];

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

                if (ContainsPermanentErrorCode(message))
                {
                    return Task.FromResult(RequestFailedHandlerResult.Ignore);
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

                        if (IsPermanentErrorCode(innerErrorCode))
                        {
                            return Task.FromResult(RequestFailedHandlerResult.Ignore);
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

                    if (IsPermanentErrorCode(errorCode))
                    {
                        return RequestFailedHandlerResult.Ignore;
                    }
                }

                try
                {
                    var content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (ContainsPermanentErrorCode(content))
                    {
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

        private static bool IsPermanentErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                return false;
            }

            return _permanentErrorCodes.Contains(errorCode) || errorCode.StartsWith(_corruptInputErrorCode);
        }

        private static bool ContainsPermanentErrorCode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return _permanentErrorCodes.Any(text.Contains) || text.Contains(_corruptInputErrorCode);
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
