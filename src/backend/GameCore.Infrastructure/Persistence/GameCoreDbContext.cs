using GameCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Infrastructure.Persistence;

public sealed class GameCoreDbContext(DbContextOptions<GameCoreDbContext> options)
    : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AgeRating> AgeRatings => Set<AgeRating>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<GameGenre> GameGenres => Set<GameGenre>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<GamePlatform> GamePlatforms => Set<GamePlatform>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(x => x.CompanyId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.Email).HasMaxLength(150).IsUnicode(false);
            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasKey(x => x.BranchId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(250);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Branches)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<JobPosition>(entity =>
        {
            entity.ToTable("JobPositions");
            entity.HasKey(x => x.JobPositionId);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(x => x.EmployeeId);
            entity.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.Email).HasMaxLength(150).IsUnicode(false);
            entity.Property(x => x.AddressLine).HasMaxLength(250);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.JobPosition)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.JobPositionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.CustomerId);
            entity.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.Email).HasMaxLength(150).IsUnicode(false);
            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<AgeRating>(entity =>
        {
            entity.ToTable("AgeRatings");
            entity.HasKey(x => x.AgeRatingId);
            entity.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(x => x.GameId);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Story).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ReleaseDate).HasColumnType("date");
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();
            entity.HasIndex(x => new { x.Title, x.ReleaseDate }).IsUnique();

            entity.HasOne(x => x.AgeRating)
                .WithMany(x => x.Games)
                .HasForeignKey(x => x.AgeRatingId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genres");
            entity.HasKey(x => x.GenreId);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<GameGenre>(entity =>
        {
            entity.ToTable("GameGenres");
            entity.HasKey(x => new { x.GameId, x.GenreId });

            entity.HasOne(x => x.Game)
                .WithMany(x => x.GameGenres)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Genre)
                .WithMany(x => x.GameGenres)
                .HasForeignKey(x => x.GenreId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Platform>(entity =>
        {
            entity.ToTable("Platforms");
            entity.HasKey(x => x.PlatformId);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<GamePlatform>(entity =>
        {
            entity.ToTable("GamePlatforms");
            entity.HasKey(x => new { x.GameId, x.PlatformId });

            entity.HasOne(x => x.Game)
                .WithMany(x => x.GamePlatforms)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Platform)
                .WithMany(x => x.GamePlatforms)
                .HasForeignKey(x => x.PlatformId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries");
            entity.HasKey(x => x.CountryId);
            entity.Property(x => x.Iso2)
                .HasMaxLength(2)
                .IsFixedLength()
                .IsUnicode(false)
                .IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Iso2).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Distribution>(entity =>
        {
            entity.ToTable("Distributions");
            entity.HasKey(x => x.DistributionId);
            entity.Property(x => x.DistributionDate).HasColumnType("date");
            entity.HasIndex(x => new { x.GameId, x.CountryId, x.DistributionDate }).IsUnique();

            entity.HasOne(x => x.Game)
                .WithMany(x => x.Distributions)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Country)
                .WithMany(x => x.Distributions)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(x => x.SaleId);
            entity.Property(x => x.SaleDate)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();
            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Completed")
                .IsRequired();

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.ToTable("SaleDetails");
            entity.HasKey(x => x.SaleDetailId);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.HasIndex(x => new { x.SaleId, x.GameId }).IsUnique();

            entity.HasOne(x => x.Sale)
                .WithMany(x => x.SaleDetails)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Game)
                .WithMany(x => x.SaleDetails)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
