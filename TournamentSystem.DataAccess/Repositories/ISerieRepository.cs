namespace TournamentSystem.DataAccess.Repositories
{
    public interface ISerieRepository
    {
        Task<bool> DoAllSeriesExistAsync(int[] serieIds);
    }
}