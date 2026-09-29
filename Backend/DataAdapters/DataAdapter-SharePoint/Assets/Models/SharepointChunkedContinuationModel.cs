using Newtonsoft.Json;

namespace SmintIo.Portals.DataAdapter.SharePoint.Assets.Models
{
    /// <summary>
    /// Continuation used while a large SharePoint delta page is processed in chunks.
    /// It points back to the SharePoint continuation that produced the page, together with the last item already processed.
    /// Plain SharePoint continuations (delta or next links) are URLs, so a JSON object tells them apart.
    /// </summary>
    public class SharepointChunkedContinuationModel
    {
        [JsonProperty("v")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// The SharePoint delta link that produced the page being processed.
        /// </summary>
        [JsonProperty("sharepointDeltaLink")]
        public string SharepointDeltaLink { get; set; }

        /// <summary>
        /// The asset ID of the last drive item of the page that was already processed.
        /// </summary>
        [JsonProperty("lastProcessedAssetId")]
        public string LastProcessedAssetId { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static SharepointChunkedContinuationModel TryParse(string continuationUuid)
        {
            if (string.IsNullOrEmpty(continuationUuid) || !continuationUuid.TrimStart().StartsWith("{"))
            {
                return null;
            }

            try
            {
                var chunkedContinuation = JsonConvert.DeserializeObject<SharepointChunkedContinuationModel>(continuationUuid);

                if (string.IsNullOrEmpty(chunkedContinuation?.LastProcessedAssetId))
                {
                    return null;
                }

                return chunkedContinuation;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
