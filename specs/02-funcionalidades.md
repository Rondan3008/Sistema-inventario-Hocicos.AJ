# HOCICOS A.J. — Funcionalidades del sistema

## 1. Tienda web

### RF-01. Catálogo de productos
El cliente podrá visualizar la ropa para perros
disponible en la tienda.

### RF-02. Categorías
Los productos podrán organizarse por categorías.

### RF-03. Usuarios
El sistema permitirá gestionar las cuentas
de los usuarios.

### RF-04. Compras
Los clientes podrán realizar compras de productos.

### RF-05. Comprobantes
El sistema permitirá consultar los comprobantes
asociados a las compras.

## 2. Sistema administrativo

### RF-06. Gestión de productos
El administrador podrá registrar, consultar,
actualizar y eliminar productos.

### RF-07. Gestión de categorías
El administrador podrá administrar las categorías.

### RF-08. Gestión de usuarios
El administrador podrá consultar y gestionar
los usuarios registrados.

- Restringir el módulo a usuarios autorizados por la política `Administradores`.
- Permitir consultar y buscar por nombre o nombre de usuario; mostrar ID, nombre, nombre de usuario y `tipo_usuario`.
- Permitir consultar el detalle de una cuenta, sin mostrar la contraseña ni su hash.
- Mantener las consultas en modo solo lectura y conservar las relaciones existentes con compras.
- No ofrecer cambios de rol en esta entrega; por lo tanto, ningún parámetro enviado por el navegador puede modificar `tipo_usuario` ni permitir que el administrador cambie su propio rol.
- La tabla `usuarios` no tiene un campo de estado `activo`. La desactivación o reactivación queda pendiente de una decisión de esquema autorizada; no se elimina físicamente ningún usuario.

### RF-09. Gestión de compras
El administrador podrá consultar las compras
y sus respectivos detalles.

### RF-10. Gestión de comprobantes
El administrador podrá consultar los comprobantes
de las compras.

## 3. Base de datos

El sistema utiliza las seis tablas MySQL existentes. El entorno de desarrollo y pruebas
debe usar exclusivamente `hocicos_aj_test`; `hocicos_aj` no se modifica durante el desarrollo:

- categoria
- usuarios
- productos
- compras
- detalle_compra
- comprobante

## 4. Restricciones

- No modificar las tablas existentes sin autorización.
- Implementar las funcionalidades según las especificaciones.
- Utilizar ASP.NET Core MVC y Entity Framework Core.
- Separar las funcionalidades de clientes y administradores.

## 5. Criterios de aceptación del alcance inicial

### RF-01. Catálogo público

- Mostrar solo productos con stock disponible y permitir filtrar por categoría.
- Mostrar nombre, descripción, talla, precio, categoría y disponibilidad.
- Mostrar un estado vacío cuando no haya productos o la categoría no tenga productos.
- Responder como no encontrada si se solicita una categoría inexistente.
- Mostrar en el catálogo los mensajes de éxito o error producidos por las acciones del carrito.

### RF-02, RF-06 y RF-07. Productos y categorías

- Registrar y editar productos y categorías con validación en servidor.
- Rechazar categorías inexistentes al registrar o editar un producto y nombres de categoría duplicados.
- Mostrar categorías en el catálogo y en los listados administrativos.
- Proteger los formularios POST contra CSRF y no enlazar directamente entidades de base de datos para escritura.
- La eliminación de productos y categorías es lógica: cambiar `activo` de `1` a `0`, sin borrar filas.
- Permitir reactivar registros cambiando `activo` de `0` a `1`; mostrar registros activos e inactivos y su estado en administración.
- Confirmar las acciones de desactivación/reactivación y protegerlas con POST, validación de servidor y antiforgery.
- No desactivar una categoría si tiene productos asociados; informar al administrador y mantener intactos los registros relacionados.
- El catálogo público solo muestra productos activos con stock y cuya categoría también esté activa.

### Seguridad administrativa

