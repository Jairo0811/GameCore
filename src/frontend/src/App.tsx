import { FormEvent, ReactNode, useCallback, useEffect, useMemo, useState } from 'react'
import { api, login } from './api'

type Section='dashboard'|'games'|'customers'|'sales'|'employees'|'distributions'|'reports'
type IconName='dashboard'|'game'|'users'|'cart'|'briefcase'|'globe'|'money'|'refresh'|'chart'|'clock'|'reports'|'search'|'plus'|'edit'|'close'|'user'|'chevron'
type Session={token:string,email:string,role:string}
type Row=Record<string,unknown>

type DashboardData={
  games:number;customers:number;sales:number;employees:number;revenue:number
  topGames:Array<{gameId:number,title:string,unitsSold:number,revenue:number}>
  monthlySales:Array<{year:number,month:number,salesCount:number,revenue:number}>
  recentSales:Array<{saleId:number,saleDate:string,customer:string,total:number}>
  distributionByCountry:Array<{countryId:number,country:string,units:number}>
}

type Catalogs={
  genres:Array<{genreId:number,name:string}>
  platforms:Array<{platformId:number,name:string}>
  ratings:Array<{ageRatingId:number,code:string,name:string}>
  countries:Array<{countryId:number,name:string}>
  branches:Array<{branchId:number,name:string}>
  positions:Array<{jobPositionId:number,name:string}>
}

type ReportsData={
  monthlySales:Row[]
  topGames:Row[]
  customers:Row[]
  countries:Row[]
}

type Toast={id:number,message:string,tone:'success'|'error'}

const sections:Array<[Section,string,IconName,string]>= [
  ['dashboard','Dashboard','dashboard','/dashboard'],
  ['games','Videojuegos','game','/games'],
  ['customers','Clientes','users','/customers'],
  ['sales','Ventas','cart','/sales'],
  ['employees','Empleados','briefcase','/employees'],
  ['distributions','Distribución','globe','/distributions'],
  ['reports','Reportes','reports','/reports']
]

const descriptions:Record<Section,string>={
  dashboard:'Resumen general del sistema',
  games:'Administra el catálogo de videojuegos',
  customers:'Gestiona la información de tus clientes',
  sales:'Consulta y registra las operaciones comerciales',
  employees:'Gestiona el equipo y sus posiciones',
  distributions:'Controla la distribución de títulos por país',
  reports:'Analiza el rendimiento comercial y operativo'
}

const routeToSection=():Section=>{
  const path=window.location.pathname.toLowerCase()
  return sections.find(([, , ,route])=>route===path)?.[0]??'dashboard'
}

