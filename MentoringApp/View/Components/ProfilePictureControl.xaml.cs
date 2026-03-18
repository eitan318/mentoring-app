using System.Windows;
using System.Windows.Controls;

namespace MentoringApp.View.Components
{
    public partial class ProfilePictureControl : UserControl
    {
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(ProfilePictureControl), new PropertyMetadata(null));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        public ProfilePictureControl()
        {
            InitializeComponent();
        }
    }
}
