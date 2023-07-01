namespace Sterling.NIPOutwardService.Domain.Config.Implementations;

public class KafkaDebitProducerConfig 
{
    public string OutwardDebitTopic { get; set; }
    public ClientConfig ClientConfig { get; set; }
}