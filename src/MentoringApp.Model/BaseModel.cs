using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    /// <summary>
    /// Base class for every domain entity. Provides the database primary key <see cref="Id"/>,
    /// and inherits <see cref="ObservableObject"/> so models can raise PropertyChanged for WPF data binding.
    /// </summary>
    public abstract class BaseModel : ObservableObject
    {
        public int Id { get; set; }
    }
}
