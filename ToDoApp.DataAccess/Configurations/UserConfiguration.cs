using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDoApp.Interfaces.Entities;

namespace ToDoApp.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Provider).IsRequired().HasMaxLength(32);
        builder.Property(u => u.ProviderKey).IsRequired().HasMaxLength(128);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(128);
        builder.Property(u => u.PictureUrl).HasMaxLength(512);

        builder.HasIndex(u => new { u.Provider, u.ProviderKey }).IsUnique();
    }
}
