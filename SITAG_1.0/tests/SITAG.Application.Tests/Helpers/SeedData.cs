using SITAG.Domain.Entities;
using SITAG.Domain.Enums;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Application.Tests.Helpers;

/// <summary>
/// Helpers to seed standard test data into an in-memory context.
/// </summary>
public static class SeedData
{
    public static (Tenant tenant, User admin, Farm farm) SeedBasic(
        SitagDbContext db,
        Guid? tenantId = null,
        string passwordHash = "hashed_password")
    {
        var tid = tenantId ?? Guid.NewGuid();

        var tenant = new Tenant
        {
            Id           = tid,
            Name         = "Test Farm Co",
            PrimaryEmail = "farm@test.com",
            Status       = TenantStatus.Active,
            PaidUntil    = DateTimeOffset.UtcNow.AddYears(1),
        };

        var admin = new User
        {
            Id           = Guid.NewGuid(),
            TenantId     = tid,
            Email        = "admin@test.com",
            PasswordHash = passwordHash,
            Role         = UserRole.AdminSistema,
            IsActive     = true,
            FirstName    = "Test",
            LastName     = "Admin",
        };

        var farm = new Farm
        {
            Id       = Guid.NewGuid(),
            TenantId = tid,
            Name     = "Main Farm",
        };

        db.Tenants.Add(tenant);
        db.Users.Add(admin);
        db.Farms.Add(farm);
        db.SaveChanges();

        return (tenant, admin, farm);
    }

    public static Animal SeedAnimal(
        SitagDbContext db,
        Guid tenantId,
        Guid farmId,
        string tagNumber = "TAG001",
        AnimalStatus status = AnimalStatus.Activo)
    {
        var animal = new Animal
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenantId,
            FarmId    = farmId,
            TagNumber = tagNumber,
            Sex       = "M",
            Status    = status,
        };
        db.Animals.Add(animal);
        db.SaveChanges();
        return animal;
    }

    public static Supply SeedSupply(
        SitagDbContext db,
        Guid tenantId,
        Guid farmId,
        string name = "Vaccine A",
        decimal currentQty = 100m,
        decimal minStock = 10m)
    {
        var supply = new Supply
        {
            Id              = Guid.NewGuid(),
            TenantId        = tenantId,
            FarmId          = farmId,
            Name            = name,
            Unit            = "units",
            CurrentQuantity = currentQty,
            MinStockLevel   = minStock,
        };
        db.Supplies.Add(supply);
        db.SaveChanges();
        return supply;
    }

    public static VetService SeedVetService(
        SitagDbContext db,
        Guid tenantId,
        Guid farmId,
        string serviceType = "Sanidad")
    {
        var svc = new VetService
        {
            Id            = Guid.NewGuid(),
            TenantId      = tenantId,
            FarmId        = farmId,
            ServiceType   = serviceType,
            ScheduledDate = DateTimeOffset.UtcNow,
        };
        db.VetServices.Add(svc);
        db.SaveChanges();
        return svc;
    }
}
