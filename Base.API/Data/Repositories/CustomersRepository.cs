using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base.API.Models;
using Base.API.Models.Responses;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Base.API.Data.Repositories
{
    public class CustomersRepository
    {
        private readonly IMongoCollection<Customer> _customersCollection;

        public CustomersRepository(IMongoClient mongoClient)
        {
            var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConvention, _ => true);
            
            _customersCollection = mongoClient.GetDatabase("base_api").GetCollection<Customer>("customers");
        }
        
        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var filterBuilder = Builders<Customer>.Filter;
            var filter = filterBuilder.Eq(x => x.IsActive, true);

            return await _customersCollection.Find(filter)
                .SortByDescending(p => p.Id)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<Customer> GetCustomerAsync(string email) => await _customersCollection.Find(c => c.Email.Equals(email)).FirstOrDefaultAsync();
        public async Task<CustomerResponse> CreateCustomerAsync(string name, string email)
        {
            try
            {
                var customer = new Customer
                {
                    Name = name,
                    Email = email,
                    IsActive = true,
                    CreateDate = DateTime.UtcNow
                };

                await _customersCollection.InsertOneAsync(customer);
                var newCustomer = await GetCustomerAsync(customer.Email);
                return new CustomerResponse(newCustomer);
            }
            catch (Exception)
            {
               return new CustomerResponse(false, "Não foi possivel cadastrar cliente");
            }
        }

        public async Task UpdateCustomerAsync(string email, Customer customer)
        {
            await _customersCollection.ReplaceOneAsync(c => c.Email.Equals(email), customer);
        }
            
        public async Task<CustomerResponse> DeleteCustomerAsync(string email)
        {
            try
            {
                await _customersCollection.DeleteOneAsync(c => c.Email.Equals(email));

                var deleteCustomer = await _customersCollection.FindAsync(c => c.Email.Equals(email));
                if (deleteCustomer.FirstOrDefault() == null)
                    return new CustomerResponse(true, "Cliente removido com sucesso");
                return new CustomerResponse(false, "Erro ao remover cliente");
            }
            catch (Exception ex)
            {
                return new CustomerResponse(false, ex.ToString());
            }
        }
    }
}