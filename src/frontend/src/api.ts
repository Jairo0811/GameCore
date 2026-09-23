const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:5001'

export async function login(email:string,password:string){
  const r=await fetch(`${API_URL}/api/auth/login`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,password})})
  if(!r.ok) throw new Error('Credenciales inválidas')
  return r.json() as Promise<{token:string,email:string,role:string}>
}
export async function api<T>(path:string,token:string,init?:RequestInit):Promise<T>{
  const r=await fetch(`${API_URL}${path}`,{...init,headers:{'Content-Type':'application/json',Authorization:`Bearer ${token}`,...(init?.headers??{})}})
  if(!r.ok) throw new Error((await r.text())||`HTTP ${r.status}`)
  if(r.status===204) return undefined as T
  return r.json()
}