export default function App(){
  const stored=sessionStorage.getItem('gamecore_session')
  const [session,setSession]=useState<Session|null>(stored?JSON.parse(stored):null)
  const [section,setSection]=useState<Section>(routeToSection())
  const [data,setData]=useState<unknown>(null)
  const [catalogs,setCatalogs]=useState<Catalogs|null>(null)
  const [loading,setLoading]=useState(false)
  const [error,setError]=useState('')
  const [drawer,setDrawer]=useState<{section:Section,row?:Row}|null>(null)
  const [toasts,setToasts]=useState<Toast[]>([])
  const [profileOpen,setProfileOpen]=useState(false)

  const notify=useCallback((message:string,tone:Toast['tone']='success')=>{
    const id=Date.now()+Math.random()
    setToasts(current=>[...current,{id,message,tone}])
    window.setTimeout(()=>setToasts(current=>current.filter(x=>x.id!==id)),3200)
  },[])

  const navigate=useCallback((next:Section)=>{
    const route=sections.find(x=>x[0]===next)?.[3]??'/dashboard'
    window.history.pushState({},'',route)
    setSection(next)
    setProfileOpen(false)
  },[])

  useEffect(()=>{
    const onPop=()=>setSection(routeToSection())
    window.addEventListener('popstate',onPop)
    if(window.location.pathname==='/'&&session)navigate('dashboard')
    return()=>window.removeEventListener('popstate',onPop)
  },[navigate,session])

  const load=useCallback(async()=>{
    if(!session)return
    setLoading(true);setError('')
    try{
      setData(await api(`/api/${section}`,session.token))
    }catch(e){
      setError(e instanceof Error?e.message:'Error inesperado')
    }finally{
      setLoading(false)
    }
  },[section,session])

  useEffect(()=>{void load()},[load])

  useEffect(()=>{
    if(!session)return
    void api<Catalogs>('/api/catalogs',session.token).then(setCatalogs).catch(()=>undefined)
  },[session])

  if(!session)return <Login onSession={value=>{
    sessionStorage.setItem('gamecore_session',JSON.stringify(value))
    window.history.replaceState({},'', '/dashboard')
    setSection('dashboard')
    setSession(value)
  }}/>

  const current=sections.find(x=>x[0]===section)

  const logout=()=>{
    sessionStorage.removeItem('gamecore_session')
    window.history.replaceState({},'', '/')
    setSession(null)
  }

  const saveComplete=async(message:string)=>{
    setDrawer(null)
    notify(message)
    await load()
  }

  return <div className="app-shell">
    <aside>
      <div className="brand"><img src="/brand/gamecore-logo.png" alt="GameCore" className="brand-logo"/></div>
      <nav aria-label="Navegación principal">
        {sections.map(([key,label,icon])=>
          <button key={key} className={section===key?'active':''} onClick={()=>navigate(key)} aria-current={section===key?'page':undefined}>
            <Icon name={icon}/><span>{label}</span>
          </button>
        )}
      </nav>
      <div className="brand-signature">PLAY · STORE · MANAGE · GROW</div>
    </aside>

    <main>
      <header className="app-header">
        <div>
          <p className="eyebrow">GAMECORE · CONTROL CENTER</p>
          <h1>{current?.[1]}</h1>
          <p className="header-subtitle">{descriptions[section]}</p>
        </div>
        <div className="header-actions">
          <button className="refresh-button" onClick={()=>void load()}><Icon name="refresh"/><span>Actualizar</span></button>
          <div className="profile-wrap">
            <button className="profile-button" onClick={()=>setProfileOpen(v=>!v)} aria-expanded={profileOpen}>
              <span className="profile-avatar"><Icon name="user"/></span>
              <span className="profile-copy"><strong>{session.role}</strong><small>{session.email}</small></span>
              <Icon name="chevron"/>
            </button>
            {profileOpen&&<div className="profile-menu">
              <div><strong>{session.role}</strong><small>{session.email}</small></div>
              <button onClick={logout}>Cerrar sesión</button>
            </div>}
          </div>
        </div>
      </header>

      {error&&<div className="error" role="alert">{error}</div>}
      {loading?<Skeleton/>:
        <View
          section={section}
          data={data}
          token={session.token}
          navigate={navigate}
          onCreate={()=>setDrawer({section})}
          onEdit={row=>setDrawer({section,row})}
        />
      }

      <footer className="app-footer">Proyecto restaurado · SOF-006 · ITLA · 2016 → 2026</footer>
    </main>

    {drawer&&<ResourceDrawer
      section={drawer.section}
      row={drawer.row}
      token={session.token}
      catalogs={catalogs}
      onClose={()=>setDrawer(null)}
      onSaved={saveComplete}
      onError={message=>notify(message,'error')}
    />}

    <div className="toast-stack" aria-live="polite">
      {toasts.map(t=><div key={t.id} className={`toast ${t.tone}`}>{t.message}</div>)}
    </div>
  </div>
}

function Login({onSession}:{onSession:(session:Session)=>void}){
  const [email,setEmail]=useState('admin@gamecore.local')
  const [password,setPassword]=useState('GameCore123!')
  const [error,setError]=useState('')
  const [busy,setBusy]=useState(false)

  async function submit(e:FormEvent){
    e.preventDefault();setBusy(true);setError('')
    try{onSession(await login(email,password))}
    catch(err){setError(err instanceof Error?err.message:'No se pudo iniciar sesión')}
    finally{setBusy(false)}
  }

  return <div className="login-page">
    <section className="login-hero" aria-label="GameCore — Database + Gaming">
      <div className="login-hero-shade"/>
      <div className="login-hero-badges"><span>SQL Server</span><span>.NET 10</span><span>React</span></div>
      <div className="login-hero-caption">
        <span className="hero-kicker">DATABASE + GAMING</span>
        <strong>Datos que impulsan el juego.</strong>
        <small>PLAY · STORE · MANAGE · GROW</small>
      </div>
    </section>
    <section className="login-access">
      <form className="login-card" onSubmit={submit}>
        <div className="login-card-brand"><img src="/brand/gamecore-icon.svg" className="login-icon" alt="GameCore"/><div><span>GAMECORE</span><small>VIDEO GAME MANAGEMENT SYSTEM</small></div></div>
        <div className="login-heading"><h1>Bienvenido</h1><p>Accede a tu centro de gestión.</p></div>
        <label>Correo<input type="email" autoComplete="username" value={email} onChange={e=>setEmail(e.target.value)} required/></label>
        <label>Contraseña<input type="password" autoComplete="current-password" value={password} onChange={e=>setPassword(e.target.value)} required/></label>
        {error&&<div className="error" role="alert">{error}</div>}
        <button type="submit" disabled={busy}><span>{busy?'Accediendo…':'Entrar a GameCore'}</span><span aria-hidden="true">→</span></button>
        <div className="login-demo-note"><span/><small>Cuenta demo local para validación del proyecto.</small><span/></div>
      </form>
      <footer className="login-footer"><span>GameCore</span><span>•</span><span>SOF-006</span><span>•</span><span>Restauración 2026</span></footer>
    </section>
  </div>
}

