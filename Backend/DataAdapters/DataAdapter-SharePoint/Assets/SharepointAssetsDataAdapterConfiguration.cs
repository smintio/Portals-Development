using System;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.AllowedValuesProviders;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Configurations;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Interfaces.Ai;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Prefab;
using SmintIo.Portals.SDK.Core.Configuration.Annotations;
using SmintIo.Portals.SDK.Core.Models.MetadataAttributes;
using static SmintIo.Portals.DataAdapterSDK.DataAdapters.Prefab.IAlwaysOnIntegrationLayerDataAdapterConfiguration;

namespace SmintIo.Portals.DataAdapter.SharePoint.Assets
{
    [Serializable]
    public class SharepointAssetsDataAdapterConfiguration : IAlwaysOnIntegrationLayerDataAdapterConfiguration, IOutputFormatDataAdapterConfiguration, IPreserveMetadataDataAdapterConfiguration
    {
        public MetadataAttributeModel[] SmintIoIntegrationLayerSearchIndexMetadataAttributeList { get; set; }

        public MetadataAttributeModel[] SmintIoIntegrationLayerSearchIndexSortMetadataAttributeList { get; set; }

        public MetadataAttributeModel[] SmintIoIntegrationLayerSearchIndexSortFallbackMetadataAttributeList { get; set; }

        public MetadataAttributeModel SmintIoIntegrationLayerSearchIndexOverrideNameAttribute { get; set; }

        public SmintIoDataSearchIndexFileType[] SmintIoSearchIndexFileTypeAllowList { get; set; }

        public SmintIoDataSearchIndexFileType[] SmintIoSearchIndexFileTypeDenyList { get; set; }

        public string[] SmintIoSearchIndexIndexedLanguages { get; set; }

        public MetadataAttributeModel[] SmintIoSearchIndexOnlyIndexAssetsWithAttributes { get; set; }

        public string[] SmintIoSearchIndexOnlyIndexAssetsWithAttributeValues { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] OutputFormatIdAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] OutputFormatIdImagesAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] OutputFormatIdVideosAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] OutputFormatIdAudioFilesAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] OutputFormatIdDocumentsAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] OutputFormatIdOtherFileTypesAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] HiResOutputFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] HiResOutputImagesFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] HiResOutputVideosFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] HiResOutputAudioFilesFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] HiResOutputDocumentsFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProviderBaseImpl))]
        public string[] HiResOutputOtherFileTypesFormatIdList { get; set; }

        public MetadataAttributeModel[] SmintIoPreserveMetadataAttributes { get; set; }

        public bool SmintIoDisableMetadataSecurity { get; set; }

        public SmintIoDataSearchIndexFulltextSearchType SmintIoSearchIndexFulltextSearchType { get; set; }

        public bool SmintIoSearchIndexEnableAutoCompletion { get; set; }

        public bool EnableFolderNavigation { get; set; }

        public bool SearchSubFoldersEnabledByDefault { get; set; }

        public bool SmintIoSearchIndexEnableNaturalLanguageSearch { get; set; }

        public int SmintIoSearchIndexNaturalLanguageSearchSimilarity { get; set; }

        public bool SmintIoSearchIndexEnableReverseImageSearch { get; set; }

        public int SmintIoSearchIndexReverseImageSearchSimilarity { get; set; }

        public SmintIoDataSearchIndexAiSearchMergingBehaviorType SmintIoSearchIndexNaturalLanguageSearchAiSearchMergingBehaviorType { get; set; }

        public SmintIoDataSearchIndexAiSearchMergingBehaviorType SmintIoSearchIndexReverseImageSearchAiSearchMergingBehaviorType { get; set; }

        public SmintIoDataSearchIndexAiSearchMergingBehaviorType SmintIoSearchIndexAiSearchMergingBehaviorType { get; set; }

        public ISmintedAi SmintedAiDataAdapter { get; set; }
    }
}