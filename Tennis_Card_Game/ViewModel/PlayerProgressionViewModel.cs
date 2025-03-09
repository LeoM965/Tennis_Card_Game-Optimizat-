using Tennis_Card_Game.Models;
public class PlayerProgressionViewModel
{
    public Player Player { get; set; }
    public int CurrentLevelXpRequirement { get; set; }
    public int XpToNextLevel { get; set; }
    public double LevelProgressPercentage { get; set; }
    public int AvailableSkillPoints { get; set; }
    public int SkillUpgradeCost { get; set; }
    public List<PlayingStyle> PossiblePlayingStyles { get; set; }
    public List<SpecialAbility> PossibleSpecialAbilities { get; set; }
    public List<TrainingModule> RecommendedTrainingModules { get; set; }
}
public class TrainingModule
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int EnergyRequired { get; set; }
    public int ExperienceReward { get; set; }
}

