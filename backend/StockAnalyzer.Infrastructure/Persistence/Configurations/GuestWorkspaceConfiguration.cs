using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Workspace;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class GuestWorkspaceConfiguration : IEntityTypeConfiguration<GuestWorkspace>
{
    public void Configure(EntityTypeBuilder<GuestWorkspace> builder)
    {
        builder.ToTable("GuestWorkspaces");
        builder.HasKey(workspace => workspace.Id);
        builder.Property(workspace => workspace.ClientId).HasMaxLength(36).IsRequired();
        builder.HasIndex(workspace => workspace.ClientId);
        builder.HasIndex(workspace => workspace.UserId).IsUnique();
        builder.HasOne<StockAnalyzerUser>().WithMany().HasForeignKey(workspace => workspace.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(workspace => workspace.WatchlistJson).HasColumnType("jsonb").IsRequired();
        builder.Property(workspace => workspace.AlertStateJson).HasColumnType("jsonb").IsRequired();
        builder.Property(workspace => workspace.PortfolioJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
    }
}
