<div align="center">

# GameCore

<img src="https://img.shields.io/badge/ITLA-SOF--006-0057B8?style=for-the-badge" alt="ITLA SOF-006" />
<img src="https://img.shields.io/badge/Per%C3%ADodo-2016--C2-0F766E?style=for-the-badge" alt="Período 2016-C2" />
<img src="https://img.shields.io/badge/Estado-Implementaci%C3%B3n%20completa-F59E0B?style=for-the-badge" alt="Implementación completa; validación final pendiente" />

<br/><br/>

<a href="https://github.com/Jairo0811/GameCore/actions/workflows/ci.yml">
  <img src="https://github.com/Jairo0811/GameCore/actions/workflows/ci.yml/badge.svg" alt="CI" />
</a>

<br/><br/>

**Gestión de videojuegos, clientes, ventas, empleados y distribución sobre una reconstrucción moderna de un proyecto de bases de datos.**

</div>

## 📌 Descripción

**GameCore** es la reconstrucción 2026 de un proyecto final desarrollado originalmente para **Introducción a las Bases de Datos (SOF-006)** en el Instituto Tecnológico de Las Américas (ITLA).

El trabajo original se centró en modelado relacional y SQL Server. La reconstrucción conserva el artefacto SQL histórico y extiende el mismo dominio hacia una aplicación full stack con API, autenticación, módulos operativos, consultas avanzadas y frontend web.

---

## 🎓 Información académica

| Información | Detalle |
|---|---|
| 🏫 Institución | **Instituto Tecnológico de Las Américas (ITLA)** |
| 📖 Asignatura | **Introducción a las Bases de Datos (SOF-006)** |
| 👨‍🏫 Profesor | **Freidy Ramón Núñez Pérez** |
| 📅 Período académico | **2016-C2** |
| 👨🏻‍💻 Estudiante | **Francis Jairo Matías Rosario — 2015-2984** |
| 📁 Tipo de entrega | **Proyecto Final** |
| 🛠️ Reconstrucción | **2026** |

El SQL original se conserva en `docs/original/TAREA-FINAL.sql` y no se sobrescribe con datos modernos de demostración.

---

## 🧭 Continuidad académica

### 👨‍🏫 Continuidad por profesor

GameCore comparte profesor y período con [**PySL**](https://github.com/Jairo0811/PySL). Ambos fueron desarrollados durante **2016-C2** bajo la docencia de **Freidy Ramón Núñez Pérez**, pero corresponden a asignaturas y objetivos distintos.

| Orden | Asignatura | Proyecto | Período |
|---:|---|---|---|
| 1 | Fundamentos de Programación (SOF-001) | [**PySL**](https://github.com/Jairo0811/PySL) | 2016-C2 |
| 2 | Introducción a las Bases de Datos (SOF-006) | **GameCore** | 2016-C2 |

La relación es **académica y docente**. No existe dependencia técnica entre ambos repositorios.

---

## 🧱 Stack tecnológico

### 🎨 Frontend

<p>
  <img src="https://skillicons.dev/icons?i=react,ts,vite" alt="React, TypeScript y Vite" />
</p>

- React 19.3
- TypeScript 7
- Vite 8

### ⚙️ Backend

<p>
  <img src="https://skillicons.dev/icons?i=dotnet,cs" alt=".NET y C#" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt="ASP.NET Core Web API" />
</p>

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- JWT Authentication
- Domain / Application / Infrastructure separation

### 🗄️ Datos

<p>
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/microsoftsqlserver/microsoftsqlserver-plain.svg" width="48" height="48" alt="Microsoft SQL Server" />
</p>

- Microsoft SQL Server
- modelo relacional restaurado y ampliado
- consultas y capacidades SQL avanzadas

### 🧪 Calidad y DevOps

<p>
  <img src="https://skillicons.dev/icons?i=docker,git,github,githubactions" alt="Docker, Git, GitHub y GitHub Actions" />
</p>

- Docker
- Git / GitHub
- GitHub Actions
- build y validación automatizada

---

## 🏗️ Arquitectura

```text
React + TypeScript
       ↓
ASP.NET Core Web API
       ↓
Domain / Application / Infrastructure
       ↓
Entity Framework Core
       ↓
GameCoreDB (SQL Server)
```

## 🧩 Módulos

Dashboard · Videojuegos · Clientes · Ventas · Empleados · Distribución · Catálogos · Advanced SQL

---

## 🗺️ Fases de restauración

| Fase | Alcance | Estado |
|---:|---|:---:|
| 0 | Legacy Preservation | ✅ |
| 1 | Database Redesign | ✅ |
| 2 | SQL Server Core | ✅ |
| 3 | Advanced SQL | ✅ |
| 4 | .NET Foundation | ✅ |
| 5 | REST API | ✅ |
| 6 | React Foundation | ✅ |
| 7 | Game Management | ✅ |
| 8 | Customers & Sales | ✅ |
| 9 | Employees & Distribution | ✅ |
| 10 | Dashboard & Reports | ✅ |
| 11 | Security & Validation | ✅ |
| 12 | Portfolio Hardening | ✅ |
| 13 | Release 1.0 implementation | ✅ |

---

## 🚀 Ejecución local

### Base de datos

```powershell
sqlcmd -S localhost -E -i database/setup.sql
```

### Backend

```powershell
dotnet restore src/backend/GameCore.sln
dotnet build src/backend/GameCore.sln -c Release
dotnet run --project src/backend/GameCore.WebAPI/GameCore.WebAPI.csproj
```

### Frontend

```powershell
cd src/frontend
npm install
npm run dev
```

### 🔑 Acceso de demostración

- Email: `admin@gamecore.local`
- Password: `GameCore123!`

Estas credenciales y la clave JWT son valores locales de demostración y deben reemplazarse antes de cualquier despliegue real.

---

## 📚 Preservación académica

El repositorio conserva el material histórico bajo `docs/original/`. La reconstrucción no reutiliza identificadores personales del SQL original como seed público de la aplicación moderna.

---

## 📊 Estado actual

La implementación funcional está completada. La **validación runtime final y la etiqueta real `v1.0.0` permanecen pendientes** hasta completar el pase local documentado en `docs/RELEASE.md`.

<p align="center">
  <strong>GameCore · Del modelo relacional a una aplicación full stack.</strong>
</p>
