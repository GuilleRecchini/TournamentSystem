﻿using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.DataAccess.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class AuthenticationRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IAuthenticationRepository
    {
        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            const string query = @"
            DELETE FROM RefreshTokens WHERE user_id = @UserId;
            INSERT INTO RefreshTokens (user_id, token, expires, created_at) 
            VALUES (@UserId, @Token, @Expires, @CreatedAt);";

            var parameters = new { refreshToken.UserId, refreshToken.Token, refreshToken.Expires, refreshToken.CreatedAt };

            await using var connection = CreateConnection();
            await connection.ExecuteAsync(query, parameters);
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string token)
        {
            const string query = @"
            SELECT *
            FROM RefreshTokens
            WHERE token = @Token AND expires > NOW()";

            await using var connection = CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<RefreshToken>(query, new { Token = token });
        }

        public async Task DeleteRefreshTokenAsync(string token)
        {
            const string query = @"
            DELETE FROM RefreshTokens
            WHERE token = @Token";

            await using var connection = CreateConnection();
            await connection.ExecuteAsync(query, new { Token = token });
        }
    }
}
