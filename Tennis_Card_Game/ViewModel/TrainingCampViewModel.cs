using Tennis_Card_Game.Models;
using Tennis_Card_Game.Services;

public class TrainingCampViewModel
{
    public Player Player { get; set; }
    public List<TrainingModule> AvailableTrainingModules { get; set; }
    public double TrainingBonus { get; set; }
    public TrainingService TrainingService { get; set; }
}