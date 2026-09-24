import { FormEvent, ReactNode, useCallback, useEffect, useMemo, useState } from 'react'
import { api, login } from './api'

type Section='dashboard'|'games'|'customers'|'sales'|'employees'|'distributions'
type IconName='dashboard'|'game'|'users'|'cart'|'briefcase'|'globe'|'money'|'refresh'|'chart'|'clock'

type DashboardData={
  games:number
  customers:number
  sales:number
  employees:number
  revenue:number
  topGames:Array<{gameId:number,title:string,unitsSold:number,revenue:number}>
  monthlySales:Array<{year:number,month:number,salesCount:number,revenue:number}>
  recentSales:Array<{saleId:number,saleDate:string,customer:string,total:number}>
  distributionByCountry:Array<{countryId:number,country:string,units:number}>
}

const sections:Array<[Section,string,IconName]>=[
  ['dashboard','Dashboard','dashboard'],
  ['games','Videojuegos','game'],
  ['customers','Clientes','users'],
  ['sales','Ventas','cart'],
  ['employees','Empleados','briefcase'],
  ['distributions','Distribución','globe']
]

const sectionDescriptions:Record<Section,string>={
  dashboard:'Resumen general del sistema',
  games:'Administra el catálogo de videojuegos',
  customers:'Gestiona la información de tus clientes',
  sales:'Consulta y registra las operaciones comerciales',
  employees:'Gestiona el equipo y sus posiciones',
  distributions:'Controla la distribución de títulos por país'
}

export default function App(){
  const [token,setToken]=useState(localStorage.getItem('gamecore_token')??'')
  const [section,setSection]=useState<Section>('dashboard')
  const [data,setData]=useState<unknown>(null)
  const [error,setError]=useState('')
  const [loading,setLoading]=useState(false)

  const load=useCallback(async()=>{
    if(!token)return
    setLoading(true);setError('')
    try{setData(await api(`/api/${section}`,token))}
    catch(e){setError(e instanceof Error?e.message:'Error inesperado')}
    finally{setLoading(false)}
  },[section,token])

  useEffect(()=>{void load()},[load])

  if(!token)return <Login onToken={v=>{localStorage.setItem('gamecore_token',v);setToken(v)}}/>

  const current=sections.find(x=>x[0]===section)

  return <div className="app-shell">
    <aside>
      <div className="brand">
        <img src="/brand/gamecore-logo.svg" alt="GameCore" className="brand-logo"/>
      </div>
      <nav>{sections.map(([k,l,icon])=>
        <button key={k} className={section===k?'active':''} onClick={()=>setSection(k)}>
          <Icon name={icon}/><span>{l}</span>
        </button>
      )}</nav>
      <div className="brand-signature">PLAY · STORE · MANAGE · GROW</div>
      <button className="logout" onClick={()=>{localStorage.removeItem('gamecore_token');setToken('')}}>
        <span>↪</span><span>Cerrar sesión</span>
      </button>
    </aside>

    <main>
      <header className="app-header">
        <div>
          <p className="eyebrow">GAMECORE · CONTROL CENTER</p>
          <h1>{current?.[1]}</h1>
          <p className="header-subtitle">{sectionDescriptions[section]}</p>
        </div>
        <button className="refresh-button" onClick={()=>void load()}>
          <Icon name="refresh"/><span>Actualizar</span>
        </button>
      </header>

      {error&&<div className="error">{error}</div>}
      {loading?<div className="panel loading-panel">Cargando información…</div>:<View section={section} data={data}/>}
      <footer className="app-footer">Proyecto restaurado · SOF-006 · ITLA · 2016 → 2026</footer>
    </main>
  </div>
}

function Login({onToken}:{onToken:(token:string)=>void}){
  const [email,setEmail]=useState('admin@gamecore.local'),[password,setPassword]=useState('GameCore123!'),[error,setError]=useState('')
  async function submit(e:FormEvent){e.preventDefault();try{onToken((await login(email,password)).token)}catch(err){setError(err instanceof Error?err.message:'Error')}}
  return <div className="login-page">
    <section className="login-hero" aria-label="GameCore — Database + Gaming">
      <div className="login-hero-shade"/>
      <div className="login-hero-badges">
        <span>SQL Server</span><span>.NET 10</span><span>React</span>
      </div>
      <div className="login-hero-caption">
        <span className="hero-kicker">DATABASE + GAMING</span>
        <strong>Datos que impulsan el juego.</strong>
        <small>PLAY · STORE · MANAGE · GROW</small>
      </div>
    </section>

    <section className="login-access">
      <form className="login-card" onSubmit={submit}>
        <div className="login-card-brand">
          <img src="/brand/gamecore-icon.svg" className="login-icon" alt="GameCore"/>
          <div><span>GAMECORE</span><small>VIDEO GAME MANAGEMENT SYSTEM</small></div>
        </div>

        <div className="login-heading">
          <h1>Bienvenido</h1>
          <p>Accede a tu centro de gestión.</p>
        </div>

        <label>Correo
          <input type="email" autoComplete="username" value={email} onChange={e=>setEmail(e.target.value)}/>
        </label>
        <label>Contraseña
          <input type="password" autoComplete="current-password" value={password} onChange={e=>setPassword(e.target.value)}/>
        </label>

        {error&&<div className="error">{error}</div>}

        <button type="submit"><span>Entrar a GameCore</span><span aria-hidden="true">→</span></button>

        <div className="login-demo-note"><span/><small>Cuenta demo local para validación del proyecto.</small><span/></div>
      </form>

      <footer className="login-footer"><span>GameCore</span><span>•</span><span>SOF-006</span><span>•</span><span>Restauración 2026</span></footer>
    </section>
  </div>
}

