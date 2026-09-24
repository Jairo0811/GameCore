const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5152'

export async function login(email:string,password:string){
  const response=await fetch(`${API_URL}/api/auth/login`,{
    method:'POST',
    headers:{'Content-Type':'application/json'},
    body:JSON.stringify({email,password})
  })

  if(!response.ok)throw new Error(response.status===401?'Correo o contraseña incorrectos.':await readError(response))
  return response.json() as Promise<{token:string,email:string,role:string}>
}

export async function api<T>(path:string,token:string,init?:RequestInit):Promise<T>{
  const response=await fetch(`${API_URL}${path}`,{
    ...init,
    headers:{
      'Content-Type':'application/json',
      Authorization:`Bearer ${token}`,
      ...(init?.headers??{})
    }
  })

  if(!response.ok)throw new Error(await readError(response))
  if(response.status===204)return undefined as T
  return response.json() as Promise<T>
}

async function readError(response:Response){
  const text=await response.text()
  if(!text)return `Error HTTP ${response.status}`
  try{
    const data=JSON.parse(text) as {error?:string;title?:string;detail?:string;errors?:Record<string,string[]>}
    if(data.error)return data.error
    if(data.detail)return data.detail
    if(data.errors)return Object.values(data.errors).flat().join(' ')
    if(data.title)return data.title
  }catch{
    // The response is plain text.
  }
  return text
}
