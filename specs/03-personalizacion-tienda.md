# HOCICOS A.J. — Tienda y personalización visual (PASO 155)

## Experiencias

- La tienda pública presenta navegación adaptable, búsqueda, categorías activas, productos disponibles, tallas, precios, stock y espacios para imágenes locales. La búsqueda filtra por nombre o descripción y no altera las reglas de visibilidad del catálogo.
- El panel usa una navegación administrativa separada y requiere la política `Administradores`. Sus tarjetas y gráficos se alimentan de consultas reales y de los servicios estadísticos actuales. Los importes son compras registradas; no demuestran que un pago se haya recibido.
- Se mantiene la paleta de marca existente: amarillo `#FFD600`, naranja `#FF8900`, rojo `#D71918`, azul `#0876E4` y blanco `#FFFFFF`, incluidas sus transparencias. No se cargan imágenes desde sitios externos.

## Elementos editables

Desde **Personalización** se pueden cambiar el título/subtítulo/acción del banner principal, la imagen principal, textos de secciones, texto del pie, logo, imagen decorativa del dashboard, imágenes de categorías y hasta tres banners secundarios. Los banners secundarios están desactivados por defecto. El administrador también puede elegir productos activos y con stock para destacar; si no hay selección se muestran productos disponibles.

Los botones solo pueden enlazar a rutas locales aprobadas: `/Productos`, `/Carrito` y `/Home/Registro`. Los valores de texto se codifican al renderizarse y no admiten HTML arbitrario.

La previsualización en la pantalla de edición no persiste cambios. El guardado se realiza explícitamente al enviar el formulario.

## Almacenamiento y despliegue

No se cambia el esquema de MySQL ni se crea una tabla de configuración. De forma predeterminada, la aplicación almacena los archivos bajo:

```text
Environment.SpecialFolder.LocalApplicationData/HocicosAJ/storefront/
├── storefront.json
└── uploads/
```

En Windows, `LocalApplicationData` normalmente corresponde a `%LOCALAPPDATA%`. Para un servidor, contenedor o despliegue se recomienda configurar `Storefront:StoragePath` con una ruta persistente absoluta fuera del directorio publicado. La aplicación rechaza rutas relativas o rutas dentro de `ContentRoot`.

`Storefront:MaximumImageSizeBytes` configura el máximo por imagen; el valor predeterminado es 5 MiB y el rango permitido es 1 KiB–20 MiB. El JSON se reemplaza mediante archivo temporal, bloqueo exclusivo y comprobación de versión para evitar sobrescrituras por formularios obsoletos. Los archivos subidos reciben nombres aleatorios del servidor y se publican desde `/store-assets/`.

Respaldar conjuntamente `storefront.json` y `uploads/`. Al desplegar una versión nueva, montar/restaurar la misma ubicación persistente y otorgar permisos de lectura/escritura al proceso. Para varias instancias, utilizar una ubicación compartida cuyo sistema de archivos respete bloqueos exclusivos y reemplazo atómico. La aplicación no borra automáticamente imágenes antiguas al reemplazarlas para evitar romper referencias existentes; el almacenamiento requiere seguimiento y política de limpieza/retención.

## Seguridad de archivos

- Las acciones de creación y modificación requieren el rol de administrador y antiforgery.
- El servidor impone el límite de tamaño, valida la extensión junto con el contenido real del contenedor JPG/PNG/WebP y limita las dimensiones/píxeles aceptados.
- Se ignoran nombres enviados para almacenamiento: el servidor genera nombres aleatorios y no acepta rutas arbitrarias.
- El endpoint público de imágenes solo resuelve nombres de archivo del formato generado y entrega un tipo MIME fijo con `X-Content-Type-Options: nosniff`.
- Las referencias del JSON deben usar `/store-assets/` y nombres generados. Una configuración inválida no se sustituye silenciosamente.
- Las imágenes son públicas porque forman parte del escaparate; la escritura y la administración de referencias son exclusivas del panel.

## Limitaciones

- Las categorías no tienen una columna de imagen, por lo que su ruta se guarda en el JSON y se asocia mediante el ID actual. El logo personalizado es una imagen presentada con `object-fit: contain`; no se altera su archivo ni su paleta.
- El módulo de personalización no guarda contenido en MySQL y no permite incrustar HTML ni destinos externos.
- Los banners secundarios son espacios de presentación; no representan ofertas ni servicios predefinidos y requieren texto/imagen y activación explícita del administrador.
- Los archivos de imagen antiguos no se eliminan automáticamente. Las copias de seguridad deben contemplar su crecimiento.
- La verificación funcional de rutas administrativas y cargas requiere una sesión administrativa válida. Sin ella, solo se puede comprobar que la autorización redirige/rechaza el acceso.
- No hay datos ni imágenes de referencia en el repositorio para validar el resultado contra maquetas proporcionadas externamente. Se utilizan espacios visuales reemplazables y no imágenes ficticias.

## Comprobación segura

1. Iniciar la aplicación con `ConnectionStrings:HocicosAJ` apuntando a `hocicos_aj_test` en Development.
2. Abrir la portada y catálogo, buscar por texto, probar categorías y confirmar estados vacíos sin modificar datos.
3. Iniciar sesión con una cuenta administradora ya autorizada y visitar el panel y **Personalización**.
4. Previsualizar textos y archivos locales; verificar el formato/tamaño en el servidor. No es necesario crear pedidos.
5. Guardar una personalización solo con autorización y en un directorio de prueba seguro. Reiniciar y comprobar que el JSON y las imágenes persisten.
6. Confirmar que un cliente y una sesión anónima no pueden acceder a acciones administrativas.
7. No ejecutar migraciones ni operaciones de escritura SQL para estas comprobaciones.