- Autenticar cuentas existentes de `usuarios` con `usuario` y hash de contraseña ASP.NET Identity `PasswordHasher<Usuario>`. No comparar contraseñas en texto claro ni actualizar hashes heredados durante el inicio de sesión.
- Aceptar únicamente `tipo_usuario` igual a `Administrador` o `Cliente`, ignorando mayúsculas y espacios externos; emitir el rol canónico correspondiente desde el registro de base de datos.
- No aceptar roles desde formularios públicos ni ofrecer registro público de administradores.
- Proteger el panel con la política `Administradores`, autenticación por cookies seguras y autorización por rol.
- Proteger inicio y cierre de sesión contra CSRF; mostrar un mensaje genérico ante credenciales no válidas.
- Revalidar periódicamente la cuenta y el rol de la cookie contra `usuarios`; revocar cuentas o roles no válidos y aplicar los cambios de rol en un máximo de cinco minutos.
- Crear la primera cuenta administradora con un procedimiento manual autorizado: ejecutar `dotnet run --project HocicosAJ.csproj -- --generar-hash-admin` en el entorno local para introducir y confirmar una contraseña de al menos 12 caracteres sin eco. El comando solo genera un hash compatible (máximo 250 caracteres), no se conecta a MySQL ni inserta usuarios. Un operador autorizado transfiere el hash de forma segura e inserta manualmente el usuario con `tipo_usuario` igual a `Administrador`; no guardar el hash o la contraseña en el repositorio ni en logs.
- Ejemplo para el operador autorizado, sustituyendo los marcadores solo en su cliente MySQL seguro y verificando previamente que `usuario` no exista:

  ```sql
  INSERT INTO usuarios (nombre, usuario, contraseña, tipo_usuario)
  VALUES ('<nombre>', '<usuario>', '<hash generado localmente>', 'Administrador');
  ```

- Las cuentas heredadas cuyas contraseñas no sean hashes verificables de `PasswordHasher<Usuario>` no podrán iniciar sesión hasta que un operador autorizado establezca un hash nuevo mediante el procedimiento aprobado. El sistema no convierte ni modifica credenciales existentes automáticamente.
- No permitir el acceso al panel ni a sus acciones de escritura sin autenticación y autorización de administrador.

### RF-03. Registro y acceso de clientes

- Permitir registrar una cuenta con nombre, usuario único, correo opcional y contraseña confirmada; establecer `tipo_usuario` como `Cliente` exclusivamente en el servidor.
- Almacenar nuevas contraseñas con `PasswordHasher<Usuario>`; exigir mínimo 12 caracteres y no almacenar credenciales en texto claro.
- Reutilizar la autenticación por cookies; un cliente autenticado solo recibe el rol `Cliente` y no puede acceder a rutas protegidas por `Administradores`.
- Proteger registro y cierre de sesión contra CSRF; no ofrecer selección pública de roles.

### RF-04. Carrito previo a la compra

- Restringir el carrito a cuentas autenticadas con rol `Cliente`.
- Mantener productos y cantidades en sesión, sin insertar/modificar/eliminar filas de negocio al agregar, actualizar o retirar una línea.
- Vaciar el carrito al cerrar sesión o al iniciar sesión con una cuenta distinta para no trasladar accidentalmente el carrito entre clientes del mismo navegador.
- Validar en servidor que producto y categoría estén activos y que las cantidades sean positivas y no superen el stock actual.
- Calcular precios y subtotales usando el precio actual de la base de datos; excluir del total líneas que ya no estén disponibles e informar al cliente.
- Proteger todas las acciones POST del carrito contra CSRF.
- El carrito permite confirmar una compra solo después de volver a validar todas sus líneas en servidor.

### RF-04. Confirmación e historial de compras

- Solo clientes autenticados pueden confirmar compras; el cliente se determina por `ClaimTypes.NameIdentifier`, nunca por datos enviados por el navegador.
- En `compras`, `cliente_idusuarios` y `usuarios_idusuarios` refieren ambos al cliente autenticado, de acuerdo con la decisión de negocio aprobada.
- El identificador de compra es AUTO_INCREMENT; EF Core lo configura como generado al insertar.
- El único estado inicial aprobado es `Pendiente`; no implementar cambios de estado ni otras transiciones sin nuevas reglas de negocio.
- Crear compra, detalles y descuentos de stock dentro de una transacción. Antes del registro, verificar existencia, estado activo, categoría activa, cantidad, precio y stock actuales.
- Decrementar el stock mediante actualización condicional y revertir la transacción si cambia el precio, estado o stock concurrentemente.
- Guardar el precio unitario y subtotal del servidor en `detalle_compra`, vaciar el carrito solo tras el commit y mostrar historial/detalle únicamente al cliente propietario.
- Proteger la confirmación con POST y antiforgery; no crear comprobantes ni procesar pagos reales como parte del checkout.

