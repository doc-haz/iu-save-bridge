<p align="center">
  <img src="assets/banner.png" alt="Editor de partidas de Infinite Undiscovery Recomp" width="100%" />
</p>

# Editor de partidas de Infinite Undiscovery Recomp (IU Save Bridge v2.3.0)

**IU Save Bridge v2.3.0** es el editor de partidas oficial complementario, diseñado específicamente para [**Infinite Undiscovery Recomp**](https://github.com/doc-haz/infinite-undiscovery-recomp) **v1.0.0-rc1**.

Ofrece una interfaz moderna, de alto contraste y estética de fantasía cristalina, con manipulación directa de partidas sin carpetas de paso, copias de seguridad automáticas obligatorias con marca de tiempo y verificación de integridad de doble CRC32 Tri-Ace byte a byte.

---

## Características destacadas

- **Flujo directo y transparente:** `Abrir partida -> Editar -> Guardar cambios`. El editor elimina las carpetas de paso y el empaquetado CON/STFS de la experiencia de usuario, manteniendo el reemplazo atómico de archivos.
- **Copias de seguridad obligatorias:** Antes de modificar cualquier partida activa, se crea automáticamente una copia con marca de tiempo en `backups\<PERFIL>\`. Si la copia falla, la partida queda intacta.
- **Integración nativa con Infinite Undiscovery Recomp:** Detecta instalaciones portables (`<recompRoot>\<PERFIL>\saves\`) para los cinco perfiles actuales: **USA**, **USA-UNDUB**, **EUROPE**, **JAPAN** y **ASIA**.
- **Compatibilidad con carpetas heredadas:** Las instalaciones antiguas que aún usan `NTSC-U\` y `PAL\` se detectan y se mapean en memoria a **USA** y **EUROPE** respectivamente. Las carpetas heredadas se leen y editan en su sitio; nunca se renombran, mueven ni migran automáticamente.
- **Aislamiento estricto de DLC y runtime:** Omite de forma segura directorios DLC y de ejecución (como `0000000000000000\535107DB\00000002`, `cache\` y `Headers\`), apuntando únicamente a ranuras verificadas (`00000001\InfiniteUndiscovery_*.bin`).
- **Exclusión de achievements:** El árbol de logros del Recomp (`saves\achievements\...`) se excluye explícitamente del descubrimiento de partidas (sin distinguir mayúsculas/minúsculas). Puede contener un payload con forma de save en `achievements\535107DB\00000001\...` que **no** es una ranura jugable.
- **Estética Recomp completa:** Cabecera zafiro con detalles dorados, paneles esmerilados semitransparentes, tipografía de alto contraste y arte integrado (`IU_Recomp_Save_Editor_Menu.png`).
- **100% portable:** Cero escrituras en el Registro de Windows, `%APPDATA%` o carpetas de usuario. Los ajustes se guardan en un `config.json` limpio junto al ejecutable.
- **Ejecutable autocontenido:** Icono multirresolución (`.ico`), catálogo completo de 1.023 objetos (`ItemNames.txt`) y arte de menú integrados, sin dependencias externas en tiempo de ejecución.
- **Cambio bilingüe instantáneo:** Alterna en vivo entre inglés (`en`) y español (`es`) con paridad de cadenas del 100% y terminología canónica del juego.

---

## Perfiles y compatibilidad

IU Recomp v1.0.0-rc1 introdujo su modelo final de **perfiles**. El editor habla ese modelo de forma nativa:

| Perfil (código estable) | Nombre en la IU (ES) | Nombre en la IU (EN) | Carpeta heredada |
|---|---|---|---|
| `USA` | USA | USA | `NTSC-U\` |
| `USA-UNDUB` | USA UNDUB (Voces japonesas) | USA UNDUB (Japanese Voices) | — |
| `EUROPE` | Europa | Europe | `PAL\` |
| `JAPAN` | Japón | Japan | — |
| `ASIA` | Asia (Inglés) | Asia (English) | — |

Comportamiento:

- El selector muestra siempre los cinco perfiles soportados e indica cuáles están realmente instalados (existe una carpeta `saves\`).
- `NTSC-U\` se trata como `USA`; `PAL\` se trata como `EUROPE`.
- Si coexisten una carpeta nueva y su equivalente heredada (`USA\` + `NTSC-U\`, o `EUROPE\` + `PAL\`), gana la **nueva**.
- La lógica de migración nunca modifica las carpetas heredadas en disco; el mapeo ocurre solo en memoria.

### Matriz de validación

**VALIDACIÓN REAL** (probada de extremo a extremo contra partidas reales en disco):

| Perfil | Estado |
|---|---|
| `USA` | Validación real (instalación recomp USA real, 20 ranuras) |
| `NTSC-U` (heredado, mapeado a `USA`) | Validación real (instalación NTSC-U heredada real) |

**VALIDACIÓN SINTÉTICA / POR FORMATO** (modelo de perfil y carpetas, solo fixtures sintéticos):

| Perfil | Estado |
|---|---|
| `USA-UNDUB` | Validación sintética / por formato |
| `EUROPE` | Validación sintética / por formato |
| `JAPAN` | Validación sintética / por formato |
| `ASIA` | Validación sintética / por formato |

- El payload `UDSV` de 409.600 bytes, su magia, tamaño, doble CRC32, SHA-256, escritura atómica y copias obligatorias están **validados contra partidas reales de Infinite Undiscovery** (retail NTSC-U, mapeada a `USA`) más fixtures sintéticos.
- **No se ha validado ninguna partida retail real para `USA-UNDUB`, `EUROPE`, `JAPAN` ni `ASIA`.** Esos perfiles se consideran compatibles solo por formato. No asumas que una partida de otra edición es intercambiable solo porque el editor pueda abrirla.

---

## Funciones por pestaña

### 1. Partidas (`Saves`)
- Detección automática de ranuras válidas en el perfil activo del Recomp.
- Metadatos por ranura: número, nombre de carpeta, fecha de modificación, tamaño, Fol actual, hash SHA-256 y estado del doble CRC.
- Vista previa de la miniatura de la partida (`__thumbnail.png`).
- Edición directa de Fol (hasta 99.999.999) con recálculo de doble CRC32 Tri-Ace en tiempo real.
- **Guardar cambios** con un clic: copia de seguridad automática, reemplazo atómico `.tmp` y refresco inmediato de la IU.

### 2. Personajes (`Characters`)
- Soporte completo de los 18 personajes jugables:
  *Capell, Aya, Eugene, Michelle, Kiriya, Sigmund, Edward, Komachi, Rico, Rucha, Kristofer, Balbagan, Touma, Savio, Vic, Gustav, Dominica, Seraphina*.
- Atributos editables por personaje:
  - Nivel (1 - 255) y EXP (0 - 99.999.999)
  - HP actual y máximo (1 - 99.999)
  - MP actual y máximo (1 - 99.999, almacenado a escala binaria x1000)
  - Estadísticas base: ATQ, DEF, PUN, AGI, INT (0 - 9.999)
  - Puntos de acción / PA (0 - 99.999)
  - Indicadores: *En el grupo* y *Grupo activo*
- Acciones rápidas: botón **Estadísticas máximas** (HP/MP a 9999, estadísticas a 999, PA a 10.000).

### 3. Inventario (`Inventory`)
- Base de datos completa de **1.023 objetos** (armas, armaduras, accesorios, consumibles, materiales y grimorios) integrada en el binario.
- Filtro de búsqueda en tiempo real por nombre o ID.
- Casilla "Solo items en posesión".
- Ajuste individual de cantidad (0 - 99) con total actualizado automáticamente.
- Acción masiva: botón **Dar todo x99** para poner los 1.023 objetos a 99.

### 4. Copias de seguridad (`Backups`)
- Repositorio segregado por perfil (`backups\USA\`, `backups\USA-UNDUB\`, `backups\EUROPE\`, `backups\JAPAN\`, `backups\ASIA\`).
- Nombres con marca de tiempo sin colisiones: `InfiniteUndiscovery_<slot>_<yyyyMMdd_HHmmss>[_seq].bin`.
- Metadatos en `.meta` con Fol, fecha, perfil, número de ranura y hash SHA-256.
- Conservación de miniaturas (`.png` junto a la copia).
- **Restaurar copia** con un clic: crea una copia de seguridad obligatoria de la partida activa antes de restaurar.

### 5. Importación de partidas de Xbox 360 (`Archivo > Importar save de Xbox 360...`)
- **Conversión directa CON/STFS:** convierte partidas originales de Xbox 360 al formato portable del Recomp (`<PERFIL>\saves\<USER_ID>\535107DB\00000001\InfiniteUndiscovery_XXXX.bin\InfiniteUndiscovery.dat`).
- **Parser estructural completo:** recorre descriptores y tablas de archivos STFS para extraer `InfiniteUndiscovery.dat` (sin cortes de offset fijos).
- **Extracción de miniatura:** extrae la miniatura PNG original (`__thumbnail.png`) del contenedor STFS.
- **Diálogo de vista previa:** muestra número de ranura, Fol, nivel de Capell, Title ID y estado de checksums antes de escribir.
- **Protección ante conflictos y backup automático:** detecta ranuras de destino existentes y ofrece reemplazar (con backup obligatorio) o renumerar.
- **Integridad de la fuente:** nunca modifica el archivo original de Xbox 360.

### 6. Ajustes (`Settings`)
- Descubrimiento del directorio del Recomp: detección automática de instalaciones portables cerca del editor o selección manual.
- Selector de perfil: cambia entre **USA, USA-UNDUB, EUROPE, JAPAN y ASIA** sin reiniciar. Cada entrada indica si el perfil está instalado.
- Selector de idioma en vivo: cambia entre **English** y **Español** al instante.
- Estado del almacenamiento portable y accesos directos a carpetas.

---

## Estructura de directorios

Un despliegue típico junto a Infinite Undiscovery Recomp v1.0.0-rc1:

```
InfiniteUndiscoveryRecomp\
├── InfiniteUndiscoveryRecomp.exe
├── setup.json
├── USA\
│   └── saves\
│       ├── 0000000000000000\      <- Ignorado estrictamente (DLC y compartido)
│       └── 1234567890ABCDEF\
│           └── 535107DB\
│               └── 00000001\      <- Ranuras detectadas aquí
│                   ├── InfiniteUndiscovery_0001.bin\
│                   │   ├── InfiniteUndiscovery.dat
│                   │   └── __thumbnail.png
│                   └── InfiniteUndiscovery_0002.bin\
├── USA-UNDUB\
│   └── saves\
├── EUROPE\
│   └── saves\
├── JAPAN\
│   └── saves\
├── ASIA\
│   └── saves\
└── IU Save Bridge\
    ├── IU_Save_Bridge.exe         <- Ejecutable autónomo
    ├── config.json                <- Ajustes portables (basados en perfil)
    └── backups\
        ├── USA\
        ├── USA-UNDUB\
        ├── EUROPE\
        ├── JAPAN\
        └── ASIA\
```

Las instalaciones heredadas que aún usan `NTSC-U\` y `PAL\` siguen funcionando:

```
InfiniteUndiscoveryRecomp\
├── NTSC-U\        <- detectada y mapeada a USA (nunca se renombra)
│   └── saves\
└── PAL\           <- detectada y mapeada a EUROPE (nunca se renombra)
    └── saves\
```

---

## Configuración portable (`config.json`)

El editor guarda sus ajustes junto al ejecutable:

```json
{
  "language": "es",
  "recompPath": "C:\\Juegos\\InfiniteUndiscoveryRecomp",
  "profile": "USA"
}
```

Los `config.json` antiguos que aún usan una clave `"region"` se leen de forma transparente y se actualizan en memoria:

- `"region": "NTSC-U"` se trata como `"profile": "USA"`.
- `"region": "PAL"` se trata como `"profile": "EUROPE"`.

La siguiente vez que el editor escriba la configuración, guardará solo el formato nuevo con `"profile"`.

---

## Especificaciones técnicas

- **Tamaño del payload:** exactamente 409.600 bytes (`0x64000`).
- **Firma mágica:** `0x55445356` (`UDSV` en ASCII Big-Endian).
- **Versión de formato:** `0x00000033`.
- **Title ID:** `0x535107DB`.
- **Doble CRC32 (polinomio estándar Tri-Ace `0x04C11DB7`):**
  - **CRC1 (offset `0x14`):** sobre la cabecera (offsets `0x00` a `0xE7`, 232 bytes) con ambos campos CRC a cero durante el cálculo.
  - **CRC2 (offset `0x18`):** sobre los datos de juego (offsets `0xE8` a `0x63FFF`, 409.368 bytes).
- **Plataforma objetivo:** Windows x64 (.NET Framework 4.8 / Windows Forms).

El formato de partida, todos los offsets conocidos y la lógica de CRC permanecen sin cambios respecto a v2.2.0.

---

## Referencia de línea de comandos

`IU_Save_Bridge.exe` admite automatización por línea de comandos:

```bash
# Verifica integridad, magia, versión, Fol, SHA-256 y doble CRC32
IU_Save_Bridge.exe verify <ruta_a_la_partida.dat>

# Modifica Fol directamente desde la terminal con recálculo de CRC
IU_Save_Bridge.exe set-fol <entrada.dat> <salida.dat> <cantidad>

# Muestra la sintaxis y opciones disponibles
IU_Save_Bridge.exe --help
```

---

## Licencia y créditos

- Desarrollado para [Infinite Undiscovery Recomp](https://github.com/doc-haz/infinite-undiscovery-recomp).
- Infinite Undiscovery © Square Enix / tri-Ace.
