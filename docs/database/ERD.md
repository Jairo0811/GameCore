# GameCore relational model

```mermaid
erDiagram
    Companies ||--o{ Branches : has
    Branches ||--o{ Employees : employs
    JobPositions ||--o{ Employees : assigns

    AgeRatings ||--o{ Games : rates
    Games ||--o{ GameGenres : classified_as
    Genres ||--o{ GameGenres : includes
    Games ||--o{ GamePlatforms : released_on
    Platforms ||--o{ GamePlatforms : hosts

    Countries ||--o{ Distributions : receives
    Games ||--o{ Distributions : distributed_as

    Customers ||--o{ Sales : places
    Employees ||--o{ Sales : processes
    Sales ||--|{ SaleDetails : contains
    Games ||--o{ SaleDetails : sold_as
```

## Core relationship correction

The legacy model effectively used:

```text
Customer -> Game
```

GameCore uses:

```text
Customer -> Sale -> SaleDetail -> Game
```

This supports multiple purchases per customer and multiple games per sale.
