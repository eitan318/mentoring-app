
using MentoringApp.Model.User;

namespace MentoringApp.ViewModel.Store;

/// <summary>Singleton holder of the currently logged-in <see cref="UserModel"/>, shared across view models.</summary>
public class UserStore
{
    public UserModel? User { get; set; }
}
