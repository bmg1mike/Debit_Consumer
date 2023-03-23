namespace Sterling.NIPOutwardService.Domain.Config.Implementations;

public class KafkaDebitConsumerConfig 
{
    public string OutwardDebitTopic { get; set; }
    public ConsumerConfig ConsumerConfig { get; set; }
}