function View({section,data,token,navigate,onCreate,onEdit}:{section:Section,data:unknown,token:string,navigate:(s:Section)=>void,onCreate:()=>void,onEdit:(row:Row)=>void}){
  if(section==='dashboard')return <Dashboard data={data as DashboardData|null} navigate={navigate}/>
  if(section==='reports')return <Reports data={data as ReportsData|null}/>
  return <ResourceList section={section} rows={Array.isArray(data)?data as Row[]:[]} onCreate={onCreate} onEdit={onEdit} token={token}/>
}

function Dashboard({data,navigate}:{data:DashboardData|null,navigate:(s:Section)=>void}){
  if(!data)return <EmptyState title="Sin datos" text="No hay información disponible para el dashboard."/>

  return <div className="dashboard">
    <div className="cards">
      <Metric icon="game" label="Videojuegos" value={data.games} detail="Títulos activos" onClick={()=>navigate('games')}/>
      <Metric icon="users" label="Clientes" value={data.customers} detail="Registrados" onClick={()=>navigate('customers')}/>
      <Metric icon="cart" label="Ventas" value={data.sales} detail="Completadas" onClick={()=>navigate('sales')}/>
      <Metric icon="briefcase" label="Empleados" value={data.employees} detail="Activos" onClick={()=>navigate('employees')}/>
      <Metric icon="money" label="Ingresos" value={formatMoney(data.revenue)} detail="Ventas completadas" accent onClick={()=>navigate('reports')}/>
    </div>

    <div className="dashboard-grid">
      <section className="panel chart-panel"><PanelTitle icon="chart" title="Ingresos por período" subtitle="Evolución de las ventas registradas"/><RevenueChart rows={data.monthlySales}/></section>
      <section className="panel chart-panel"><PanelTitle icon="globe" title="Distribución por país" subtitle="Unidades distribuidas"/><DistributionChart rows={data.distributionByCountry}/></section>
    </div>

    <div className="dashboard-grid bottom-grid">
      <section className="panel"><PanelTitle icon="game" title="Juegos más vendidos" subtitle="Rendimiento del catálogo"/><Table rows={data.topGames as unknown as Row[]}/></section>
      <section className="panel"><PanelTitle icon="clock" title="Ventas recientes" subtitle="Últimas operaciones completadas"/><Table rows={data.recentSales as unknown as Row[]}/></section>
    </div>
  </div>
}

function Reports({data}:{data:ReportsData|null}){
  if(!data)return <EmptyState title="Sin reportes" text="No hay información disponible para generar reportes."/>
  return <div className="reports-grid">
    <section className="panel"><PanelTitle icon="chart" title="Ventas mensuales" subtitle="Transacciones e ingresos por período"/><Table rows={data.monthlySales}/></section>
    <section className="panel"><PanelTitle icon="game" title="Rendimiento por videojuego" subtitle="Unidades e ingresos por título"/><Table rows={data.topGames}/></section>
    <section className="panel"><PanelTitle icon="users" title="Valor por cliente" subtitle="Compras acumuladas y valor de vida"/><Table rows={data.customers}/></section>
    <section className="panel"><PanelTitle icon="globe" title="Distribución geográfica" subtitle="Unidades y títulos por país"/><Table rows={data.countries}/></section>
  </div>
}

function ResourceList({section,rows,onCreate,onEdit}:{section:Section,rows:Row[],onCreate:()=>void,onEdit:(row:Row)=>void,token:string}){
  const [query,setQuery]=useState('')
  const [status,setStatus]=useState('all')
  const [page,setPage]=useState(1)
  const pageSize=8

  useEffect(()=>setPage(1),[query,status,section])

  const filtered=useMemo(()=>rows.filter(row=>{
    const haystack=Object.values(row).flatMap(v=>Array.isArray(v)?v:[v]).join(' ').toLowerCase()
    const matches=haystack.includes(query.toLowerCase())
    if(status==='all')return matches
    if(section==='games'||section==='employees')return matches&&String(row.isActive)===status
    if(section==='sales')return matches&&String(row.status).toLowerCase()===status
    return matches
  }),[rows,query,status,section])

  const pages=Math.max(Math.ceil(filtered.length/pageSize),1)
  const pageRows=filtered.slice((page-1)*pageSize,page*pageSize)
  const canCreate=section!=='reports'
  const canEdit=['games','customers','employees','distributions'].includes(section)

  return <div className="resource-stack">
    <div className="resource-toolbar">
      <div className="search-box"><Icon name="search"/><input value={query} onChange={e=>setQuery(e.target.value)} placeholder={`Buscar en ${descriptions[section].toLowerCase()}…`} aria-label="Buscar"/></div>
      <div className="toolbar-actions">
        {(section==='games'||section==='employees'||section==='sales')&&
          <select value={status} onChange={e=>setStatus(e.target.value)} aria-label="Filtrar por estado">
            <option value="all">Todos los estados</option>
            {section==='sales'?<><option value="completed">Completadas</option><option value="pending">Pendientes</option><option value="cancelled">Canceladas</option></>:<><option value="true">Activos</option><option value="false">Inactivos</option></>}
          </select>}
        {canCreate&&<button className="primary-action" onClick={onCreate}><Icon name="plus"/><span>{createLabel(section)}</span></button>}
      </div>
    </div>

    <div className="panel data-panel">
      {pageRows.length?<Table rows={pageRows} actions={canEdit?row=><button className="icon-action" onClick={()=>onEdit(row)} aria-label="Editar"><Icon name="edit"/></button>:undefined}/>:<EmptyState title="Sin resultados" text="Ajusta la búsqueda o crea un nuevo registro."/>}
    </div>

    <div className="pagination">
      <span>{filtered.length} registro{filtered.length===1?'':'s'}</span>
      <div>
        <button onClick={()=>setPage(p=>Math.max(1,p-1))} disabled={page===1}>‹</button>
        <span>Página {page} de {pages}</span>
        <button onClick={()=>setPage(p=>Math.min(pages,p+1))} disabled={page===pages}>›</button>
      </div>
    </div>
  </div>
}