### RF-09. Consulta administrativa de compras

- El administrador puede consultar pedidos mediante la política `Administradores` y filtrar por fechas inclusivas, nombre o nombre de usuario del cliente.
- El listado y el detalle se consultan en modo lectura; el detalle muestra cliente, fecha, estado, totales y las líneas con talla, cantidad, precio unitario y subtotal.
- El administrador puede imprimir un resumen de compra. El resumen declara que no es un comprobante fiscal ni confirma que el pago se haya recibido.
- Esta entrega no modifica compras, detalles, stock ni el estado de pedidos.

### Revisión previa a compras y comprobantes

- La asignación de ambas relaciones obligatorias a `usuarios` y el estado inicial `Pendiente` fueron definidos y documentados en RF-04.
- `comprobante` exige número, tipo, fecha, monto, método de pago y compra; definir quién genera esos datos y el flujo de emisión antes de implementarlo. No se procesan pagos reales ni se inventan datos fiscales.

### RF-05 y RF-10. Consulta de comprobantes

- Los clientes autenticados pueden consultar comprobantes asociados únicamente a compras cuyo `cliente_idusuarios` corresponda a su `ClaimTypes.NameIdentifier`; la consulta por identificador de comprobante debe aplicar esa condición en la base de datos.
- Solo administradores autorizados por la política `Administradores` pueden consultar todos los comprobantes y sus clientes asociados.
- La consulta es de solo lectura. No se crean, editan ni eliminan comprobantes, no se procesan pagos y no se generan datos fiscales.
- El listado permite consultar todos los comprobantes o filtrar por compra; el detalle muestra los campos existentes y permite volver a la compra correspondiente.
- La inspección de solo lectura `SHOW CREATE TABLE comprobante` en `hocicos_aj_test` confirmó `idcomprobante INT NOT NULL AUTO_INCREMENT` y clave primaria; también confirmó número único, campos de tipo/fecha/monto/método de pago y la clave foránea `compras_idcompras` hacia `compras.idcompras`. EF Core lo mapea con `ValueGeneratedOnAdd`.
- La pantalla aclara que los datos almacenados no validan por sí mismos la naturaleza fiscal del comprobante ni confirman la recepción del pago. No se emiten comprobantes.

### Estadísticas descriptivas de compras

- El módulo administrativo es de solo lectura y está protegido por la política `Administradores`. Consulta compras dentro del rango de fechas inclusivo y los detalles asociados mediante sus claves foráneas a productos y categorías.
- Se incluyen todas las compras registradas en el período, también las que están `Pendiente`, porque no se han definido estados finales ni pagos. La suma de `compras.total` representa importes registrados, no pagos confirmados ni ingresos de caja. El módulo no crea ni modifica estados.
- Las unidades, productos y categorías se calculan desde `detalle_compra`. La categoría se toma de la relación actual del producto; como el detalle no conserva una categoría histórica, una reasignación de producto afecta la clasificación histórica mostrada.
- Se muestran total de importes registrados, número de compras, unidades, promedio por compra, producto y categoría con más unidades. En caso de empate, se muestran todos los máximos; sin datos se muestra un estado vacío o `No disponible`.
- Para los importes de `compras.total`, la media es `Σxᵢ/N`, la mediana es el valor central ordenado (o el promedio de los dos centrales), la moda presenta todos los importes con frecuencia máxima si están repetidos, y el rango es `máximo − mínimo`.
- La varianza poblacional es `Σ(xᵢ − μ)²/N` y su desviación estándar es su raíz cuadrada. La varianza muestral usa `Σ(xᵢ − x̄)²/(N − 1)` y su desviación estándar es la raíz cuadrada; ambas medidas muestrales no están disponibles para menos de dos compras. El coeficiente de variación poblacional es `σ/|μ| × 100`; no está disponible si la media es cero.
- La tabla de frecuencias agrupa importes con `k = ⌈1 + log₂(N)⌉` clases (regla de Sturges), muestra frecuencia absoluta, relativa porcentual y acumulada; el intervalo final incluye su límite superior. Los límites de clase se muestran con seis decimales para evitar ocultar la amplitud calculada. Si todos los importes son iguales se usa una sola clase.
- El gráfico de importes por período agrupa por día si se selecciona un rango de hasta 31 días; en los demás casos agrupa por mes. El gráfico por categoría muestra unidades. Ambos usan la paleta existente y no dependen de bibliotecas externas.
- El bloque de gráficas administrativas añade unidades e importes registrados por producto, participación del importe por categoría, histograma de `compras.total`, compras registradas por fecha y frecuencia de métodos consignados en comprobantes. Se consulta en modo lectura con los mismos filtros de fecha; la participación por categoría usa los subtotales de detalle y la relación actual producto-categoría. Los comprobantes muestran métodos consignados, pero no prueban que se haya recibido el pago. No se consultan datos personales de `usuarios`.
- Estas gráficas incluyen todas las compras registradas, independientemente de su estado. Chart.js 4.4.9 se sirve localmente desde `wwwroot/lib/chart.js`; no se usa un CDN. Las gráficas y los indicadores se describen como compras e importes registrados, no como ventas completadas ni ingresos efectivamente percibidos. Si faltan observaciones, la visualización muestra un mensaje informativo.
- Las consultas no crean ni modifican compras, detalles, productos o categorías. No se inventan importes ni valores cuando faltan datos.

