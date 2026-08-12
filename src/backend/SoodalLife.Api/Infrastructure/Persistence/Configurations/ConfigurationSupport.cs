using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal abstract class EntityConfiguration<TEntity>(string tableName) : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    protected string TableName { get; } = tableName;

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(TableName);
        Mapping.Id(builder);
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}

internal static class Mapping
{
    public static void Id<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.HasKey("Id");
        builder.Property<long>("Id").HasColumnName("id").HasColumnType("bigint").UseIdentityColumn();
    }

    public static void Long<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, bool required = true)
        where TEntity : class => Required(builder.Property<long>(property).HasColumnName(column).HasColumnType("bigint"), required);

    public static void NullableLong<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column)
        where TEntity : class => builder.Property<long?>(property).HasColumnName(column).HasColumnType("bigint").IsRequired(false);

    public static void Int<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, int? defaultValue = null)
        where TEntity : class
    {
        var p = builder.Property<int>(property).HasColumnName(column).HasColumnType("int").IsRequired();
        if (defaultValue.HasValue) p.HasDefaultValue(defaultValue.Value);
    }

    public static void Short<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, short? defaultValue = null)
        where TEntity : class
    {
        var p = builder.Property<short>(property).HasColumnName(column).HasColumnType("smallint").IsRequired();
        if (defaultValue.HasValue) p.HasDefaultValue(defaultValue.Value);
    }

    public static void NullableShort<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column)
        where TEntity : class => builder.Property<short?>(property).HasColumnName(column).HasColumnType("smallint").IsRequired(false);

    public static void Binary<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, int? length = null)
        where TEntity : class
    {
        var storeType = length.HasValue ? $"binary({length.Value})" : "varbinary(max)";
        var p = builder.Property<byte[]?>(property).HasColumnName(column).HasColumnType(storeType).IsRequired(false);
        if (length.HasValue) p.HasMaxLength(length.Value).IsFixedLength();
    }

    public static void Bool<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, bool? defaultValue = null)
        where TEntity : class
    {
        var p = builder.Property<bool>(property).HasColumnName(column).HasColumnType("bit").IsRequired();
        if (defaultValue.HasValue) p.HasDefaultValue(defaultValue.Value);
    }

    public static void Decimal<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, bool nullable = false, int precision = 19, int scale = 4, decimal? defaultValue = null)
        where TEntity : class
    {
        if (nullable)
        {
            var p = builder.Property<decimal?>(property).HasColumnName(column).HasColumnType($"decimal({precision},{scale})").HasPrecision(precision, scale).IsRequired(false);
            if (defaultValue.HasValue) p.HasDefaultValue(defaultValue.Value);
        }
        else
        {
            var p = builder.Property<decimal>(property).HasColumnName(column).HasColumnType($"decimal({precision},{scale})").HasPrecision(precision, scale).IsRequired();
            if (defaultValue.HasValue) p.HasDefaultValue(defaultValue.Value);
        }
    }

    public static void Guid<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, bool nullable = false)
        where TEntity : class
    {
        if (nullable) builder.Property<Guid?>(property).HasColumnName(column).HasColumnType("uniqueidentifier").IsRequired(false);
        else builder.Property<Guid>(property).HasColumnName(column).HasColumnType("uniqueidentifier").IsRequired();
    }

    public static void String<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, int? maxLength, bool nullable = false, bool unicode = true, bool fixedLength = false, string? defaultValue = null)
        where TEntity : class
    {
        var typeName = unicode ? "nvarchar" : "varchar";
        if (fixedLength) typeName = unicode ? "nchar" : "char";
        var storeType = maxLength.HasValue ? $"{typeName}({maxLength.Value})" : $"{typeName}(max)";
        var p = builder.Property<string?>(property).HasColumnName(column).HasColumnType(storeType).IsUnicode(unicode).IsRequired(!nullable);
        if (maxLength.HasValue) p.HasMaxLength(maxLength.Value);
        if (fixedLength) p.IsFixedLength();
        if (defaultValue is not null) p.HasDefaultValue(defaultValue);
    }

    public static void DateTime<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, bool nullable = false, bool utcDefault = false)
        where TEntity : class
    {
        if (nullable)
        {
            builder.Property<DateTime?>(property).HasColumnName(column).HasColumnType("datetime2(7)").HasPrecision(7).IsRequired(false);
        }
        else
        {
            var p = builder.Property<DateTime>(property).HasColumnName(column).HasColumnType("datetime2(7)").HasPrecision(7).IsRequired();
            if (utcDefault) p.HasDefaultValueSql("SYSUTCDATETIME()");
        }
    }

    public static void Date<TEntity>(EntityTypeBuilder<TEntity> builder, string property, string column, bool nullable = false)
        where TEntity : class
    {
        if (nullable) builder.Property<DateOnly?>(property).HasColumnName(column).HasColumnType("date").IsRequired(false);
        else builder.Property<DateOnly>(property).HasColumnName(column).HasColumnType("date").IsRequired();
    }

    public static void RowVersion<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class =>
        builder.Property<byte[]>("RowVersion").HasColumnName("row_version").HasColumnType("rowversion").IsRowVersion().IsConcurrencyToken();

    public static void PublicId<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        Guid(builder, "PublicId", "public_id");
        builder.HasIndex("PublicId").IsUnique();
    }

    public static void FullAudit<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        DateTime(builder, "CreatedAt", "created_at", utcDefault: true);
        NullableLong(builder, "CreatedByUserId", "created_by_user_id");
        DateTime(builder, "UpdatedAt", "updated_at", utcDefault: true);
        NullableLong(builder, "UpdatedByUserId", "updated_by_user_id");
        RowVersion(builder);
        Fk<TEntity, global::SoodalLife.Api.Domain.Entities.User>(builder, "CreatedByUserId");
        Fk<TEntity, global::SoodalLife.Api.Domain.Entities.User>(builder, "UpdatedByUserId");
    }

    public static void CreatedAudit<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        DateTime(builder, "CreatedAt", "created_at", utcDefault: true);
        NullableLong(builder, "CreatedByUserId", "created_by_user_id");
        Fk<TEntity, global::SoodalLife.Api.Domain.Entities.User>(builder, "CreatedByUserId");
    }

    public static void Fk<TEntity, TPrincipal>(EntityTypeBuilder<TEntity> builder, string foreignKey)
        where TEntity : class where TPrincipal : class =>
        builder.HasOne<TPrincipal>().WithMany().HasForeignKey(foreignKey).OnDelete(DeleteBehavior.NoAction);

    private static void Required<T>(PropertyBuilder<T> property, bool required) where T : struct
    {
        if (required) property.IsRequired();
        else property.IsRequired(false);
    }
}