function ResourceDrawer({section,row,token,catalogs,onClose,onSaved,onError}:{section:Section,row?:Row,token:string,catalogs:Catalogs|null,onClose:()=>void,onSaved:(message:string)=>Promise<void>,onError:(message:string)=>void}){
  const [busy,setBusy]=useState(false)
  const [options,setOptions]=useState<{customers:Row[],employees:Row[],games:Row[]}>({customers:[],employees:[],games:[]})

  useEffect(()=>{
    if(section!=='sales')return
    void Promise.all([
      api<Row[]>('/api/customers',token),
      api<Row[]>('/api/employees',token),
      api<Row[]>('/api/games',token)
    ]).then(([customers,employees,games])=>setOptions({customers,employees,games})).catch(()=>undefined)
  },[section,token])

  async function submit(payload:unknown){
    setBusy(true)
    try{
      if(section==='games'){
        const id=row?.gameId
        await api(id?`/api/games/${id}`:'/api/games',token,{method:id?'PUT':'POST',body:JSON.stringify(payload)})
      }else if(section==='customers'){
        const id=row?.customerId
        await api(id?`/api/customers/${id}`:'/api/customers',token,{method:id?'PUT':'POST',body:JSON.stringify(payload)})
      }else if(section==='employees'){
        const id=row?.employeeId
        await api(id?`/api/employees/${id}`:'/api/employees',token,{method:id?'PUT':'POST',body:JSON.stringify(payload)})
      }else if(section==='distributions'){
        await api('/api/distributions',token,{method:'POST',body:JSON.stringify(payload)})
      }else if(section==='sales'){
        await api('/api/sales',token,{method:'POST',body:JSON.stringify(payload)})
      }
      await onSaved(row?'Cambios guardados correctamente.':'Registro creado correctamente.')
    }catch(e){onError(e instanceof Error?e.message:'No se pudo guardar el registro.')}
    finally{setBusy(false)}
  }

  return <div className="drawer-backdrop" role="presentation" onMouseDown={e=>{if(e.target===e.currentTarget)onClose()}}>
    <aside className="drawer" role="dialog" aria-modal="true" aria-label={row?'Editar registro':'Nuevo registro'}>
      <div className="drawer-header">
        <div><span>{row?'Editar':'Nuevo'}</span><h2>{resourceTitle(section)}</h2></div>
        <button className="icon-action" onClick={onClose} aria-label="Cerrar"><Icon name="close"/></button>
      </div>
      <div className="drawer-body">
        {section==='games'&&<GameForm row={row} catalogs={catalogs} busy={busy} onSubmit={submit}/>}
        {section==='customers'&&<CustomerForm row={row} busy={busy} onSubmit={submit}/>}
        {section==='employees'&&<EmployeeForm row={row} catalogs={catalogs} busy={busy} onSubmit={submit}/>}
        {section==='distributions'&&<DistributionForm row={row} catalogs={catalogs} games={options.games} token={token} busy={busy} onSubmit={submit}/>}
        {section==='sales'&&<SaleForm options={options} busy={busy} onSubmit={submit}/>}
      </div>
    </aside>
  </div>
}

