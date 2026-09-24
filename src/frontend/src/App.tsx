import { FormEvent, useCallback, useEffect, useState } from 'react'
import { api, login } from './api'

type Section='dashboard'|'games'|'customers'|'sales'|'employees'|'distributions'
const sections:Array<[Section,string]>=[['dashboard','Dashboard'],['games','Videojuegos'],['customers','Clientes'],['sales','Ventas'],['employees','Empleados'],['distributions','Distribución']]

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

  return <div className="app-shell">
    <aside>
      <div className="brand">
        <img src="/brand/gamecore-icon.svg" alt="" className="brand-icon"/>
        <div><strong>GameCore</strong><small>Management System</small></div>
      </div>
      <nav>{sections.map(([k,l])=><button key={k} className={section===k?'active':''} onClick={()=>setSection(k)}>{l}</button>)}</nav>
      <div className="brand-signature">PLAY · STORE · MANAGE · GROW</div>
      <button className="logout" onClick={()=>{localStorage.removeItem('gamecore_token');setToken('')}}>Cerrar sesión</button>
    </aside>
    <main>
      <header><div><p className="eyebrow">SOF-006 · Restauración 2026</p><h1>{sections.find(x=>x[0]===section)?.[1]}</h1></div><button onClick={()=>void load()}>Actualizar</button></header>
      {error&&<div className="error">{error}</div>}
      {loading?<div className="panel">Cargando…</div>:<View section={section} data={data}/>}
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
        <span>SQL Server</span>
        <span>.NET 10</span>
        <span>React</span>
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
          <div>
            <span>GAMECORE</span>
            <small>VIDEO GAME MANAGEMENT SYSTEM</small>
          </div>
        </div>

        <div className="login-heading">
          <h1>Bienvenido</h1>
          <p>Accede a tu centro de gestión.</p>
        </div>

        <label>
          Correo
          <input
            type="email"
            autoComplete="username"
            value={email}
            onChange={e=>setEmail(e.target.value)}
          />
        </label>

        <label>
          Contraseña
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={e=>setPassword(e.target.value)}
          />
        </label>

        {error&&<div className="error">{error}</div>}

        <button type="submit">
          <span>Entrar a GameCore</span>
          <span aria-hidden="true">→</span>
        </button>

        <div className="login-demo-note">
          <span/>
          <small>Cuenta demo local para validación del proyecto.</small>
          <span/>
        </div>
      </form>

      <footer className="login-footer">
        <span>GameCore</span>
        <span>•</span>
        <span>SOF-006</span>
        <span>•</span>
        <span>Restauración 2026</span>
      </footer>
    </section>
  </div>
}
function View({section,data}:{section:Section,data:unknown}){
  if(section==='dashboard'){
    const d=data as {games:number,customers:number,sales:number,employees:number,revenue:number,topGames:Record<string,unknown>[]}|null
    if(!d)return <div className="panel">Sin datos.</div>
    return <><div className="cards"><Metric l="Videojuegos" v={d.games}/><Metric l="Clientes" v={d.customers}/><Metric l="Ventas" v={d.sales}/><Metric l="Empleados" v={d.employees}/><Metric l="Ingresos" v={`US$ ${Number(d.revenue).toFixed(2)}`}/></div><div className="panel"><h2>Juegos más vendidos</h2><Table rows={d.topGames}/></div></>
  }
  return <div className="panel"><Table rows={Array.isArray(data)?data as Record<string,unknown>[]:[]}/></div>
}
function Metric({l,v}:{l:string,v:string|number}){return <div className="metric"><span>{l}</span><strong>{v}</strong></div>}
function Table({rows}:{rows:Record<string,unknown>[]}){
  if(!rows.length)return <p className="muted">No hay registros.</p>
  const cols=Object.keys(rows[0]).filter(k=>!Array.isArray(rows[0][k])&&typeof rows[0][k]!=='object')
  return <div className="table-wrap"><table><thead><tr>{cols.map(c=><th key={c}>{c}</th>)}</tr></thead><tbody>{rows.map((r,i)=><tr key={i}>{cols.map(c=><td key={c}>{String(r[c]??'')}</td>)}</tr>)}</tbody></table></div>
}
