using MongoDB.Bson.Serialization.Attributes;

namespace Base.API.Models
{
    public abstract class Session
    {
        [BsonElement("user_id")] 
        public string UserId { get; set; }
        
        public string Jwt { get; set; }
    }
}