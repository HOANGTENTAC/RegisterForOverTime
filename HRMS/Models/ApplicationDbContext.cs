using System.Data.Entity;

namespace HRMS.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
            // Tắt initializer để EF không kiểm tra schema
            Database.SetInitializer<ApplicationDbContext>(null);
        }
        public DbSet<EmployeeModel> Employees { get; set; }

        public DbSet<DepartmentModel> Departments { get; set; }

        public DbSet<TblTicketsModel> TblTickets { get; set; }

        public DbSet<TblOvertimeHeadersModel> TblOvertimeHeaders { get; set; }

        public DbSet<TblOvertimeDetailsModel> TblOvertimeDetails { get; set; }

        public DbSet<MstTicketTypesModel> MstTicketTypes { get; set; }

        public DbSet<MstTicketStatusModel> MstTicketStatuses { get; set; }

        public DbSet<MstShiftTypesModel> MstShiftTypes { get; set; }

        public DbSet<TblShiftRegisterHeadersModel> TblShiftRegisterHeaders { get; set; }

        public DbSet<TblShiftRegisterDetailsModel> TblShiftRegisterDetails { get; set; }

        public DbSet<UsersModel> Users { get; set; }

        public DbSet<UserRolesModel> UserRoles { get; set; }

        public DbSet<MstDefaultShiftsModel> DefaultShifts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TblOvertimeHeadersModel>()
                .HasKey(c => new
                {
                    c.Id,
                    c.TicketId
                });

            modelBuilder.Entity<TblOvertimeDetailsModel>()
                .HasKey(c => new
                {
                    c.EmployeeCD,
                    c.OvertimeHeaderId
                });

            modelBuilder.Entity<UserRolesModel>()
               .HasKey(x => new
               {
                   x.MaNhanVien,
                   x.BoPhanQuanLy
               });

            modelBuilder.Entity<TblShiftRegisterHeadersModel>()
                .HasKey(c => new
                {
                    c.Id,
                    c.TicketId
                });

            modelBuilder.Entity<TblShiftRegisterDetailsModel>()
                .HasKey(c => new
                {
                    c.EmployeeCD,
                    c.ShiftRegisterHeaderId,
                    c.WorkDate
                });

            base.OnModelCreating(modelBuilder);
        }

    }
}