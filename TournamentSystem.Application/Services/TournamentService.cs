using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ISerieRepository _serieRepository;
        private readonly IUserRepository _userRepository;

        public TournamentService(ITournamentRepository tournamentRepository, ISerieRepository serieRepository, IUserRepository userRepository)
        {
            _tournamentRepository = tournamentRepository;
            _serieRepository = serieRepository;
            _userRepository = userRepository;
        }

        public async Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId)
        {
            var seriesExist = await _serieRepository.DoAllSeriesExistAsync(dto.SeriesIds.ToArray());

            if (!seriesExist)
                throw new NotFoundException("One or more series do not exist");

            var tournament = new Tournament
            {
                Name = dto.Name,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                CountryCode = dto.CountryCode,
                OrganizerId = oganizerId
            };
            var tournamentId = await _tournamentRepository.CreateTournamentAsync(tournament);

            await _tournamentRepository.AddSeriesToTournamentAsync(tournamentId, dto.SeriesIds.ToArray());

            return tournamentId;
        }

        public async Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto)
        {
            var existingTournament = await _tournamentRepository.GetTournamentByIdAsync(dto.TournamentId);

            if (existingTournament is null)
                throw new NotFoundException("Tournament not found");

            existingTournament.Name = dto.Name ?? existingTournament.Name;
            existingTournament.StartDateTime = dto.StartDateTime ?? existingTournament.StartDateTime;
            existingTournament.EndDateTime = dto.EndDateTime ?? existingTournament.EndDateTime;
            existingTournament.CountryCode = dto.CountryCode ?? existingTournament.CountryCode;
            existingTournament.Winner = dto.Winner ?? existingTournament.Winner;
            existingTournament.OrganizerId = dto.OrganizerId ?? existingTournament.OrganizerId;

            return await _tournamentRepository.UpdateTournamentAsync(existingTournament);
        }

        public async Task<BaseTournamentDto> GetTournamentByIdAsync(int tournamentId, UserRole userRole)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (userRole == UserRole.Administrator || userRole == UserRole.Organizer)
            {
                return new TournamentAdminDto
                {
                    TournamentId = tournament.TournamentId,
                    Name = tournament.Name,
                    StartDateTime = tournament.StartDateTime,
                    EndDateTime = tournament.EndDateTime,
                    CountryCode = tournament.CountryCode,
                    Winner = tournament.Winner,
                    Series = tournament.Series,
                    Players = tournament.Players.ConvertAll(p => new UserForAdminsDto
                    {
                        UserId = p.UserId,
                        Name = p.Name,
                        Alias = p.Alias,
                        Email = p.Email,
                        AvatarUrl = p.AvatarUrl,
                        CountryCode = p.CountryCode,
                        Role = p.Role
                    }),
                    Judges = tournament.Judges.ConvertAll(j => new UserForAdminsDto
                    {
                        UserId = j.UserId,
                        Name = j.Name,
                        Alias = j.Alias,
                        Email = j.Email,
                        AvatarUrl = j.AvatarUrl,
                        CountryCode = j.CountryCode,
                        Role = j.Role
                    }),
                    Organizer = new UserForAdminsDto
                    {
                        UserId = tournament.Organizer.UserId,
                        Name = tournament.Organizer.Name,
                        Alias = tournament.Organizer.Alias,
                        Email = tournament.Organizer.Email,
                        AvatarUrl = tournament.Organizer.AvatarUrl,
                        CountryCode = tournament.Organizer.CountryCode,
                        Role = tournament.Organizer.Role
                    }
                };
            }

            return new TournamentPublicDto
            {
                TournamentId = tournament.TournamentId,
                Name = tournament.Name,
                StartDateTime = tournament.StartDateTime,
                EndDateTime = tournament.EndDateTime,
                CountryCode = tournament.CountryCode,
                Winner = tournament.Winner,
                Series = tournament.Series,
                Players = tournament.Players.ConvertAll(p => new BaseUserDto
                {
                    Alias = p.Alias,
                    AvatarUrl = p.AvatarUrl,
                    CountryCode = p.CountryCode
                }),
                Judges = tournament.Judges.ConvertAll(j => new BaseUserDto
                {
                    Alias = j.Alias,
                    AvatarUrl = j.AvatarUrl,
                    CountryCode = j.CountryCode,
                }),
                Organizer = new BaseUserDto
                {
                    Alias = tournament.Organizer.Alias,
                    AvatarUrl = tournament.Organizer.AvatarUrl,
                    CountryCode = tournament.Organizer.CountryCode,
                }
            };
        }

        public async Task<bool> RegisterPlayerAsync(int tournamentId, int playerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.Phase != Domain.Enums.TournamentPhase.Registration)
                throw new ValidationException("The tournament is not in the registration phase.");

            if (tournament.Players.Exists(p => p.UserId == playerId))
                throw new ValidationException("The player is already registered for the tournament.");

            var maxPlayers = CalculateMaxPlayers(tournament);
            //var currentPlayers = await _tournamentRepository.GetPlayerCountAsync(tournamentId);
            var currentPlayers = tournament.Players.Count;

            if (currentPlayers >= maxPlayers)
                throw new ValidationException("The tournament has reached its maximum capacity of players.");

            return await _tournamentRepository.RegisterPlayerAsync(tournamentId, playerId);
        }

        public async Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId, int organizerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.OrganizerId != organizerId)
                throw new ValidationException("The organizer is not allowed to assign judges to this tournament.");

            var judge = await _userRepository.GetUserByIdAsync(judgeId);

            if (judge is null || judge.Role != Domain.Enums.UserRole.Judge)
                throw new NotFoundException("Judge not found");

            if (tournament.Judges.Exists(j => j.UserId == judgeId))
                throw new ValidationException("The judge is already assigned to the tournament.");

            return await _tournamentRepository.AssignJudgeToTournamentAsync(tournamentId, judgeId);
        }

        private int CalculateMaxPlayers(Tournament tournament)
        {
            const int gameDurationInMinutes = 30;

            var startHour = tournament.StartDateTime.TimeOfDay;
            var endHour = tournament.EndDateTime.TimeOfDay;
            var totalDays = tournament.EndDateTime.Day - tournament.StartDateTime.Day + 1;
            var tournamentTotalMinutes = (endHour - startHour).TotalMinutes * totalDays;
            var maxGames = tournamentTotalMinutes / gameDurationInMinutes;
            var possiblePlayers = maxGames + 1;

            var maxPlayers = 1;

            while ((maxPlayers * 2) + 1 <= possiblePlayers)
            {
                maxPlayers *= 2;
            }

            return maxPlayers + 1;
        }
    }
}
