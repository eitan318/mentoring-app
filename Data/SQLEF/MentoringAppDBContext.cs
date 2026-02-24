using Microsoft.EntityFrameworkCore;
using MentoringApp.Data.SQLEF.DataObject;

namespace MentoringApp.Data.SQLEF
{
    internal class MentoringDbContext : DbContext
    {
        public MentoringDbContext(DbContextOptions<MentoringDbContext> options) : base(options) { }

        // The Tables
        public DbSet<UserData> Users { get; set; }
        public DbSet<VerificationCodeData> VerificationCodes { get; set; }
        public DbSet<UserStudentData> Students { get; set; }
        public DbSet<UserMentorData> Mentors { get; set; }
        public DbSet<UserMenteeData> Mentees { get; set; }
        public DbSet<UserSupervisorData> Supervisors { get; set; }
        public DbSet<UserAdminData> Admins { get; set; }
        public DbSet<PairData> Pairs { get; set; }
        public DbSet<IssueData> Issues { get; set; }
        public DbSet<IssueCategoryData> IssueCategories { get; set; }
        public DbSet<ReviewData> Reviews { get; set; }
        public DbSet<SubjectData> Subjects { get; set; }
        public DbSet<GradeData> Grades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserData Configuration
            modelBuilder.Entity<UserData>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NationalId).IsRequired().HasMaxLength(20);
            });

            // UserStudentData Configuration
            modelBuilder.Entity<UserStudentData>(entity =>
            {
                entity.ToTable("UserStudents");
                entity.HasKey(e => e.UserId); 
                // If you want a database-level constraint without C# Nav Properties:
                entity.HasOne<UserData>()
                    .WithOne()
                    .HasForeignKey<UserStudentData>(e => e.UserId);
            });

            // UserMentorData Configuration
            modelBuilder.Entity<UserMentorData>(entity =>
            {
                entity.ToTable("UserMentors");
                entity.HasKey(e => e.UserId);

                entity.HasOne<UserData>()
                    .WithOne()
                    .HasForeignKey<UserMentorData>(e => e.UserId);
            });

            // UserMenteeData Configuration
            modelBuilder.Entity<UserMenteeData>(entity =>
            {
                entity.ToTable("UserMentees");
                entity.HasKey(e => e.UserId);

                entity.HasOne<UserData>()
                    .WithOne()
                    .HasForeignKey<UserMenteeData>(e => e.UserId);
            });

            // UserSupervisorData Configuration
            modelBuilder.Entity<UserSupervisorData>(entity =>
            {
                entity.ToTable("UserSupervisors");
                entity.HasKey(e => e.UserId);

                entity.HasOne<UserData>()
                    .WithOne()
                    .HasForeignKey<UserSupervisorData>(e => e.UserId);
            });

            // UserSupervisorData Configuration
            modelBuilder.Entity<UserAdminData>(entity =>
            {
                entity.ToTable("UserAdmins");
                entity.HasKey(e => e.UserId);

                entity.HasOne<UserData>()
                    .WithOne()
                    .HasForeignKey<UserAdminData>(e => e.UserId);
            });

            // VerificationCodeData Configuration
            modelBuilder.Entity<VerificationCodeData>(entity =>
            {
                entity.ToTable("VerificationCodes");
                entity.HasKey(e => e.UserId); 
                entity.HasOne<UserData>()
                    .WithOne()
                    .HasForeignKey<VerificationCodeData>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PairData Configuration
            modelBuilder.Entity<PairData>(entity =>
            {
                entity.ToTable("Pairs");
                entity.HasKey(e => e.Id);
                entity.HasOne<UserData>()
                    .WithMany()
                    .HasForeignKey(e => e.MentorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<UserData>()
                    .WithMany()
                    .HasForeignKey(e => e.MenteeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<UserData>()
                    .WithMany()
                    .HasForeignKey(e => e.SupervisorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // IssueCategoryData Configuration
            modelBuilder.Entity<IssueCategoryData>(entity =>
            {
                entity.ToTable("IssueCategories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // IssueData Configuration
            modelBuilder.Entity<IssueData>(entity =>
            {
                entity.ToTable("Issues");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired();
                entity.HasOne<UserData>()
                    .WithMany()
                    .HasForeignKey(e => e.ReportedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<IssueCategoryData>()
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ReviewData Configuration
            modelBuilder.Entity<ReviewData>(entity =>
            {
                entity.ToTable("Reviews");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.HasOne<PairData>()
                    .WithMany()
                    .HasForeignKey(e => e.PairId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<UserData>()
                    .WithMany()
                    .HasForeignKey(e => e.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SubjectData>(entity =>
            {
                entity.ToTable("Subjects");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<GradeData>(entity =>
            {
                entity.ToTable("Grades");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });
        }
    }
}

