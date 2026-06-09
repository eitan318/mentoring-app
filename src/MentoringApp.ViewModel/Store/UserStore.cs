
using MentoringApp.Model.User;

namespace MentoringApp.ViewModel.Store;

/// <summary>
/// Singleton holder of the currently logged-in <see cref="UserModel"/>, shared across view models.
/// Raises <see cref="UserChanged"/> whenever the reference is replaced, so dependent view models
/// (e.g. the top-bar avatar in the authenticated shell) can refresh after a profile update.
/// </summary>
public class UserStore
{
    private UserModel? _user;

    public UserModel? User
    {
        get => _user;
        set
        {
            _user = value;
            UserChanged?.Invoke();
        }
    }

    /// <summary>Raised whenever <see cref="User"/> is assigned (login, logout, profile refresh).</summary>
    public event Action? UserChanged;
}
