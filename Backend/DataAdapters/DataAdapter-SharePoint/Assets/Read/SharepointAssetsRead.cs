using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmintIo.Portals.DataAdapter.SharePoint.Assets.Common;
using SmintIo.Portals.DataAdapter.SharePoint.Assets.Models;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Impl;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Interfaces.Assets;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Interfaces.Assets.Models;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Interfaces.Assets.Parameters;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Interfaces.Assets.Results;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Progress;
using SmintIo.Portals.SDK.Core.Http.Prefab.Models;
using SmintIo.Portals.SDK.Core.Models.Metamodel.Data;
using SmintIo.Portals.SDK.Core.Rest.Prefab.Exceptions;

namespace SmintIo.Portals.DataAdapter.SharePoint.Assets
{
    public partial class SharepointAssetsDataAdapter : AssetsDataAdapterBaseImpl, IAssetsIntegrationLayerApiProvider
    {
        private static readonly Regex _downloadUrlRegex = new Regex("downloadUrl\":\"(.*?)\"", RegexOptions.IgnoreCase);
        private static readonly Regex _spItemUrlRegex = new Regex("g_fileInfo =(.*?); var g_webApplicationUrls", RegexOptions.IgnoreCase);

        public override bool HasPlaybackPreviewOutputFormat(AssetDataObject assetDataObject)
        {
            return false;
        }

        public async override Task<AssetDownloadStreamModel> GetAssetThumbnailDownloadStreamAsync(
            AssetIdentifier assetId,
            ContentTypeEnumDataObject contentType,
            AssetThumbnailSize assetThumbnailSize,
            string thumbnailSpec,
            long? maxFileSizeBytes)
        {
            if (assetId == null)
            {
                throw new ArgumentNullException(nameof(assetId));
            }

            var useIntegrationLayerProvider = _smintIoIntegrationLayerProvider != null;

            var isStreaming = assetThumbnailSize == AssetThumbnailSize.PlaybackStreaming;

            if (isStreaming)
            {
                // we get the streaming object instead

                useIntegrationLayerProvider = false;
            }

            if (useIntegrationLayerProvider)
            {
                var assetDownloadUrlModel = await _smintIoIntegrationLayerProvider.GetAssetThumbnailDownloadUrlAsync(Context, assetId, contentType, assetThumbnailSize).ConfigureAwait(false);

                if (assetDownloadUrlModel == null || string.IsNullOrEmpty(assetDownloadUrlModel.Url))
                {
                    return null;
                }

                var streamResponse = await _sharepointClient.GetHttpClientStreamResponseWithoutBackoffAsync(assetDownloadUrlModel.Url, _sharepointClient.DefaultRequestFailedHandler, accessToken: null, hint: $"Get asset thumbnail download stream ({assetId}, {contentType}, {assetThumbnailSize}, {thumbnailSpec})", cancelRequestDelay: TimeSpan.FromSeconds(5), maxFileSizeBytes: maxFileSizeBytes).ConfigureAwait(false);

                if (streamResponse == null)
                {
                    return null;
                }

                return new AssetDownloadStreamModel(
                    streamResponse.FileName,
                    streamResponse.FileSizeInBytes,
                    streamResponse.MediaType,
                    streamResponse.Stream);
            }

            if (string.Equals(thumbnailSpec, SharepointContentConverter.ConvertThumbnailSpec))
            {
                var streamResponse = await _sharepointClient.GetDriveItemPdfContentAsync(assetId.UnscopedId, maxFileSizeBytes).ConfigureAwait(false);

                if (streamResponse == null)
                {
                    // not supported

                    return null;
                }

                var fileName = streamResponse.FileName;

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Guid.NewGuid().ToString();
                }

                return new AssetDownloadStreamModel(
                    fileName,
                    streamResponse.FileSizeInBytes,
                    mediaType: streamResponse.MediaType,
                    streamResponse.Stream);
            }

            if (isStreaming ||
                assetThumbnailSize == AssetThumbnailSize.PlaybackLarge ||
                assetThumbnailSize == AssetThumbnailSize.PlaybackSmall)
            {
                var streamResponse = await GetPreviewContentAsync(assetId, contentType, isStreaming, maxFileSizeBytes).ConfigureAwait(false);

                if (streamResponse == null)
                {
                    return null;
                }

                var fileName = streamResponse.FileName;

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Guid.NewGuid().ToString();
                }

                return new AssetDownloadStreamModel(
                    fileName,
                    streamResponse.FileSizeInBytes,
                    mediaType: streamResponse.MediaType,
                    streamResponse.Stream);
            }

            var isPdfPreview = assetThumbnailSize == AssetThumbnailSize.PdfPreview;