function GameForm({row,catalogs,busy,onSubmit}:{row?:Row,catalogs:Catalogs|null,busy:boolean,onSubmit:(p:unknown)=>void}){
  const [title,setTitle]=useState(String(row?.title??''))
  const [story,setStory]=useState(String(row?.story??''))
  const [releaseDate,setReleaseDate]=useState(String(row?.releaseDate??'').slice(0,10))
  const [unitPrice,setUnitPrice]=useState(Number(row?.unitPrice??0))
  const [ageRatingId,setAgeRatingId]=useState(Number(row?.ageRatingId??0))
  const [genreIds,setGenreIds]=useState<number[]>((row?.genreIds as number[]|undefined)??[])
  const [platformIds,setPlatformIds]=useState<number[]>((row?.platformIds as number[]|undefined)??[])
  const [isActive,setIsActive]=useState(row?.isActive===undefined?true:Boolean(row.isActive))

  return <form className="resource-form" onSubmit={e=>{e.preventDefault();onSubmit({title,story:story||null,releaseDate:releaseDate||null,unitPrice,ageRatingId:ageRatingId||null,genreIds,platformIds,isActive})}}>
    <Field label="Título"><input value={title} onChange={e=>setTitle(e.target.value)} required/></Field>
    <Field label="Historia / descripción"><textarea value={story} onChange={e=>setStory(e.target.value)} rows={4}/></Field>
    <div className="form-grid"><Field label="Lanzamiento"><input type="date" value={releaseDate} onChange={e=>setReleaseDate(e.target.value)}/></Field><Field label="Precio"><input type="number" min="0" step="0.01" value={unitPrice} onChange={e=>setUnitPrice(Number(e.target.value))} required/></Field></div>
    <Field label="Clasificación"><select value={ageRatingId} onChange={e=>setAgeRatingId(Number(e.target.value))}><option value={0}>Sin clasificación</option>{catalogs?.ratings.map(x=><option key={x.ageRatingId} value={x.ageRatingId}>{x.code} · {x.name}</option>)}</select></Field>
    <CheckGroup label="Géneros" options={catalogs?.genres.map(x=>({id:x.genreId,name:x.name}))??[]} value={genreIds} onChange={setGenreIds}/>
    <CheckGroup label="Plataformas" options={catalogs?.platforms.map(x=>({id:x.platformId,name:x.name}))??[]} value={platformIds} onChange={setPlatformIds}/>
    {row&&<label className="switch-row"><input type="checkbox" checked={isActive} onChange={e=>setIsActive(e.target.checked)}/><span>Videojuego activo</span></label>}
    <Submit busy={busy} editing={Boolean(row)}/>
  </form>
}

function CustomerForm({row,busy,onSubmit}:{row?:Row,busy:boolean,onSubmit:(p:unknown)=>void}){
  const [firstName,setFirstName]=useState(String(row?.firstName??''))
  const [lastName,setLastName]=useState(String(row?.lastName??''))
  const [phone,setPhone]=useState(String(row?.phone??''))
  const [email,setEmail]=useState(String(row?.email??''))
  return <form className="resource-form" onSubmit={e=>{e.preventDefault();onSubmit({firstName,lastName,phone:phone||null,email:email||null})}}>
    <div className="form-grid"><Field label="Nombre"><input value={firstName} onChange={e=>setFirstName(e.target.value)} required/></Field><Field label="Apellido"><input value={lastName} onChange={e=>setLastName(e.target.value)} required/></Field></div>
    <Field label="Teléfono"><input value={phone} onChange={e=>setPhone(e.target.value)}/></Field>
    <Field label="Correo"><input type="email" value={email} onChange={e=>setEmail(e.target.value)}/></Field>
    <Submit busy={busy} editing={Boolean(row)}/>
  </form>
}

function EmployeeForm({row,catalogs,busy,onSubmit}:{row?:Row,catalogs:Catalogs|null,busy:boolean,onSubmit:(p:unknown)=>void}){
  const [branchId,setBranchId]=useState(Number(row?.branchId??catalogs?.branches[0]?.branchId??0))
  const [jobPositionId,setJobPositionId]=useState(Number(row?.jobPositionId??catalogs?.positions[0]?.jobPositionId??0))
  const [firstName,setFirstName]=useState(String(row?.firstName??''))
  const [lastName,setLastName]=useState(String(row?.lastName??''))
  const [phone,setPhone]=useState(String(row?.phone??''))
  const [email,setEmail]=useState(String(row?.email??''))
  const [addressLine,setAddressLine]=useState(String(row?.addressLine??''))
  const [isActive,setIsActive]=useState(row?.isActive===undefined?true:Boolean(row.isActive))

  return <form className="resource-form" onSubmit={e=>{e.preventDefault();onSubmit({branchId,jobPositionId,firstName,lastName,phone:phone||null,email:email||null,addressLine:addressLine||null,isActive})}}>
    <div className="form-grid"><Field label="Nombre"><input value={firstName} onChange={e=>setFirstName(e.target.value)} required/></Field><Field label="Apellido"><input value={lastName} onChange={e=>setLastName(e.target.value)} required/></Field></div>
    <div className="form-grid">
      <Field label="Sucursal"><select value={branchId} onChange={e=>setBranchId(Number(e.target.value))}>{catalogs?.branches.map(x=><option key={x.branchId} value={x.branchId}>{x.name}</option>)}</select></Field>
      <Field label="Puesto"><select value={jobPositionId} onChange={e=>setJobPositionId(Number(e.target.value))}>{catalogs?.positions.map(x=><option key={x.jobPositionId} value={x.jobPositionId}>{x.name}</option>)}</select></Field>
    </div>
    <Field label="Correo"><input type="email" value={email} onChange={e=>setEmail(e.target.value)}/></Field>
    <Field label="Teléfono"><input value={phone} onChange={e=>setPhone(e.target.value)}/></Field>
    <Field label="Dirección"><input value={addressLine} onChange={e=>setAddressLine(e.target.value)}/></Field>
    <label className="switch-row"><input type="checkbox" checked={isActive} onChange={e=>setIsActive(e.target.checked)}/><span>Empleado activo</span></label>
    <Submit busy={busy} editing={Boolean(row)}/>
  </form>
}

