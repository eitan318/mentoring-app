using MentoringApp.Model.User;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MentoringApp.View.Components
{
    public partial class ProfilePictureControl : UserControl
    {
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(ProfilePictureControl),
                new PropertyMetadata(null, OnImagePathOrGenderChanged));

        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public static readonly DependencyProperty GenderProperty =
            DependencyProperty.Register("Gender", typeof(Gender), typeof(ProfilePictureControl),
                new PropertyMetadata(Gender.PreferNoAnswer, OnImagePathOrGenderChanged));

        public Gender Gender
        {
            get => (Gender)GetValue(GenderProperty);
            set => SetValue(GenderProperty, value);
        }

        private static readonly DependencyPropertyKey DefaultFillPropertyKey =
            DependencyProperty.RegisterReadOnly("DefaultFill", typeof(Brush), typeof(ProfilePictureControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))));

        public static readonly DependencyProperty DefaultFillProperty = DefaultFillPropertyKey.DependencyProperty;
        public Brush DefaultFill => (Brush)GetValue(DefaultFillProperty);

        private static readonly DependencyPropertyKey ShowDefaultAvatarPropertyKey =
            DependencyProperty.RegisterReadOnly("ShowDefaultAvatar", typeof(bool), typeof(ProfilePictureControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ShowDefaultAvatarProperty = ShowDefaultAvatarPropertyKey.DependencyProperty;
        public bool ShowDefaultAvatar => (bool)GetValue(ShowDefaultAvatarProperty);

        private static void OnImagePathOrGenderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((ProfilePictureControl)d).UpdateDefaultAvatar();

        private void UpdateDefaultAvatar()
        {
            bool noImage = string.IsNullOrWhiteSpace(ImagePath) || !System.IO.File.Exists(ImagePath);
            SetValue(ShowDefaultAvatarPropertyKey, noImage);

            var color = Gender switch
            {
                Gender.Male   => Color.FromRgb(0xB3, 0xD4, 0xFF),
                Gender.Female => Color.FromRgb(0xFF, 0xD6, 0xE8),
                _             => Color.FromRgb(0xCC, 0xCC, 0xCC)
            };
            SetValue(DefaultFillPropertyKey, new SolidColorBrush(color));
        }

        public ProfilePictureControl()
        {
            InitializeComponent();
        }
    }
}
