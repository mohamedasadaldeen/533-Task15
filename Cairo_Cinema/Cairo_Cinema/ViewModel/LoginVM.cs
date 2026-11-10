using System.ComponentModel.DataAnnotations;

namespace Cairo_Cinema.ViewModel
{
    public class LoginVM
    {
        public int Id { get; set; }
        public string UserNameOrEmail { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool RememberMe{ get; set; }


    }
}
