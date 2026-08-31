using System.Collections.Generic;

namespace UserManagementMvc.ViewModels
{
    public class MessengerViewModel
    {
        public List<UserSearchViewModel> Users { get; set; }
            = new List<UserSearchViewModel>();
    }

    public class UserSearchViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string UserName { get; set; } = "";

        public string? Department { get; set; }

        public string? LastSeen { get; set; }

        // Latest conversation information
        public string? LastMessage { get; set; }

        public string? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }
    }
}