using System.Collections.Generic;

namespace kanzeed.ApplicationData
{
    public static class AppData
    {
        public static UserData CurrentUser { get; set; }

        public static Dictionary<int, int> CurrentCart { get; set; }
            = new Dictionary<int, int>();
    }

    public class UserData
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsEmployee { get; set; }
    }
}
