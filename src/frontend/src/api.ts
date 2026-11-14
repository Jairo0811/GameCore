import type { Session } from './session'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5152'

export class ApiError extends Error{
  readonly status:number
  constructor(status:number,message:string){
    super(message)
    this.name='ApiError'
    this.status=status
  }
}

let unauthorizedHandler:(()=>void)|null=null

/** Called once per 401 from an authenticated request (expired or invalid token). */
export function setUnauthorizedHandler(handler:(()=>void)|null){
  unauthorizedHandler=handler
}

async function send(url:string,init:RequestInit){
  try{
    return await fetch(url,init)
  }catch{
    throw new ApiError(0,'No se pudo conectar con el servidor de GameCore.')
  }
}

export async function login(email:string,password:string):Promise<Session>{
  const response=await send(`${API_URL}/api/auth/login`,{
    method:'POST',
    headers:{'Content-Type':'application/json'},
    body:JSON.stringify({email,password})
  })

  if(!response.ok)throw new ApiError(response.status,response.status===401?'Correo o contraseña incorrectos.':await readError(response))
  return response.json() as Promise<Session>
}

export async function api<T>(path:string,token:string,init?:RequestInit):Promise<T>{
  const response=await send(`${API_URL}${path}`,{
    ...init,
    headers:{
      'Content-Type':'application/json',
      Authorization:`Bearer ${token}`,
      ...(init?.headers??{})
    }
  })

  if(response.status===401){
    unauthorizedHandler?.()
    throw new ApiError(401,'Tu sesión expiró. Inicia sesión de nuevo.')
  }
  if(response.status===403)throw new ApiError(403,'No tienes permisos para realizar esta acción.')
  if(!response.ok)throw new ApiError(response.status,await readError(response))
  if(response.status===204)return undefined as T
  const text=await response.text()
  return (text?JSON.parse(text):undefined) as T
}

/** Extracts a user-facing message from ProblemDetails, legacy { error } bodies or plain text. */
export async function readError(response:Response){
  let text=''
  try{text=await response.text()}catch{/* body unavailable */}
  if(!text)return response.status>=500?'El servidor no está disponible. Inténtalo de nuevo más tarde.':`Error HTTP ${response.status}`
  try{
    const data=JSON.parse(text) as {error?:string;title?:string;detail?:string;errors?:Record<string,string[]>}
    if(data.error)return data.error
    if(data.detail)return data.detail
    if(data.errors){
      const messages=[...new Set(Object.values(data.errors).flat())]
      if(messages.length)return messages.join(' ')
    }
    if(data.title)return data.title
  }catch{
    // The response is plain text.
  }
  return text
}
