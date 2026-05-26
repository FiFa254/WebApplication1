using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class PermissionViewModel
    {
        public List<Permission> Permissions { get; set; } = new List<Permission>();
        public string[] SelectedPermissions { get; set; } = new string[0];
    }
}
