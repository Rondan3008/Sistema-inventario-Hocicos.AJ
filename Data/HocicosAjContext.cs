using System;
using System.Collections.Generic;
using HocicosAJ.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Data;

public partial class HocicosAjContext : DbContext
{
    public HocicosAjContext(DbContextOptions<HocicosAjContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Compra> Compras { get; set; }

    public virtual DbSet<Comprobante> Comprobantes { get; set; }

    public virtual DbSet<DetalleCompra> DetalleCompras { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.Idcategoria).HasName("PRIMARY");

            entity.ToTable("categoria");

            entity.Property(e => e.Activo)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Idcategoria).HasColumnName("idcategoria");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .HasColumnName("descripcion");
            entity.Property(e => e.NombreCategoria)
                .HasMaxLength(45)
                .HasColumnName("nombre_categoria");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.Idcompras).HasName("PRIMARY");

            entity.ToTable("compras");

            entity.HasIndex(e => e.ClienteIdusuarios, "fk_compras_cliente_idx");

            entity.HasIndex(e => e.UsuariosIdusuarios, "fk_compras_usuarios1_idx");

            entity.HasIndex(e => e.ClaveIdempotencia, "uq_compras_clave_idempotencia")
                .IsUnique();

            entity.Property(e => e.Idcompras)
                .ValueGeneratedOnAdd()
                .HasColumnName("idcompras");
            entity.Property(e => e.ClienteIdusuarios).HasColumnName("cliente_idusuarios");
            entity.Property(e => e.ClaveIdempotencia)
                .HasColumnType("char")
                .HasMaxLength(36)
                .HasColumnName("clave_idempotencia");
            entity.Property(e => e.Estado)
                .HasMaxLength(45)
                .HasColumnName("estado");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("fecha");
            entity.Property(e => e.Total)
                .HasPrecision(10, 2)
                .HasColumnName("total");
            entity.Property(e => e.UsuariosIdusuarios).HasColumnName("usuarios_idusuarios");

            entity.HasOne(d => d.ClienteIdusuariosNavigation).WithMany(p => p.CompraClienteIdusuariosNavigations)
                .HasForeignKey(d => d.ClienteIdusuarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_compras_cliente");

            entity.HasOne(d => d.UsuariosIdusuariosNavigation).WithMany(p => p.CompraUsuariosIdusuariosNavigations)
                .HasForeignKey(d => d.UsuariosIdusuarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_compras_usuarios1");
        });

        modelBuilder.Entity<Comprobante>(entity =>
        {
            entity.HasKey(e => e.Idcomprobante).HasName("PRIMARY");

            entity.ToTable("comprobante");

            entity.HasIndex(e => e.ComprasIdcompras, "fk_comprobante_compras1_idx");

            entity.HasIndex(e => e.NumeroComprobante, "numero_comprobante_UNIQUE").IsUnique();

            entity.Property(e => e.Idcomprobante)
                .ValueGeneratedOnAdd()
                .HasColumnName("idcomprobante");
            entity.Property(e => e.ComprasIdcompras).HasColumnName("compras_idcompras");
            entity.Property(e => e.FechaEmision)
                .HasColumnType("datetime")
                .HasColumnName("fecha_emision");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(30)
                .HasColumnName("metodo_pago");
            entity.Property(e => e.MontoTotal)
                .HasPrecision(10, 2)
                .HasColumnName("monto_total");
            entity.Property(e => e.NumeroComprobante)
                .HasMaxLength(30)
                .HasColumnName("numero_comprobante");
            entity.Property(e => e.TipoComprobante)
                .HasMaxLength(30)
                .HasColumnName("tipo_comprobante");

            entity.HasOne(d => d.ComprasIdcomprasNavigation).WithMany(p => p.Comprobantes)
                .HasForeignKey(d => d.ComprasIdcompras)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comprobante_compras1");
        });

        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.HasKey(e => e.Iddetalle).HasName("PRIMARY");

            entity.ToTable("detalle_compra");

            entity.HasIndex(e => e.ComprasIdcompras, "fk_detalle_compra_compras1_idx");

            entity.HasIndex(e => e.ProductosIdproductos, "fk_detalle_compra_productos1_idx");

            entity.Property(e => e.Iddetalle).HasColumnName("iddetalle");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.ComprasIdcompras).HasColumnName("compras_idcompras");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(10, 2)
                .HasColumnName("precio_unitario");
            entity.Property(e => e.ProductosIdproductos).HasColumnName("productos_idproductos");
            entity.Property(e => e.Subtotal)
                .HasPrecision(10, 2)
                .HasColumnName("subtotal");

            entity.HasOne(d => d.ComprasIdcomprasNavigation).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.ComprasIdcompras)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_compra_compras1");

            entity.HasOne(d => d.ProductosIdproductosNavigation).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.ProductosIdproductos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_compra_productos1");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Idproductos).HasName("PRIMARY");

            entity.ToTable("productos");

            entity.HasIndex(e => e.CategoriaIdcategoria, "fk_productos_categoria_idx");

            entity.Property(e => e.Idproductos).HasColumnName("idproductos");
            entity.Property(e => e.Activo)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CategoriaIdcategoria).HasColumnName("categoria_idcategoria");
            entity.Property(e => e.Descripción)
                .HasMaxLength(200)
                .HasColumnName("descripción");
            entity.Property(e => e.Imagen)
                .HasMaxLength(255)
                .HasColumnName("imagen");
            entity.Property(e => e.Nombre)
                .HasMaxLength(60)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.Talla)
                .HasMaxLength(45)
                .HasColumnName("talla");

            entity.HasOne(d => d.CategoriaIdcategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaIdcategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_productos_categoria");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Idusuarios).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Usuario1, "usuario_UNIQUE").IsUnique();

            entity.Property(e => e.Idusuarios).HasColumnName("idusuarios");
            entity.Property(e => e.Contraseña)
                .HasMaxLength(250)
                .HasColumnName("contraseña");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .HasColumnName("correo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.RolTrabajo)
                .HasMaxLength(50)
                .HasColumnName("rol_trabajo");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
            entity.Property(e => e.TipoUsuario)
                .HasMaxLength(50)
                .HasColumnName("tipo_usuario");
            entity.Property(e => e.Usuario1)
                .HasMaxLength(50)
                .HasColumnName("usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
