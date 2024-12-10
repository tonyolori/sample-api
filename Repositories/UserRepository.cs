﻿using MongoDB.Driver;
using SampleAPI.Entities;
using SampleAPI.Models;
using SampleAPI.Persistence;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace SampleAPI.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<ApiKey> _apiKeys;

        public UserRepository(MongoDbContext dbContext)
        {
            _users = dbContext.GetCollection<User>("Users");
            _apiKeys = dbContext.GetCollection<ApiKey>("ApiKeys");
        }

        public async Task<Result> CreateUser(UserDto user)
        {
            var filter = Builders<User>.Filter.Eq(i => i.Email, user.Email);
            var userExist = await _users.Find(filter).AnyAsync();
            if (userExist)
            {
                return Result.Failure("User already exists");
            }
            var entity = new User
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            await _users.InsertOneAsync(entity);
            var key = await GenerateApiKeyAsync(entity.UserId);
            return Result.Success("Created successfully",key);
        }

        public async Task<Result> GetUser(string email)
        {
            var filter = Builders<User>.Filter.Eq(i => i.Email, email);
            var user = await _users.Find(filter).FirstOrDefaultAsync();
            if (user == null) return Result.Failure("Invalid user");
            var keyFilter = Builders<ApiKey>.Filter.Eq(i => i.UserId, user.UserId);
            var key = await _apiKeys.Find(keyFilter).FirstOrDefaultAsync();
            return Result.Success("Success", key.Key);
        }
        public async Task<string> GenerateApiKeyAsync(Guid userId)
        {
            ApiKey apiKey = new()
            {
                Key = GenerateRandomKey(),
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                UserId = userId
            };

            await _apiKeys.InsertOneAsync(apiKey);

            return apiKey.Key;
        }

        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            var apiKeyEntity = await _apiKeys.FindAsync(x => x.Key == apiKey && x.IsActive);

            return apiKeyEntity != null;
        }

        private static string GenerateRandomKey(int length = 16)
        {
            if (length <= 0) throw new ArgumentException("Length must be a positive integer", nameof(length));

            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[length];
            rng.GetBytes(keyBytes);

            // Convert to a Base64 string
            var apiKey = Convert.ToBase64String(keyBytes);

            // Ensure it is URL-safe
            apiKey = apiKey.Replace('+', '-').Replace('/', '_').TrimEnd('=');

            return apiKey;
        }
        public async Task RevokeApiKeyAsync(string apiKey)
        {
            await _apiKeys.FindOneAndDeleteAsync(x => x.Key == apiKey && x.IsActive);
        }

    }
}