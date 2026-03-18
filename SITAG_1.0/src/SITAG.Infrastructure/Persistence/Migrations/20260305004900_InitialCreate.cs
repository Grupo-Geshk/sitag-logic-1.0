using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "farms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Hectares = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_farms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_farms_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "producers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_producers_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PaidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_audit_logs_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transaction_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transaction_categories_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RoleLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Contact = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workers_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "divisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_divisions_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_divisions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    MinStockLevel = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplies_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplies_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TxnDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_transactions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_transactions_transaction_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "transaction_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vet_services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vet_services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vet_services_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vet_services_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vet_services_workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "worker_farm_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_farm_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_worker_farm_assignments_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_farm_assignments_workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "animals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HealthStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloseReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_animals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_animals_animals_ParentId",
                        column: x => x.ParentId,
                        principalTable: "animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_animals_divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_animals_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_animals_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supply_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    PreviousQuantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    NewQuantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MovementDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supply_movements_supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalTable: "supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_supply_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_supply_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_supply_consumptions_supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalTable: "supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_supply_consumptions_vet_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "vet_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "animal_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EventDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_animal_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_animal_events_animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_animal_events_workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "animal_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromFarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromDivisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToFarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToDivisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    MovementDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_animal_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_animal_movements_animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_animals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_animals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_animals_animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_animals_vet_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "vet_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_animal_events_AnimalId",
                table: "animal_events",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_animal_events_WorkerId",
                table: "animal_events",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_animal_movements_AnimalId",
                table: "animal_movements",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_animals_DivisionId",
                table: "animals",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_animals_FarmId",
                table: "animals",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_animals_ParentId",
                table: "animals",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_animals_TenantId_TagNumber",
                table: "animals",
                columns: new[] { "TenantId", "TagNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_divisions_FarmId",
                table: "divisions",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_divisions_TenantId",
                table: "divisions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_transactions_CategoryId",
                table: "economy_transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_transactions_TenantId",
                table: "economy_transactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_farms_TenantId",
                table: "farms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_producers_TenantId",
                table: "producers",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_animals_AnimalId",
                table: "service_animals",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_service_animals_ServiceId_AnimalId",
                table: "service_animals",
                columns: new[] { "ServiceId", "AnimalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_supply_consumptions_ServiceId",
                table: "service_supply_consumptions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_service_supply_consumptions_SupplyId",
                table: "service_supply_consumptions",
                column: "SupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_supplies_FarmId",
                table: "supplies",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_supplies_TenantId",
                table: "supplies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_supply_movements_SupplyId",
                table: "supply_movements",
                column: "SupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_audit_logs_TenantId",
                table: "tenant_audit_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_categories_TenantId_Name_Type",
                table: "transaction_categories",
                columns: new[] { "TenantId", "Name", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId_Email",
                table: "users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vet_services_FarmId",
                table: "vet_services",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_vet_services_TenantId",
                table: "vet_services",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_vet_services_WorkerId",
                table: "vet_services",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_worker_farm_assignments_FarmId",
                table: "worker_farm_assignments",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_worker_farm_assignments_WorkerId_FarmId",
                table: "worker_farm_assignments",
                columns: new[] { "WorkerId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_workers_TenantId",
                table: "workers",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "animal_events");

            migrationBuilder.DropTable(
                name: "animal_movements");

            migrationBuilder.DropTable(
                name: "economy_transactions");

            migrationBuilder.DropTable(
                name: "producers");

            migrationBuilder.DropTable(
                name: "service_animals");

            migrationBuilder.DropTable(
                name: "service_supply_consumptions");

            migrationBuilder.DropTable(
                name: "supply_movements");

            migrationBuilder.DropTable(
                name: "tenant_audit_logs");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "worker_farm_assignments");

            migrationBuilder.DropTable(
                name: "transaction_categories");

            migrationBuilder.DropTable(
                name: "animals");

            migrationBuilder.DropTable(
                name: "vet_services");

            migrationBuilder.DropTable(
                name: "supplies");

            migrationBuilder.DropTable(
                name: "divisions");

            migrationBuilder.DropTable(
                name: "workers");

            migrationBuilder.DropTable(
                name: "farms");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