### Estadística avanzada y probabilidad (PASO 151)

- Los cuartiles Q1, Q2 y Q3 corresponden a los percentiles 25, 50 y 75. El percentil configurable se calcula por interpolación lineal R-7: `h = (N − 1)p`, interpolando entre los valores ordenados que rodean `h`. Q2 coincide con la mediana.
- La asimetría poblacional es `m₃ / m₂^(3/2)` y la curtosis poblacional se presenta como exceso `m₄ / m₂² − 3`, donde `mᵣ = Σ(xᵢ − μ)ʳ/N`. No se muestran si la varianza es cero. No son estimadores corregidos para muestras.
- El diagrama de caja usa Q1, mediana y Q3; las cercas de Tukey son `Q1 − 1,5 × RIC` y `Q3 + 1,5 × RIC`, con `RIC = Q3 − Q1`. Los bigotes terminan en la observación no atípica más extrema y se señalan por separado los valores fuera de las cercas.
- El histograma reutiliza las clases de Sturges de la tabla de frecuencias. Los gráficos existentes conservan las variables de color y las implementaciones locales; las seis gráficas nuevas usan Chart.js local y la misma paleta.
- El espacio muestral de las probabilidades empíricas son las compras registradas en el período filtrado. `A` es una compra cuyo importe es mayor o igual a la mediana del período; `B` es una compra que contiene al menos una unidad de una categoría con el máximo de unidades vendidas en ese mismo período. Los empates de categoría forman parte de B. `P(A)=n(A)/N`, `P(B)=n(B)/N`, `P(A∩B)=n(A∩B)/N` y `P(A|B)=n(A∩B)/n(B)`; la condicional no está disponible si `n(B)=0`. B usa la relación actual detalle-producto-categoría y no atribuye una categoría histórica que el esquema no conserva.
- Poisson modela el conteo de compras por día para un filtro explícito de hasta 31 días, y por mes en los demás casos. `λ = compras / períodos`, incluyendo los días o meses con cero compras; `E(X)=Var(X)=λ`. Se presentan `P(X=k)`, `P(X≤k)` y `P(X≥k)`. La varianza empírica por período se informa aparte. Se advierte si hay menos de 30 compras o períodos y que independencia, tasa constante y falta de estacionalidad no quedan demostradas.
- La normal para importes se calcula únicamente como escenario teórico con parámetros ingresados; no se ajusta ni se interpreta como una conclusión sobre las compras observadas. Se muestra `Z=(x−μ)/σ`, las probabilidades de cola, el percentil elegido y el intervalo central teórico del 95%.
- El modelo exponencial para tiempos entre compras estima `λ = número de intervalos / tiempo total` en horas solo cuando hay al menos 30 intervalos estrictamente positivos. Fechas iguales o precisión temporal insuficiente impiden ese ajuste. También se permiten parámetros teóricos ingresados, identificados como tales. Se informa `P(T≤t)=1−e^(−λt)`, media `1/λ`, percentil `−ln(1−p)/λ` e intervalo central teórico del 95%; la independencia y tasa constante son supuestos no verificados.
- Cuando no hay observaciones o parámetros suficientes, el módulo muestra `No disponible` y advertencias en vez de fabricar resultados. Todos los filtros y parámetros se validan en el servidor; el endpoint y sus consultas son solo lectura.