function View({section,data}:{section:Section,data:unknown}){
  if(section==='dashboard')return <Dashboard data={data as DashboardData|null}/>
  return <div className="panel data-panel"><Table rows={Array.isArray(data)?data as Record<string,unknown>[]:[]}/></div>
}

function Dashboard({data}:{data:DashboardData|null}){
  if(!data)return <div className="panel">Sin datos.</div>

  return <div className="dashboard">
    <div className="cards">
      <Metric icon="game" label="Videojuegos" value={data.games} detail="Títulos activos"/>
      <Metric icon="users" label="Clientes" value={data.customers} detail="Registrados"/>
      <Metric icon="cart" label="Ventas" value={data.sales} detail="Completadas"/>
      <Metric icon="briefcase" label="Empleados" value={data.employees} detail="Activos"/>
      <Metric icon="money" label="Ingresos" value={formatMoney(data.revenue)} detail="Ventas completadas" accent/>
    </div>

    <div className="dashboard-grid">
      <section className="panel chart-panel">
        <PanelTitle icon="chart" title="Ingresos por período" subtitle="Evolución de las ventas registradas"/>
        <RevenueChart rows={data.monthlySales}/>
      </section>

      <section className="panel chart-panel">
        <PanelTitle icon="globe" title="Distribución por país" subtitle="Unidades distribuidas"/>
        <DistributionChart rows={data.distributionByCountry}/>
      </section>
    </div>

    <div className="dashboard-grid bottom-grid">
      <section className="panel">
        <PanelTitle icon="game" title="Juegos más vendidos" subtitle="Rendimiento del catálogo"/>
        <Table rows={data.topGames as unknown as Record<string,unknown>[]}/>
      </section>

      <section className="panel">
        <PanelTitle icon="clock" title="Ventas recientes" subtitle="Últimas operaciones completadas"/>
        <Table rows={data.recentSales as unknown as Record<string,unknown>[]}/>
      </section>
    </div>
  </div>
}

function Metric({icon,label,value,detail,accent=false}:{icon:IconName,label:string,value:string|number,detail:string,accent?:boolean}){
  return <div className={`metric ${accent?'metric-accent':''}`}>
    <div className="metric-top"><span className="metric-icon"><Icon name={icon}/></span><span>{label}</span></div>
    <strong>{value}</strong>
    <small>{detail}</small>
  </div>
}

function PanelTitle({icon,title,subtitle}:{icon:IconName,title:string,subtitle:string}){
  return <div className="panel-title">
    <span className="panel-title-icon"><Icon name={icon}/></span>
    <div><h2>{title}</h2><p>{subtitle}</p></div>
  </div>
}

function RevenueChart({rows}:{rows:DashboardData['monthlySales']}){
  const max=useMemo(()=>Math.max(...rows.map(x=>Number(x.revenue)),1),[rows])
  if(!rows.length)return <EmptyChart/>

  return <div className="bar-chart">
    {rows.map(row=>{
      const label=new Intl.DateTimeFormat('es-DO',{month:'short',year:'2-digit'}).format(new Date(row.year,row.month-1,1))
      const height=Math.max((Number(row.revenue)/max)*100,8)
      return <div className="bar-item" key={`${row.year}-${row.month}`}>
        <div className="bar-tooltip">{formatMoney(row.revenue)} · {row.salesCount} venta{row.salesCount===1?'':'s'}</div>
        <div className="bar-track"><div className="bar-fill" style={{height:`${height}%`}}/></div>
        <span>{label.replace('.','')}</span>
      </div>
    })}
  </div>
}

function DistributionChart({rows}:{rows:DashboardData['distributionByCountry']}){
  const max=useMemo(()=>Math.max(...rows.map(x=>x.units),1),[rows])
  if(!rows.length)return <EmptyChart/>

  return <div className="distribution-list">
    {rows.map(row=><div className="distribution-row" key={row.countryId}>
      <div className="distribution-label"><span>{repairText(row.country)}</span><strong>{row.units.toLocaleString('es-DO')}</strong></div>
      <div className="distribution-track"><div style={{width:`${Math.max((row.units/max)*100,4)}%`}}/></div>
    </div>)}
  </div>
}