            if (isPdfPreview)
            {
                var streamResponse = await _sharepointClient.GetDriveItemContentAsync(assetId.UnscopedId, maxFileSizeBytes).ConfigureAwait(false);

                if (streamResponse == null)
                {
                    return null;
                }

                return new AssetDownloadStreamModel(
                    streamResponse.FileName,
                    streamResponse.FileSizeInBytes,
                    streamResponse.MediaType,
                    streamResponse.Stream);
            }
            else
            {
                var driveItem = await _sharepointClient.GetDriveItemAsync(assetId.UnscopedId).ConfigureAwait(false);

                if (driveItem == null)
                {
                    throw new ExternalDependencyException(ExternalDependencyStatusEnum.AssetNotFound, "The asset was not found", assetId.UnscopedId);
                }

                var thumbnailSet = driveItem.Thumbnails.FirstOrDefault();

                if (thumbnailSet == null)
                {
                    _logger.LogWarning($"Thumbnail set NULL for asset ID {assetId.UnscopedId}, drive item {driveItem.Id} - no thumbnails present, ignoring and returning NULL");

                    // no thumbnails available

                    return null;
                }

                string thumbnailUrl = null;
                var computePreview = false;

                switch (assetThumbnailSize)
                {
                    case AssetThumbnailSize.Preview:
                        computePreview = true;
                        break;

                    case AssetThumbnailSize.Large:
                        thumbnailUrl = thumbnailSet.Large?.Url;
                        break;

                    case AssetThumbnailSize.Small:
                        thumbnailUrl = thumbnailSet.Small?.Url;
                        break;

                    default:
                        thumbnailUrl = thumbnailSet.Medium?.Url;
                        break;
                }

                if (!computePreview && string.IsNullOrEmpty(thumbnailUrl))
                {
                    return null;
                }

                StreamResponse streamResponse;

                if (computePreview)
                {
                    streamResponse = await _sharepointClient.GetDriveItemThumbnailContentAsync(assetId.UnscopedId, thumbnailSet.Id, size: "c1600x1600", maxFileSizeBytes).ConfigureAwait(false);
                }
                else
                {
                    if (string.IsNullOrEmpty(thumbnailUrl))
                    {
                        _logger.LogWarning($"Thumbnail is not present for asset {assetId.UnscopedId}");

                        return null;
                    }

                    streamResponse = await _sharepointClient.GetHttpClientStreamResponseWithBackoffAsync(thumbnailUrl, _sharepointClient.DefaultRequestFailedHandler, accessToken: null, hint: $"Get asset thumbnail download stream ({assetId}, {contentType}, {assetThumbnailSize}, {thumbnailSpec})", maxFileSizeBytes: maxFileSizeBytes).ConfigureAwait(false);
                }

                if (streamResponse == null)
                {
                    return null;
                }

                var fileName = streamResponse.FileName;

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Guid.NewGuid().ToString();
                }

