namespace Sterling.NIPOutwardService.Domain.Config.Implementations;

public class KafkaSendToNIBSSProducerConfig 
{
    public string OutwardSendToNIBSSTopic { get; set; }
    public ClientConfig ClientConfig { get; set; }
}

public class KafkaImalSendToNIBSSProducerConfig : KafkaSendToNIBSSProducerConfig
{
}