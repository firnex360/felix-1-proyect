using Microsoft.EntityFrameworkCore;
using felix1.Logic;

namespace felix1.Logic
{
    public static class AppSession
    {
        public static User CurrentUser { get; set; }
    }
}