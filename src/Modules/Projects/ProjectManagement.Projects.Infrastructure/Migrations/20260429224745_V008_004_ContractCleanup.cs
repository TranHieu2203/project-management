using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectManagement.Projects.Infrastructure.Persistence;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    /// <summary>
    /// Phase 4 — Contract/Cleanup. DEFER: Only run after Phase 1-3 stable on staging >= 1 week
    /// and ALL pre-conditions are met (see checklist in story 8-0).
    /// </summary>
    [DbContext(typeof(ProjectsDbContext))]
    [Migration("20260429224745_V008_004_ContractCleanup")]
    public partial class V008_004_ContractCleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DEFERRED:
            // Phase 4 contract/cleanup MUST NOT auto-run in story 8.0.
            // This migration is intentionally a no-op during normal development/test runs.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no-op (deferred migration)
        }
    }
}
