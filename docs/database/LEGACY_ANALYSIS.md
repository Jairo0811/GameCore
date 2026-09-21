# Legacy database analysis

## Original scope

The SOF-006 project modeled a video game company using four primary tables:

- `empleados`
- `cliente`
- `videojuego`
- `empresa`

The submission also demonstrated inserts, filters, joins, primary keys, and foreign keys.

## What is preserved

The original SQL is stored in `docs/original/TAREA-FINAL.sql` as historical evidence. It is intentionally not corrected in place.

## Findings

1. **Creation order**
   - `cliente` references `videojuego` before `videojuego` is created.

2. **Company foreign key**
   - `empresa.ID_empleados` exists, but the FK is declared from `empresa.ID` to `empleados.ID`.
   - This does not model the intended relationship.

3. **Customer-to-game relationship**
   - `cliente.ID_v` places one game directly on the customer record.
   - A customer should be able to make many purchases, and one purchase can contain many games.
   - The restoration introduces `Sales` and `SaleDetails`.

4. **Denormalized catalog attributes**
   - `Genero` and `Clasificacion` are free text inside `videojuego`.
   - The restoration separates genres, ratings, and platforms into related tables.

5. **Branch and employee modeling**
   - `empresa.sucursal` is free text and employees are not assigned to a branch through a proper relationship.
   - The restoration introduces `Companies`, `Branches`, and `JobPositions`.

6. **Missing distribution model**
   - The written project concept discussed distribution to countries, but the implemented SQL did not model it.
   - The restoration introduces `Countries` and `Distributions`.

7. **Syntax / DDL issues**
   - Some constraint separators and INSERT syntax are invalid.
   - The destructive `DROP TABLE` sequence also ignores FK dependency order.

8. **Data quality**
   - Some purchase dates precede release dates.
   - Historical rows are preserved in the legacy script, while modern seed data uses internally consistent dates.

## Restoration rule

GameCore does **not** rewrite history. The original submission remains archived. The production schema is a separate, modern interpretation of the same domain.
