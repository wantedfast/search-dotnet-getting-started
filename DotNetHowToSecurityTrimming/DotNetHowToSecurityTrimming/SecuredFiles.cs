using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;

// The JsonPropertyName attribute is defined in the Azure Search .NET SDK.
// Here it used to ensure that Pascal-case property names in the model class are mapped to camel-case
// field names in the index.

public partial class SecuredFiles
{
    [JsonPropertyName("fieldId")]
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string FileId { get; set; }

    [JsonPropertyName("name")]
    [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string Name { get; set; }

    [JsonPropertyName("groupIds")]
    [SimpleField(IsFilterable = true)]
    public string[] GroupIds { get; set; }
}