#### Consultas SQL de referencia para Base de Datos 1

Las siguientes consultas `SELECT` documentan las extracciones estadísticas equivalentes a las consultas LINQ ejecutadas por el módulo. Son de solo lectura y no se ejecutan automáticamente. Usan parámetros MySQL (`@desde` inclusivo y `@hasta_exclusiva` exclusivo); la segunda usa las claves foráneas existentes y la categoría actual del producto.

1. Resumen de importes con mediana exacta por orden y funciones de ventana (MySQL 8):

   ```sql
   WITH importes_ordenados AS (
       SELECT
           c.idcompras,
           c.total,
           ROW_NUMBER() OVER (ORDER BY c.total, c.idcompras) AS posicion,
           COUNT(*) OVER () AS cantidad
       FROM compras AS c
       WHERE c.fecha >= @desde
         AND c.fecha < @hasta_exclusiva
   )
   SELECT
       COUNT(*) AS numero_compras,
       SUM(total) AS total_importes_registrados,
       AVG(total) AS importe_promedio,
       MIN(total) AS importe_minimo,
       MAX(total) AS importe_maximo,
       AVG(
           CASE
               WHEN posicion IN (FLOOR((cantidad + 1) / 2), FLOOR((cantidad + 2) / 2))
               THEN total
           END
       ) AS mediana
   FROM importes_ordenados;
   ```

2. Unidades por categoría, ordenadas con ranking y asociadas por las relaciones reales:

   ```sql
   WITH unidades_categoria AS (
       SELECT
           cat.idcategoria,
           cat.nombre_categoria,
           SUM(dc.cantidad) AS unidades
       FROM compras AS c
       INNER JOIN detalle_compra AS dc
           ON dc.compras_idcompras = c.idcompras
       INNER JOIN productos AS p
           ON p.idproductos = dc.productos_idproductos
       INNER JOIN categoria AS cat
           ON cat.idcategoria = p.categoria_idcategoria
       WHERE c.fecha >= @desde
         AND c.fecha < @hasta_exclusiva
       GROUP BY cat.idcategoria, cat.nombre_categoria
   ),
   ranking_categoria AS (
       SELECT
           idcategoria,
           nombre_categoria,
           unidades,
           RANK() OVER (ORDER BY unidades DESC) AS posicion
       FROM unidades_categoria
   )
   SELECT idcategoria, nombre_categoria, unidades, posicion
   FROM ranking_categoria
   ORDER BY posicion, nombre_categoria;
   ```

3. Frecuencia de compras por mes, incluyendo meses sin compras, para revisar la tasa usada por Poisson:

   ```sql
   WITH RECURSIVE periodos AS (
       SELECT CAST(DATE_FORMAT(@desde, '%Y-%m-01') AS DATE) AS periodo
       UNION ALL
       SELECT DATE_ADD(periodo, INTERVAL 1 MONTH)
       FROM periodos
       WHERE periodo < CAST(DATE_FORMAT(DATE_SUB(@hasta_exclusiva, INTERVAL 1 DAY), '%Y-%m-01') AS DATE)
   ),
   conteos AS (
       SELECT periodo, COUNT(c.idcompras) AS compras
       FROM periodos
       LEFT JOIN compras AS c
           ON c.fecha >= periodo
          AND c.fecha < DATE_ADD(periodo, INTERVAL 1 MONTH)
          AND c.fecha >= @desde
          AND c.fecha < @hasta_exclusiva
       GROUP BY periodo
   )
   SELECT
       COUNT(*) AS periodos,
       SUM(compras) AS compras_observadas,
       AVG(compras) AS lambda_por_periodo,
       VAR_POP(compras) AS varianza_poblacional_por_periodo
   FROM conteos;
   ```

   Para este ejemplo, `@desde` y `@hasta_exclusiva` deben delimitar al menos un día y `@hasta_exclusiva` debe ser exclusivo. Si el período abarca más de 1000 meses, MySQL puede alcanzar el límite por defecto de recursión; se debe reducir el rango o configurar conscientemente ese límite solo en una sesión de análisis.

