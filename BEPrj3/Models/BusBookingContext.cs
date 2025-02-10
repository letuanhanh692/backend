using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models.DTO;

namespace BEPrj3.Models;

public partial class BusBookingContext : DbContext
{

    public class ApplicationUser : IdentityUser
    {
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    }
    public BusBookingContext()
    {
    }

    public BusBookingContext(DbContextOptions<BusBookingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Bus> Buses { get; set; }

    public virtual DbSet<BusType> BusTypes { get; set; }

    public virtual DbSet<Cancellation> Cancellations { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PriceList> PriceLists { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<StaffRoute> StaffRoutes { get; set; }


    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=Zun6925\\SQLEXPRESS;Initial Catalog=BusBooking;User ID=sa;Password=12345678;Encrypt=False;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Bookings__3213E83FA49BCFFD");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("booking_date");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.SeatNumber).HasColumnName("seat_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Schedule");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_User");
        });

        modelBuilder.Entity<Bus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Buses__3213E83FE96BA547");

            entity.HasIndex(e => e.BusNumber, "UQ__Buses__0D3182B985761C81").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusNumber)
                .HasMaxLength(20)
                .HasColumnName("bus_number");
            entity.Property(e => e.BusTypeId).HasColumnName("bus_type_id");
            entity.Property(e => e.TotalSeats).HasColumnName("total_seats");

            entity.HasOne(d => d.BusType).WithMany(p => p.Buses)
                .HasForeignKey(d => d.BusTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bus_BusType");
        });

        modelBuilder.Entity<BusType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BusTypes__3213E83FF98F7ACB");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Cancellation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cancella__3213E83FE297D2A9");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CancellationDate)
                .HasColumnType("datetime")
                .HasColumnName("cancellation_date");
            entity.Property(e => e.RefundAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("refund_amount");

            entity.HasOne(d => d.Booking).WithMany(p => p.Cancellations)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cancellation_Booking");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3213E83FBF7074A8");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .HasColumnName("method");
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(50)
                .HasColumnName("payment_code");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Booking");
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PriceLis__3213E83F83105072");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusTypeId).HasColumnName("bus_type_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.RouteId).HasColumnName("route_id");

            entity.HasOne(d => d.BusType).WithMany(p => p.PriceLists)
                .HasForeignKey(d => d.BusTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Price_BusType");

            entity.HasOne(d => d.Route).WithMany(p => p.PriceLists)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Price_Route");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3213E83FB2420FF2");

            entity.ToTable("Role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Routes__3213E83FAD0FC8A0");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DestinationPlace)
                .HasMaxLength(100)
                .HasColumnName("destination_place");
            entity.Property(e => e.Distance)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("distance");
            entity.Property(e => e.StartingPlace)
                .HasMaxLength(100)
                .HasColumnName("starting_place");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Schedule__3213E83FD610C906");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArrivalTime)
                .HasColumnType("datetime")
                .HasColumnName("arrival_time");
            entity.Property(e => e.AvailableSeats).HasColumnName("available_seats");
            entity.Property(e => e.BusId).HasColumnName("bus_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DepartureTime)
                .HasColumnType("datetime")
                .HasColumnName("departure_time");
            entity.Property(e => e.RouteId).HasColumnName("route_id");

            entity.HasOne(d => d.Bus).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.BusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedule_Bus");

            entity.HasOne(d => d.Route).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedule_Route");
        });

        modelBuilder.Entity<StaffRoute>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffRou__3213E83F61DCC8FB");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RouteId).HasColumnName("route_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.HasOne(d => d.Route).WithMany(p => p.StaffRoutes)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffRoute_Route");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffRoutes)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffRoute_Staff");
        });

      

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83F6A438CED");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E61647145A570").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC572F94D3FF4").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IdCard)
                .HasMaxLength(12)
                .HasColumnName("id_card");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

public DbSet<BEPrj3.Models.DTO.UserDTO> UserDTO { get; set; } = default!;

public DbSet<BEPrj3.Models.DTO.RouteDTO> RouteDTO { get; set; } = default!;
}
