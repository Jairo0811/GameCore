export type Role='Admin'|'Manager'|'Viewer'
export type Session={token:string,email:string,role:string,expiresAt?:string}

export const SESSION_KEY='gamecore_session'

type KeyValueStorage=Pick<Storage,'getItem'|'setItem'|'removeItem'>

/** Reads the stored session. Corrupt, incomplete or expired values are discarded instead of crashing the app. */
export function readSession(storage:KeyValueStorage,now:number=Date.now()):Session|null{
  let raw:string|null
  try{raw=storage.getItem(SESSION_KEY)}catch{return null}
  if(!raw)return null
  try{
    const value=JSON.parse(raw) as Partial<Session>|null
    if(!value||typeof value!=='object'||typeof value.token!=='string'||!value.token||typeof value.email!=='string'||typeof value.role!=='string')throw new Error('invalid session')
    const session:Session={token:value.token,email:value.email,role:value.role,expiresAt:typeof value.expiresAt==='string'?value.expiresAt:undefined}
    if(isExpired(session,now))throw new Error('expired session')
    return session
  }catch{
    clearSession(storage)
    return null
  }
}

export function saveSession(storage:KeyValueStorage,session:Session){
  try{storage.setItem(SESSION_KEY,JSON.stringify(session))}catch{/* storage unavailable: keep the in-memory session */}
}

export function clearSession(storage:KeyValueStorage){
  try{storage.removeItem(SESSION_KEY)}catch{/* storage unavailable */}
}

/** Expiry in epoch milliseconds, from expiresAt or the JWT "exp" claim. */
export function sessionExpiry(session:Session):number|null{
  if(session.expiresAt){
    const value=Date.parse(session.expiresAt)
    if(!Number.isNaN(value))return value
  }
  const payload=session.token.split('.')[1]
  if(!payload)return null
  try{
    const json=JSON.parse(atob(payload.replace(/-/g,'+').replace(/_/g,'/').padEnd(Math.ceil(payload.length/4)*4,'='))) as {exp?:unknown}
    return typeof json.exp==='number'?json.exp*1000:null
  }catch{
    return null
  }
}

export function isExpired(session:Session,now:number=Date.now()){
  const expiry=sessionExpiry(session)
  return expiry!==null&&expiry<=now
}

/** Mirrors the API policies: Viewer is read-only; Manager and Admin can create and edit. */
export function canWrite(role:string){
  return role==='Admin'||role==='Manager'
}