### Preparación manual de base de pruebas

- Crear una base vacía e independiente llamada `hocicos_aj_test`. No reutilizar ni limpiar `hocicos_aj`.
- En un equipo con herramientas MySQL configuradas, exportar solo la estructura de las seis tablas, sin filas:

  ```powershell
  mysqldump -h 127.0.0.1 -u <usuario-lector-de-esquema> -p --no-data --skip-triggers hocicos_aj categoria usuarios productos compras detalle_compra comprobante > .\hocicos_aj_test_schema.sql
  ```

- Revisar el archivo exportado y confirmar que solo contiene las definiciones de `categoria`, `usuarios`, `productos`, `compras`, `detalle_compra` y `comprobante`, con sus índices y claves foráneas, y que no selecciona la base original.
- Importar la estructura únicamente en la base vacía de pruebas:

  ```powershell
  mysql -h 127.0.0.1 -u <usuario-de-pruebas> -p hocicos_aj_test < .\hocicos_aj_test_schema.sql
  ```

- Comparar las seis tablas, columnas, tipos, valores por defecto, índices, `AUTO_INCREMENT` y claves foráneas entre origen y destino. No copiar filas de usuarios ni datos reales. Crear cuentas y catálogo de prueba manualmente solo en `hocicos_aj_test`.
- Configurar localmente `ConnectionStrings:HocicosAJ` mediante User Secrets o un gestor de secretos para apuntar exclusivamente a `hocicos_aj_test`. Usar un usuario MySQL con permisos limitados a esa base, comprobar el destino antes de iniciar la aplicación y no guardar credenciales en el repositorio.
- Para comprobar `idcomprobante`, inspeccionar en esa base aislada el `SHOW CREATE TABLE comprobante` y verificar si la definición de `idcomprobante` incluye `AUTO_INCREMENT`. No ejecutar la comprobación en producción ni inferir el atributo únicamente del modelo EF.

### Idempotencia de checkout

- El formulario de confirmación incluye un UUID generado por el servidor, conservado en sesión y ligado a una huella ordenada de los productos y cantidades. Reenviar el mismo formulario conserva la misma clave; cambiar la cesta invalida la clave anterior y genera otra al volver a mostrar el carrito.
- El servidor valida formato, cliente autenticado y vigencia de la clave. Busca una compra ya confirmada para ese cliente y clave antes de comprobar si el carrito está vacío, para que un reintento tras un commit confirmado devuelva la compra existente.
- La compra, sus detalles y los descuentos de stock siguen siendo atómicos en una sola transacción. Se inserta la compra con clave antes de descontar stock; ante cualquier fallo previo al commit se revierte la compra y los descuentos y se conserva la clave para reintentar.
- El índice `UNIQUE` es el árbitro definitivo para solicitudes simultáneas. Solo se recupera como repetición el error MySQL `1062` que identifica específicamente `uq_compras_clave_idempotencia`; después del rollback y de liberar la transacción se vuelve a consultar la clave y solo se redirige a la compra confirmada si pertenece al mismo cliente. Otros errores de MySQL no se tratan como éxito.
- Tras el primer commit, se vacía el carrito solo si la clave y el contenido de la sesión aún coinciden con la operación confirmada. Las rutas de reintento no modifican el carrito.
- SQL aplicado manualmente por el operador en `hocicos_aj_test` y no ejecutado por la aplicación. La columna nullable conserva compras históricas; todas las compras nuevas deben contener una clave:

  ```sql
  ALTER TABLE `hocicos_aj_test`.`compras`
      ADD COLUMN `clave_idempotencia` CHAR(36) NULL,
      ADD UNIQUE KEY `uq_compras_clave_idempotencia` (`clave_idempotencia`);
  ```

- ASP.NET Core Session no ofrece exclusión mutua entre solicitudes concurrentes. La comparación de clave y cesta protege contra modificaciones secuenciales y reintentos, pero no garantiza por sí sola la conservación de una modificación del carrito que se escriba simultáneamente desde otra solicitud mientras el checkout guarda su sesión. La garantía de no perder actualizaciones concurrentes requiere almacenamiento de carrito con control de concurrencia o coordinación compartida; no se debe afirmar esa garantía con el proveedor de sesión actual sin una prueba y solución adicional.