function DistributionForm({row,catalogs,games,token,busy,onSubmit}:{row?:Row,catalogs:Catalogs|null,games:Row[],token:string,busy:boolean,onSubmit:(p:unknown)=>void}){
  const [gameOptions,setGameOptions]=useState<Row[]>(games)
  useEffect(()=>{if(gameOptions.length===0)void api<Row[]>('/api/games',token).then(setGameOptions)},[gameOptions.length,token])
  const [gameId,setGameId]=useState(Number(row?.gameId??0))
  const [countryId,setCountryId]=useState(Number(row?.countryId??0))
  const [distributionDate,setDistributionDate]=useState(String(row?.distributionDate??new Date().toISOString()).slice(0,10))
  const [units,setUnits]=useState(Number(row?.units??0))

  return <form className="resource-form" onSubmit={e=>{e.preventDefault();onSubmit({gameId,countryId,distributionDate,units})}}>
    <Field label="Videojuego"><select value={gameId} onChange={e=>setGameId(Number(e.target.value))} required><option value={0}>Selecciona…</option>{gameOptions.map(x=><option key={Number(x.gameId)} value={Number(x.gameId)}>{String(x.title)}</option>)}</select></Field>
    <Field label="País"><select value={countryId} onChange={e=>setCountryId(Number(e.target.value))} required><option value={0}>Selecciona…</option>{catalogs?.countries.map(x=><option key={x.countryId} value={x.countryId}>{x.name}</option>)}</select></Field>
    <div className="form-grid"><Field label="Fecha"><input type="date" value={distributionDate} onChange={e=>setDistributionDate(e.target.value)} required/></Field><Field label="Unidades"><input type="number" min="0" value={units} onChange={e=>setUnits(Number(e.target.value))} required/></Field></div>
    <Submit busy={busy} editing={Boolean(row)}/>
  </form>
}

function SaleForm({options,busy,onSubmit}:{options:{customers:Row[],employees:Row[],games:Row[]},busy:boolean,onSubmit:(p:unknown)=>void}){
  const [customerId,setCustomerId]=useState(0)
  const [employeeId,setEmployeeId]=useState(0)
  const [items,setItems]=useState<Array<{gameId:number,quantity:number}>>([{gameId:0,quantity:1}])

  const change=(index:number,key:'gameId'|'quantity',value:number)=>setItems(current=>current.map((x,i)=>i===index?{...x,[key]:value}:x))

  return <form className="resource-form" onSubmit={e=>{e.preventDefault();onSubmit({customerId,employeeId:employeeId||null,items:items.filter(x=>x.gameId>0&&x.quantity>0)})}}>
    <Field label="Cliente"><select value={customerId} onChange={e=>setCustomerId(Number(e.target.value))} required><option value={0}>Selecciona…</option>{options.customers.map(x=><option key={Number(x.customerId)} value={Number(x.customerId)}>{repairText(`${x.firstName} ${x.lastName}`)}</option>)}</select></Field>
    <Field label="Empleado"><select value={employeeId} onChange={e=>setEmployeeId(Number(e.target.value))}><option value={0}>Sin empleado</option>{options.employees.map(x=><option key={Number(x.employeeId)} value={Number(x.employeeId)}>{repairText(`${x.firstName} ${x.lastName}`)}</option>)}</select></Field>

    <fieldset className="sale-items"><legend>Videojuegos</legend>
      {items.map((item,index)=><div className="sale-item" key={index}>
        <select value={item.gameId} onChange={e=>change(index,'gameId',Number(e.target.value))} required><option value={0}>Selecciona un juego…</option>{options.games.filter(x=>x.isActive!==false).map(x=><option key={Number(x.gameId)} value={Number(x.gameId)}>{repairText(String(x.title))} · {formatMoney(Number(x.unitPrice))}</option>)}</select>
        <input type="number" min="1" value={item.quantity} onChange={e=>change(index,'quantity',Number(e.target.value))} aria-label="Cantidad"/>
        <button type="button" className="icon-action" onClick={()=>setItems(current=>current.filter((_,i)=>i!==index))} disabled={items.length===1} aria-label="Quitar videojuego"><Icon name="close"/></button>
      </div>)}
      <button type="button" className="secondary-action" onClick={()=>setItems(current=>[...current,{gameId:0,quantity:1}])}><Icon name="plus"/>Agregar videojuego</button>
    </fieldset>
    <Submit busy={busy} editing={false}/>
  </form>
}

