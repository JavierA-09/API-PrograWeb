using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace API_LabProgra.Models;

public partial class SistemaMedicoContext : DbContext
{
    public SistemaMedicoContext()
    {
    }

    public SistemaMedicoContext(DbContextOptions<SistemaMedicoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AreasMedica> AreasMedicas { get; set; }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<Cuentum> Cuenta { get; set; }

    public virtual DbSet<Doctore> Doctores { get; set; }

    public virtual DbSet<EstadoCitum> EstadoCita { get; set; }

    public virtual DbSet<HistorialMedico> HistorialMedicos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AreasMedica>(entity =>
        {
            entity.HasKey(e => e.IdArea).HasName("PK__Areas_Me__F9279C80BED62E47");

            entity.ToTable("Areas_Medicas");

            entity.Property(e => e.IdArea)
                .ValueGeneratedNever()
                .HasColumnName("Id_area");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.IdCita).HasName("PK__Citas__5E31E370A50CE051");

            entity.Property(e => e.IdCita).HasColumnName("Id_cita");
            entity.Property(e => e.Comentarios).HasMaxLength(500);
            entity.Property(e => e.EstadoCita).HasColumnName("Estado_cita");
            entity.Property(e => e.FechaCalificacion)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_calificacion");
            entity.Property(e => e.FechaHora)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_hora");
            entity.Property(e => e.IdDoctor).HasColumnName("Id_doctor");
            entity.Property(e => e.IdUsuario).HasColumnName("Id_usuario");

            entity.HasOne(d => d.EstadoCitaNavigation).WithMany(p => p.Cita)
                .HasForeignKey(d => d.EstadoCita)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Estado");

            entity.HasOne(d => d.IdDoctorNavigation).WithMany(p => p.Cita)
                .HasForeignKey(d => d.IdDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Doctores");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Cita)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade) 
                .HasConstraintName("FK_Citas_Cuenta");
        });

        modelBuilder.Entity<Cuentum>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Cuenta__EF59F76263D3A9B0");

            entity.Property(e => e.IdUsuario).HasColumnName("Id_usuario");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Ciudad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Contraseña).HasMaxLength(200);
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Usuario).HasMaxLength(50);

            entity.HasOne(d => d.RolNavigation).WithMany(p => p.Cuenta)
                .HasForeignKey(d => d.Rol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cuenta_Roles");
        });

        modelBuilder.Entity<Doctore>(entity =>
        {
            entity.HasKey(e => e.IdDoctor).HasName("PK__Doctores__429B975F5BE37762");

            entity.Property(e => e.IdDoctor).HasColumnName("Id_doctor");
            entity.Property(e => e.Especialidad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArea).HasColumnName("Id_area");
            entity.Property(e => e.IdUsuario).HasColumnName("Id_usuario");
            entity.Property(e => e.LicenciaMedica)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Licencia_medica");

            entity.HasOne(d => d.IdAreaNavigation).WithMany(p => p.Doctores)
                .HasForeignKey(d => d.IdArea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Doctores_Areas");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Doctores)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade) 
                .HasConstraintName("FK_Doctores_Cuenta");
        });

        modelBuilder.Entity<EstadoCitum>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PK__Estado_C__AB2EB6F89CB34A60");

            entity.ToTable("Estado_Cita");

            entity.Property(e => e.IdEstado)
                .ValueGeneratedNever()
                .HasColumnName("Id_Estado");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<HistorialMedico>(entity =>
        {
            entity.HasKey(e => e.HistorialId).HasName("PK__Historia__97263FD203AF3AB1");

            entity.ToTable("Historial_Medico");

            entity.Property(e => e.HistorialId).HasColumnName("Historial_id");
            entity.Property(e => e.ConsumoBebidas)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Consumo_bebidas");
            entity.Property(e => e.Habitos).HasMaxLength(500);
            entity.Property(e => e.IdDoctor).HasColumnName("Id_doctor");
            entity.Property(e => e.IdUsuario).HasColumnName("Id_usuario");

            entity.HasOne(d => d.IdDoctorNavigation).WithMany(p => p.HistorialMedicos)
                .HasForeignKey(d => d.IdDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Historial_Doctores");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialMedicos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade) 
                .HasConstraintName("FK_Historial_Cuenta");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK__Roles__76482FD2C6058C99");

            entity.Property(e => e.IdRol)
                .ValueGeneratedNever()
                .HasColumnName("id_Rol");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