function EmptyChart(){return <div className="empty-chart">Aún no hay información suficiente para graficar.</div>}

function Table({rows}:{rows:Record<string,unknown>[]}){
  if(!rows.length)return <p className="muted">No hay registros para mostrar.</p>
  const cols=Object.keys(rows[0]).filter(k=>!Array.isArray(rows[0][k])&&typeof rows[0][k]!=='object')

  return <div className="table-wrap"><table>
    <thead><tr>{cols.map(c=><th key={c}>{columnLabel(c)}</th>)}</tr></thead>
    <tbody>{rows.map((r,i)=><tr key={i}>{cols.map(c=><td key={c}>{formatCell(c,r[c])}</td>)}</tr>)}</tbody>
  </table></div>
}

function columnLabel(key:string){
  const labels:Record<string,string>={
    gameId:'ID',title:'Título',unitsSold:'Vendidos',revenue:'Ingresos',
    saleId:'Venta',saleDate:'Fecha',customer:'Cliente',employee:'Empleado',total:'Total',status:'Estado',
    customerId:'ID',firstName:'Nombre',lastName:'Apellido',phone:'Teléfono',email:'Correo',createdAt:'Registro',
    employeeId:'ID',isActive:'Estado',branch:'Sucursal',position:'Puesto',
    distributionId:'ID',countryId:'País ID',country:'País',distributionDate:'Fecha',units:'Unidades',
    releaseDate:'Lanzamiento',unitPrice:'Precio',ageRating:'Clasificación'
  }
  return labels[key]??humanize(key)
}

function formatCell(key:string,value:unknown){
  if(value===null||value===undefined||value==='')return <span className="muted">—</span>

  if(key==='isActive')return <span className={`status-badge ${value?'active':'inactive'}`}>{value?'Activo':'Inactivo'}</span>
  if(key==='status'){
    const status=String(value)
    const text=status==='Completed'?'Completada':status==='Pending'?'Pendiente':status==='Cancelled'?'Cancelada':repairText(status)
    return <span className={`status-badge ${status.toLowerCase()}`}>{text}</span>
  }
  if(['unitPrice','revenue','total'].includes(key)&&typeof value==='number')return formatMoney(value)
  if(/date|At$/i.test(key)&&typeof value==='string')return formatDate(value)
  if(typeof value==='string')return repairText(value)
  return String(value)
}

function formatMoney(value:number){return new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(Number(value))}
function formatDate(value:string){
  const d=new Date(value)
  return Number.isNaN(d.getTime())?repairText(value):new Intl.DateTimeFormat('es-DO',{day:'2-digit',month:'2-digit',year:'numeric'}).format(d)
}
function repairText(value:string){
  if(!/[ÃÂ]/.test(value))return value
  try{
    const bytes=Uint8Array.from(Array.from(value).map(char=>char.charCodeAt(0)&255))
    return new TextDecoder('utf-8').decode(bytes)
  }catch{return value}
}
function humanize(key:string){return key.replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,m=>m.toUpperCase())}

function Icon({name}:{name:IconName}){
  const common={width:18,height:18,viewBox:'0 0 24 24',fill:'none',stroke:'currentColor',strokeWidth:1.8,strokeLinecap:'round' as const,strokeLinejoin:'round' as const,'aria-hidden':true}
  const paths:Record<IconName,ReactNode>={
    dashboard:<><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></>,
    game:<><path d="M8.5 8h7a5.5 5.5 0 0 1 5.3 7l-1 3.2a2.2 2.2 0 0 1-3.7.9L14 17h-4l-2.1 2.1a2.2 2.2 0 0 1-3.7-.9L3.2 15a5.5 5.5 0 0 1 5.3-7Z"/><path d="M7 12v4M5 14h4M16.5 13h.01M18.5 15h.01"/></>,
    users:<><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></>,
    cart:<><circle cx="9" cy="20" r="1"/><circle cx="19" cy="20" r="1"/><path d="M3 4h2l2.4 10.4a2 2 0 0 0 2 1.6h7.7a2 2 0 0 0 2-1.6L21 7H6"/></>,
    briefcase:<><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18M10 12v2h4v-2"/></>,
    globe:<><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18"/></>,
    money:<><rect x="3" y="6" width="18" height="12" rx="2"/><circle cx="12" cy="12" r="2.5"/><path d="M7 9H6a1 1 0 0 1-1-1M17 15h1a1 1 0 0 1 1 1"/></>,
    refresh:<><path d="M20 6v5h-5"/><path d="M4 18v-5h5"/><path d="M18.5 9A7 7 0 0 0 6.2 6.2L4 9M5.5 15A7 7 0 0 0 17.8 17.8L20 15"/></>,
    chart:<><path d="M4 19V9M10 19V5M16 19v-7M22 19H2"/></>,
    clock:<><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>
  }
  return <svg {...common}>{paths[name]}</svg>
}