function Field({label,children}:{label:string,children:ReactNode}){return <label className="field"><span>{label}</span>{children}</label>}
function CheckGroup({label,options,value,onChange}:{label:string,options:Array<{id:number,name:string}>,value:number[],onChange:(v:number[])=>void}){
  return <fieldset className="check-group"><legend>{label}</legend>{options.map(x=><label key={x.id}><input type="checkbox" checked={value.includes(x.id)} onChange={e=>onChange(e.target.checked?[...value,x.id]:value.filter(id=>id!==x.id))}/><span>{x.name}</span></label>)}</fieldset>
}
function Submit({busy,editing}:{busy:boolean,editing:boolean}){return <button className="drawer-submit" type="submit" disabled={busy}>{busy?'Guardando…':editing?'Guardar cambios':'Crear registro'}</button>}

function Metric({icon,label,value,detail,accent=false,onClick}:{icon:IconName,label:string,value:string|number,detail:string,accent?:boolean,onClick?:()=>void}){
  return <button className={`metric ${accent?'metric-accent':''}`} onClick={onClick}><div className="metric-top"><span className="metric-icon"><Icon name={icon}/></span><span>{label}</span></div><strong>{value}</strong><small>{detail}</small></button>
}
function PanelTitle({icon,title,subtitle}:{icon:IconName,title:string,subtitle:string}){return <div className="panel-title"><span className="panel-title-icon"><Icon name={icon}/></span><div><h2>{title}</h2><p>{subtitle}</p></div></div>}

function RevenueChart({rows}:{rows:DashboardData['monthlySales']}){
  const max=useMemo(()=>Math.max(...rows.map(x=>Number(x.revenue)),1),[rows])
  if(!rows.length)return <EmptyChart/>
  return <div className="bar-chart">{rows.map(row=>{const label=new Intl.DateTimeFormat('es-DO',{month:'short',year:'2-digit'}).format(new Date(row.year,row.month-1,1));const height=Math.max((Number(row.revenue)/max)*100,8);return <div className="bar-item" key={`${row.year}-${row.month}`}><div className="bar-tooltip">{formatMoney(row.revenue)} · {row.salesCount} venta{row.salesCount===1?'':'s'}</div><div className="bar-track"><div className="bar-fill" style={{height:`${height}%`}}/></div><span>{label.replace('.','')}</span></div>})}</div>
}
function DistributionChart({rows}:{rows:DashboardData['distributionByCountry']}){
  const max=useMemo(()=>Math.max(...rows.map(x=>x.units),1),[rows])
  if(!rows.length)return <EmptyChart/>
  return <div className="distribution-list">{rows.map(row=><div className="distribution-row" key={row.countryId}><div className="distribution-label"><span>{repairText(row.country)}</span><strong>{row.units.toLocaleString('es-DO')}</strong></div><div className="distribution-track"><div style={{width:`${Math.max((row.units/max)*100,4)}%`}}/></div></div>)}</div>
}
function EmptyChart(){return <div className="empty-chart">Aún no hay información suficiente para graficar.</div>}
function EmptyState({title,text}:{title:string,text:string}){return <div className="empty-state"><img src="/brand/gamecore-icon.svg" alt=""/><strong>{title}</strong><span>{text}</span></div>}
function Skeleton(){return <div className="skeleton-grid">{Array.from({length:5}).map((_,i)=><div className="skeleton-card" key={i}/>)}<div className="skeleton-panel"/><div className="skeleton-panel"/></div>}

function Table({rows,actions}:{rows:Row[],actions?:(row:Row)=>ReactNode}){
  if(!rows.length)return <p className="muted">No hay registros para mostrar.</p>
  const hidden=new Set(['genreIds','platformIds','ageRatingId','branchId','jobPositionId','addressLine','story'])
  const cols=Object.keys(rows[0]).filter(k=>!hidden.has(k)&&!Array.isArray(rows[0][k])&&typeof rows[0][k]!=='object')
  return <div className="table-wrap"><table><thead><tr>{cols.map(c=><th key={c}>{columnLabel(c)}</th>)}{actions&&<th className="actions-column">Acciones</th>}</tr></thead><tbody>{rows.map((r,i)=><tr key={String(r.id??r.gameId??r.customerId??r.saleId??r.employeeId??r.distributionId??i)}>{cols.map(c=><td key={c}>{formatCell(c,r[c])}</td>)}{actions&&<td className="actions-column">{actions(r)}</td>}</tr>)}</tbody></table></div>
}

