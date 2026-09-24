using System;
using SmintIo.Portals.DataAdapter.Picturepark.Assets.AllowedValues;
using SmintIo.Portals.DataAdapter.Picturepark.Resources;
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Configurations;
using SmintIo.Portals.DataAdapterSDK.Resources;
using SmintIo.Portals.SDK.Core.Configuration.Annotations;
using SmintIo.Portals.SDK.Core.Models.MetadataAttributes;

namespace SmintIo.Portals.DataAdapter.Picturepark.Assets
{
    [Serializable]
    [FormGroupDeclaration("data_mapping")]
    [FormGroupDisplayName("data_mapping", translationKey: nameof(Resources.ConfigurationMessages.da_assets_data_mapping_form_group_display_name))]
    public class PictureparkAssetsDataAdapterConfiguration : IOutputFormatDataAdapterConfiguration, IPreserveMetadataDataAdapterConfiguration
    {
        /// <summary>
        /// Returns the default page size.
        /// </summary>
        [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_default_page_size_display_name))]
        [Description(translationKey: nameof(Resources.ConfigurationMessages.da_assets_default_page_size_description))]
        [MinValue(1)]
        [MaxValue(500)]
        [DefaultValue(30)]
        [FormItemVisibility(Visibility = FormItemVisibilityEnum.Advanced)]
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// Multi selection item count.
        /// </summary>
        [DisplayName(translationKey: nameof(DataAdapterSDK.Resources.ConfigurationMessages.da_multi_select_item_count_display_name))]
        [Description(translationKey: nameof(DataAdapterSDK.Resources.ConfigurationMessages.da_multi_select_item_count_description))]
        [MinValue(5)]
        [MaxValue(30)]
        [DefaultValue(15)]
        [FormItemVisibility(Visibility = FormItemVisibilityEnum.Advanced)]
        public int MultiSelectItemCount { get; set; }

        public enum PictureparkDisplayPattern
        {
            [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_picturepark_display_pattern_list_display_name))]
            List = 1,

            [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_picturepark_display_pattern_name_display_name))]
            Name
        }

        [DefaultValue((int)PictureparkDisplayPattern.Name)]
        [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_gallery_title_display_pattern_display_name))]
        [Description(translationKey: nameof(Resources.ConfigurationMessages.da_assets_gallery_title_display_pattern_description))]
        [FormItemVisibility(Visibility = FormItemVisibilityEnum.Advanced)]
        [FormGroup("data_mapping")]
        public PictureparkDisplayPattern GalleryTitleDisplayPattern { get; set; }

        /// <summary>
        /// Custom field for enum resolution.
        /// </summary>
        [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_list_name_attribute_display_name))]
        [Description(translationKey: nameof(Resources.ConfigurationMessages.da_assets_list_name_attribute_description))]
        [FormItemVisibility(Visibility = FormItemVisibilityEnum.Expert)]
        [FormGroup("data_mapping")]
        public string ListNameAttribute { get; set; }

        /// <summary>
        /// Custom field for enum resolution (fallback).
        /// </summary>
        [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_list_name_attribute_2_display_name))]
        [Description(translationKey: nameof(Resources.ConfigurationMessages.da_assets_list_name_attribute_2_display_name))]
        [FormItemVisibility(Visibility = FormItemVisibilityEnum.Expert)]
        [FormGroup("data_mapping")]
        public string ListNameAttribute2 { get; set; }

        [DisplayName(translationKey: nameof(Resources.ConfigurationMessages.da_assets_resolve_list_data_attributes_display_name))]
        [Description(translationKey: nameof(Resources.ConfigurationMessages.da_assets_resolve_list_data_attributes_description))]
        [FormItemVisibility(Visibility = FormItemVisibilityEnum.Expert)]
        [FormGroup("data_mapping")]
        public string[] ResolveListDataAttributes { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] OutputFormatIdAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] OutputFormatIdImagesAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] OutputFormatIdVideosAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] OutputFormatIdAudioFilesAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] OutputFormatIdDocumentsAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] OutputFormatIdOtherFileTypesAllowList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] HiResOutputFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] HiResOutputImagesFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] HiResOutputVideosFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] HiResOutputAudioFilesFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] HiResOutputDocumentsFormatIdList { get; set; }

        [DynamicAllowedValuesProvider(typeof(OutputFormatAllowedValuesProvider))]
        public string[] HiResOutputOtherFileTypesFormatIdList { get; set; }

        public MetadataAttributeModel[] SmintIoPreserveMetadataAttributes { get; set; }

        public bool SmintIoDisableMetadataSecurity { get; set; }
    }
}