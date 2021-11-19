using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Base.API.Models
{
    public class Customer
    {
        [BsonElement("_id")]
        [JsonProperty("_id")]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsActive { get; set; }
        public void UpdateCustomer(string name, string email, bool isActive)
        {
            Name = name;
            Email = email;
            IsActive = isActive;
        }
    }
}