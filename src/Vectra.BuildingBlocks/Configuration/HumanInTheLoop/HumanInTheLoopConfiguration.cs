namespace Vectra.BuildingBlocks.Configuration.HumanInTheLoop;

public class HumanInTheLoopConfiguration
{
    public bool? Enabled { get; set; } = true;
    public double? Threshold { get; set; } = 0.8;
}