                return new AssetDownloadStreamModel(
                    fileName,
                    streamResponse.FileSizeInBytes,
                    mediaType: streamResponse.MediaType,
                    streamResponse.Stream);
            }
        }

        private async Task<StreamResponse> GetPreviewContentAsync(AssetIdentifier assetId, ContentTypeEnumDataObject contentType, bool isStreaming, long? maxFileSizeBytes)
        {
            var useMediaPresentation = isStreaming && contentType.Id == ContentTypeEnumDataObject.Video.Id;

            var url = await GetAssetItemPreviewUrlAsync(assetId.UnscopedId, useMediaPresentation).ConfigureAwait(false);

            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            return await _sharepointClient.GetHttpClientStreamResponseWithBackoffAsync(url, _sharepointClient.DefaultRequestFailedHandler, accessToken: null, hint: $"Get preview content ({assetId}, {contentType})", maxFileSizeBytes: maxFileSizeBytes).ConfigureAwait(false);
        }

        private async Task<string> GetAssetItemPreviewUrlAsync(string assetId, bool useMediaPresentation)
        {
            var itemPreviewInfo = await _sharepointClient.GetDriveItemPreviewInfoAsync(assetId).ConfigureAwait(false);

            var hasItemPreviewInfo = !string.IsNullOrEmpty(itemPreviewInfo?.GetUrl);

            if (!hasItemPreviewInfo)
            {
                return null;
            }

            var requestHeaders = new Dictionary<string, string>
            {
                { "user-agent", "Smint.io/1.0.0" }
            };

            var urlContent = await _sharepointClient.GetHttpClientStreamResponseAsStringWithBackoffAsync(itemPreviewInfo.GetUrl, _sharepointClient.DefaultRequestFailedHandler, accessToken: null, hint: $"Get asset item preview URL ({assetId}, {useMediaPresentation})", isGet: true, requestHeaders).ConfigureAwait(false);

            if (string.IsNullOrEmpty(urlContent))
            {
                return null;
            }

            if (useMediaPresentation)
            {
                var mediaPresentationUrl = GetMediaPresentationUrl(urlContent);

                if (!string.IsNullOrEmpty(mediaPresentationUrl))
                {
                    return mediaPresentationUrl;
                }
            }

            var downloadUrlMatch = _downloadUrlRegex.Match(urlContent);

            if (string.IsNullOrEmpty(downloadUrlMatch.Value) || downloadUrlMatch.Groups.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Could not determine item preview url for asset {assetId}. Revisit what Microsoft has changed");
            }

            // the match is a raw JSON string value, SharePoint may escape characters in it, e.g. & as &

            var url = JsonConvert.DeserializeObject<string>($"\"{downloadUrlMatch.Groups[1].Value}\"");

            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            return url;
        }

        private string GetMediaPresentationUrl(string rawHtml)
        {
            try
            {
                var sharepointItemPreviewText = _spItemUrlRegex.Match(rawHtml).Groups[1].Value.Trim();

                if (string.IsNullOrEmpty(sharepointItemPreviewText))
                {
                    return null;
                }

                var sharepointItemPreview = JsonConvert.DeserializeObject<SharepointItemPreview>(sharepointItemPreviewText);

                if (string.IsNullOrEmpty(sharepointItemPreview.MediaServiceFastMetadata))
                {
                    return null;
                }

                var mediaServiceFastMetadata = JsonConvert.DeserializeObject<MediaServiceFastMetadata>(sharepointItemPreview.MediaServiceFastMetadata);

                if (mediaServiceFastMetadata == null)
                {
                    return null;
                }

                sharepointItemPreview.ParsedMediaServiceFastMetadata = mediaServiceFastMetadata;

                return sharepointItemPreview.ToString();
            }
            catch (Exception ex)
            {
                // We will fall back to piped file stream.

                _logger.LogError(ex, $"SharePoint item preview manifest parsing failed. Revisit what Microsoft has changed");

                return null;
            }
        }

        public async override Task<AssetDownloadStreamModel> GetAssetDownloadStreamAsync(AssetIdentifier assetId, AssetDownloadItemMappingModel downloadItemMapping, long? maxFileSizeBytes)
        {
            if (assetId == null)
            {
                throw new ArgumentNullException(nameof(assetId));
            }

            if (downloadItemMapping == null)
            {
                throw new ArgumentNullException(nameof(downloadItemMapping));
            }

            StreamResponse streamResponse;

            if (Enum.TryParse<AssetThumbnailSize>(downloadItemMapping?.ItemId, out var assetThumbnailSize))
            {
                var assetDownloadUrlModel = await _smintIoIntegrationLayerProvider.GetAssetThumbnailDownloadUrlAsync(Context, assetId, contentType: null, assetThumbnailSize).ConfigureAwait(false);

                if (assetDownloadUrlModel == null || string.IsNullOrEmpty(assetDownloadUrlModel.Url))
                {
                    _logger.LogWarning($"Asset download URL is empty for asset {assetId.UnscopedId}");

                    return null;
                }

                streamResponse = await _sharepointClient.GetHttpClientStreamResponseWithBackoffAsync(assetDownloadUrlModel.Url, _sharepointClient.DefaultRequestFailedHandler, accessToken: null, hint: $"Get asset download stream ({assetId})", maxFileSizeBytes: maxFileSizeBytes).ConfigureAwait(false);
            }
            else
            {
                streamResponse = await _sharepointClient.GetDriveItemContentAsync(assetId.UnscopedId, maxFileSizeBytes).ConfigureAwait(false);
            }

            if (streamResponse == null)
            {
                return null;
            }

            return new AssetDownloadStreamModel(
                streamResponse.FileName,
                streamResponse.FileSizeInBytes,
                streamResponse.MediaType,
                streamResponse.Stream);
        }

        public override Task<GetAssetResult> GetAssetAsync(GetAssetParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (_smintIoIntegrationLayerProvider == null)
            {
                throw new NotImplementedException();
            }

            return _smintIoIntegrationLayerProvider.GetAssetAsync(Context, parameters);
        }

        public async override Task<GetAssetsResult> GetAssetsAsync(GetAssetsParameters parameters, IProgressMonitor progressMonitor)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (_smintIoIntegrationLayerProvider == null)
            {
                throw new NotImplementedException();
            }

            var assets = await _smintIoIntegrationLayerProvider.GetAssetsAsync(Context, parameters).ConfigureAwait(false);

            if (progressMonitor != null)
            {
                await progressMonitor.FinishedAsync(null).ConfigureAwait(false);
            }

            return assets;
        }

        public override Task<FolderDownloadStreamModel> GetFolderThumbnailDownloadStreamAsync(FolderIdentifier assetId, FolderThumbnailSize size, string thumbnailSpec)
        {
            throw new NotImplementedException();
        }
    }
}