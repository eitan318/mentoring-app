using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User
{
    /// <summary>Administrator user. No additional profile data beyond the base <see cref="UserModel"/>.</summary>
    public class AdminModel : UserModel
    {
        public AdminModel() : base() { }

        [SetsRequiredMembers]
        public AdminModel(int id, string email, string userName, string nationalId)
            : base(id, email, userName, nationalId) { }
    }
}