function columnLabel(key:string){
  const labels:Record<string,string>={
    gameId:'ID',title:'Título',unitsSold:'Vendidos',revenue:'Ingresos',saleId:'Venta',saleDate:'Fecha',customer:'Cliente',employee:'Empleado',total:'Total',status:'Estado',
    customerId:'ID',firstName:'Nombre',lastName:'Apellido',phone:'Teléfono',email:'Correo',createdAt:'Registro',employeeId:'ID',isActive:'Estado',branch:'Sucursal',position:'Puesto',
    distributionId:'ID',countryId:'País ID',country:'País',distributionDate:'Fecha',units:'Unidades',releaseDate:'Lanzamiento',unitPrice:'Precio',ageRating:'Clasificación',
    year:'Año',month:'Mes',salesCount:'Ventas',sales:'Ventas',lifetimeValue:'Valor acumulado',distributedUnits:'Unidades distribuidas',games:'Videojuegos'
  }
  return labels[key]??humanize(key)
}
function formatCell(key:string,value:unknown){
  if(value===null||value===undefined||value==='')return <span className="muted">—</span>
  if(key==='isActive')return <span className={`status-badge ${value?'active':'inactive'}`}>{value?'Activo':'Inactivo'}</span>
  if(key==='status'){const status=String(value);const text=status==='Completed'?'Completada':status==='Pending'?'Pendiente':status==='Cancelled'?'Cancelada':repairText(status);return <span className={`status-badge ${status.toLowerCase()}`}>{text}</span>}
  if(['unitPrice','revenue','total','lifetimeValue'].includes(key)&&typeof value==='number')return formatMoney(value)
  if(/date|At$/i.test(key)&&typeof value==='string')return formatDate(value)
  if(typeof value==='string')return repairText(value)
  return String(value)
}
function formatMoney(value:number){return new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(Number(value))}
function formatDate(value:string){const d=new Date(value);return Number.isNaN(d.getTime())?repairText(value):new Intl.DateTimeFormat('es-DO',{day:'2-digit',month:'2-digit',year:'numeric'}).format(d)}
function repairText(value:string){if(!/[ÃÂ]/.test(value))return value;try{const bytes=Uint8Array.from(Array.from(value).map(char=>char.charCodeAt(0)&255));return new TextDecoder('utf-8').decode(bytes)}catch{return value}}
function humanize(key:string){return key.replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,m=>m.toUpperCase())}
function createLabel(section:Section){return section==='games'?'Nuevo videojuego':section==='customers'?'Nuevo cliente':section==='sales'?'Nueva venta':section==='employees'?'Nuevo empleado':section==='distributions'?'Nueva distribución':'Nuevo'}
function resourceTitle(section:Section){return section==='games'?'videojuego':section==='customers'?'cliente':section==='sales'?'venta':section==='employees'?'empleado':section==='distributions'?'distribución':section}
function Icon({name}:{name:IconName}){
  const common={width:18,height:18,viewBox:'0 0 24 24',fill:'none',stroke:'currentColor',strokeWidth:1.8,strokeLinecap:'round' as const,strokeLinejoin:'round' as const,'aria-hidden':true}
  const p:Record<IconName,ReactNode>={
    dashboard:<><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></>,
    game:<><path d="M8.5 8h7a5.5 5.5 0 0 1 5.3 7l-1 3.2a2.2 2.2 0 0 1-3.7.9L14 17h-4l-2.1 2.1a2.2 2.2 0 0 1-3.7-.9L3.2 15a5.5 5.5 0 0 1 5.3-7Z"/><path d="M7 12v4M5 14h4M16.5 13h.01M18.5 15h.01"/></>,
    users:<><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></>,
    cart:<><circle cx="9" cy="20" r="1"/><circle cx="19" cy="20" r="1"/><path d="M3 4h2l2.4 10.4a2 2 0 0 0 2 1.6h7.7a2 2 0 0 0 2-1.6L21 7H6"/></>,
    briefcase:<><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18M10 12v2h4v-2"/></>,
    globe:<><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18"/></>,
    money:<><rect x="3" y="6" width="18" height="12" rx="2"/><circle cx="12" cy="12" r="2.5"/><path d="M7 9H6a1 1 0 0 1-1-1M17 15h1a1 1 0 0 1 1 1"/></>,
    refresh:<><path d="M20 6v5h-5"/><path d="M4 18v-5h5"/><path d="M18.5 9A7 7 0 0 0 6.2 6.2L4 9M5.5 15A7 7 0 0 0 17.8 17.8L20 15"/></>,
    chart:<><path d="M4 19V9M10 19V5M16 19v-7M22 19H2"/></>,clock:<><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
    reports:<><path d="M5 3h14a2 2 0 0 1 2 2v14H3V5a2 2 0 0 1 2-2Z"/><path d="M7 15v-3M12 15V8M17 15v-5"/></>,
    search:<><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></>,plus:<><path d="M12 5v14M5 12h14"/></>,edit:<><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></>,
    close:<><path d="M6 6l12 12M18 6 6 18"/></>,user:<><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></>,chevron:<><path d="m9 10 3 3 3-3"/></>
  }
  return <svg {...common}>{p[name]}</svg>
}
