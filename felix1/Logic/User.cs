namespace felix1.Logic
{
    public class User
    {
        public int Id { get; set; } // Auto-generated
        public string? Name { get; set; } = null;
        public string? Username { get; set; } = null;
        public string? Password { get; set; } = null;
        public string? Role { get; set; } = null;
        public bool Available { get; set; } = true;
        public bool Deleted { get; set; } = false;

        public override string ToString()
        {
            return $"User: [Id={Id}, Name={Name}, Username={Username}, Password=*****, Role={Role}, Available={Available}, Deleted={Deleted}]";
        }
    }
}