using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public abstract class BaseModel : ObservableObject
    {
        public int Id { get; set; }
    }
}
