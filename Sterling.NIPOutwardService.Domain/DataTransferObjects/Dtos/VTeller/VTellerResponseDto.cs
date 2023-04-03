using System.Text.Json.Serialization;

namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.VTeller;

public class VTellerResponseDto 
{
    public string Respreturnedcode1 { get; set; }
    public string error_text { get; set; }
    public string Prin_Rsp { get; set; }
    public string Fee_Rsp { get; set; }
    public string Vat_Rsp { get; set; }
}

public class VtellerAPIResponseDto
{
    [JsonPropertyName("Content")]
    public string? Content { get; set; }

    [JsonPropertyName("Error")]
    public VtellerResponseError? Error { get; set; }

    [JsonPropertyName("HasError")]
    public bool HasError { get; set; }

    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("RequestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("IsSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("RequestTime")]
    public DateTime RequestTime { get; set; }

    [JsonPropertyName("ResponseTime")]
    public DateTime ResponseTime { get; set; }
}

public class VtellerResponseError
{
    public int Code { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
}