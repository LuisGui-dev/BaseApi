using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base.API.Models;
using Base.API.Models.Responses;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Base.API.Data.Repositories
{
    public class UsersRepository
    {
        private readonly IMongoCollection<Session> _sessionsCollection;
        private readonly IMongoCollection<User> _usersCollection;

        public UsersRepository(IMongoClient mongoClient)
        {
            var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
            
            _usersCollection = mongoClient.GetDatabase("base_api").GetCollection<User>("users");
            _sessionsCollection = mongoClient.GetDatabase("base_api").GetCollection<Session>("sessions");
        }
        
        public async Task<IEnumerable<User>> GetAllUsersAsync() => await _usersCollection.Find(_ => true).ToListAsync();
        public async Task<User> GetUserAsync(string email) => await _usersCollection.Find(u => u.Email.Equals(email)).FirstOrDefaultAsync();
        public async Task<UserResponse> CreateUserAsync(string name, string email, string password)
        {
            try
            {
                var user = new User
                {
                    Name = name,
                    Email = email,
                    HashedPassword = PasswordHash.Hash(password)
                };

                await _usersCollection.InsertOneAsync(user);

                var newUser = await GetUserAsync(user.Email);
                return new UserResponse(newUser);
            }
            catch (Exception ex)
            {
                return ex.Message.StartsWith("MongoError: E11000 erro de chave duplicada ")
                    ? new UserResponse(false, "Já existe um usuário com o email fornecido.")
                    : new UserResponse(false, ex.Message);
            } 
        }
        
        public async Task<UserResponse> LoginUserAsync(User user)
        {
            try
            {
                var storedUser = await GetUserAsync(user.Email);
                if (storedUser == null)
                    return new UserResponse(false, "Usuário não encontrado");
                if (user.HashedPassword != null && user.HashedPassword != storedUser.HashedPassword)
                    return new UserResponse(false, "A senha com hash fornecida não é valida");
                if (user.HashedPassword == null && !PasswordHash.Verify(user.Password, storedUser.HashedPassword))
                    return new UserResponse(false, "A senha fornecida não é válida ");
                
                await _sessionsCollection.UpdateOneAsync(new BsonDocument("user_id", user.Email),
                    Builders<Session>.Update.Set(s => s.UserId, user.Email).Set(s => s.Jwt, user.AuthToken),
                    new UpdateOptions {IsUpsert = true});

                storedUser.AuthToken = user.AuthToken;
                return new UserResponse(true, "Usúario logado com sucesso");
            }
            catch (Exception ex)
            {
                return new UserResponse(false, ex.Message);
            }
        }
        
        public async Task<UserResponse> LogoutUserAsync(string email)
        {
            await _sessionsCollection.DeleteOneAsync(u => u.UserId.Equals(email));
            return new UserResponse(true, "Usuário desconectado");
        }
        
        
        public async Task<Session> GetUserSessionAsync(string email)
        {
            return await _sessionsCollection.Find(u => u.UserId.Equals(email)).FirstOrDefaultAsync();
        }
        
        public async Task<UserResponse> DeleteUserAsync(string email)
        {
            try
            {
                await _usersCollection.DeleteOneAsync(u => u.Email.Equals(email));
                await _sessionsCollection.DeleteOneAsync(u => u.UserId.Equals(email));

                var deletedUser = await _usersCollection.FindAsync(u => u.Email.Equals(email));
                var deletedSession = await _sessionsCollection.FindAsync(u => u.UserId.Equals(email));
                if(deletedUser.FirstOrDefault() == null && deletedSession.FirstOrDefault() == null)
                    return new UserResponse(true, "Usuário deletado");
                return new UserResponse(false, "Erro ao deletar usário");
            }
            catch (Exception ex)
            {
                return new UserResponse(false, ex.ToString());
            }
        }
    }
}