### PASO 155. Tienda visual, panel y personalización

- La portada consulta categorías activas y productos activos con stock cuya categoría también está activa. El buscador del encabezado busca por nombre o descripción en el catálogo real; el catálogo conserva el filtro de categoría y muestra estados vacíos. Los destacados pueden seleccionarse desde administración y solo se aceptan productos disponibles; si no se selecciona ninguno, se muestran productos disponibles reales.
- Los banners secundarios comienzan desactivados y solo se muestran si el administrador los activa. No se muestran calificaciones, descuentos, ventas simuladas, pagos confirmados ni servicios comerciales inventados. Si no se proporcionan imágenes se mantienen espacios visuales locales; no se usan imágenes externas.
- El panel nuevo está protegido por la política `Administradores`. Los indicadores se consultan en modo lectura: productos activos/sin stock, categorías, usuarios, compras e importe registrado en los últimos 30 días. El importe de `compras.total` no equivale a pagos confirmados. Los gráficos reutilizan el servicio estadístico existente.
- El módulo «Personalización de la tienda» permite cambiar título, subtítulo, botón principal, secciones informativas, pie de página, hasta tres banners secundarios, logo, imagen del banner y decoración del panel; también permite asociar imágenes a categorías y seleccionar productos destacados.
- El selector de destino de botones solo permite las rutas locales `/Productos`, `/Carrito` y `/Home/Registro`. Los textos se muestran mediante codificación HTML de Razor, no como HTML arbitrario.
- La personalización se guarda fuera del repositorio y de la carpeta de publicación. De forma predeterminada utiliza `Environment.SpecialFolder.LocalApplicationData/HocicosAJ/storefront`; se puede configurar con `Storefront:StoragePath`, que debe ser una ruta absoluta fuera de `ContentRoot`. `MaximumImageSizeBytes` es configurable entre 1 KiB y 20 MiB; el valor predeterminado es 5 MiB.
- El archivo `storefront.json` contiene los textos, referencias de imágenes, configuración de banners y selección de destacados. Las imágenes se escriben en `uploads/`, con nombres aleatorios generados por el servidor, y se sirven mediante `/store-assets/{nombre}`. Las imágenes de producto utilizan la columna `productos.imagen` ya existente; las imágenes de categorías se asocian por ID en el JSON.
- La escritura del JSON usa un archivo temporal y reemplazo atómico; un bloqueo de archivo coordina escritores y una versión SHA-256 detecta formularios obsoletos para no sobrescribir cambios concurrentes. Si el JSON no es válido, el sistema informa el error en vez de reemplazarlo silenciosamente con valores predeterminados.
- Las cargas requieren autorización administrativa, antiforgery y un límite de tamaño. Se aceptan JPG/JPEG, PNG y WebP solo cuando la extensión coincide con la firma/contenido reconocido; se comprueban estructura de contenedor y dimensiones máximas de 10 000 × 10 000 y 40 millones de píxeles. Los archivos reciben nombres generados y se guardan fuera del árbol publicado. El endpoint de lectura solo sirve nombres de imagen generados y envía `nosniff`.
- Las previsualizaciones de texto e imagen son locales al formulario y no guardan/publican cambios hasta enviar el formulario. La subida de un archivo reemplaza la referencia anterior; los archivos anteriores no se eliminan automáticamente, para evitar dejar referencias rotas. Se deben incluir `storefront.json` y `uploads/` en respaldos y conservar el almacenamiento persistente al desplegar o reemplazar la aplicación.
- El directorio predeterminado es por usuario/servidor y adecuado para una instancia local. En producción con varias instancias se debe configurar un almacenamiento persistente compartido con soporte de bloqueo y reemplazo atómico; una carpeta efímera del contenedor no conserva cambios tras recrearlo.
- No se añadió ni modificó ninguna tabla, relación, migración o dato de MySQL para esta funcionalidad. El CRUD existente de productos sigue escribiendo únicamente en columnas ya mapeadas y la personalización del sitio no escribe en la base de datos.
- La verificación de una vista administrativa y de escritura de imágenes requiere una sesión de administrador autorizada y permisos de escritura en el directorio configurado. No se deben crear pedidos ni datos comerciales para probar el diseño.