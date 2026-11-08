using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyReviewConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DanhGia_DiemDichVu')
BEGIN
    ALTER TABLE [dbo].[DanhGia] DROP CONSTRAINT [CK_DanhGia_DiemDichVu];
END

IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.DanhGia') AND name = 'DiemDichVu'
)
BEGIN
    DECLARE @defName sysname;
    SELECT @defName = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
    WHERE d.parent_object_id = OBJECT_ID('dbo.DanhGia') AND c.name = 'DiemDichVu';
    
    IF @defName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [dbo].[DanhGia] DROP CONSTRAINT [' + @defName + '];');
    END
    
    ALTER TABLE [dbo].[DanhGia] DROP COLUMN [DiemDichVu];
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
