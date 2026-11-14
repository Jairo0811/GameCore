import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, api, login, readError, setUnauthorizedHandler } from './api'

function json(status:number,body:unknown,type='application/json'){
  return new Response(typeof body==='string'?body:JSON.stringify(body),{status,headers:{'Content-Type':type}})
}

afterEach(()=>{
  vi.unstubAllGlobals()
  setUnauthorizedHandler(null)
})

describe('readError',()=>{
  it('prefers the legacy error field',async()=>{
    expect(await readError(json(400,{error:'Mensaje legado'}))).toBe('Mensaje legado')
  })

  it('uses ProblemDetails detail',async()=>{
    expect(await readError(json(409,{title:'Conflicto con datos existentes.',status:409,detail:'Ya existe un cliente con ese correo.'},'application/problem+json'))).toBe('Ya existe un cliente con ese correo.')
  })

  it('joins and de-duplicates validation errors',async()=>{
    const body={title:'La solicitud contiene datos inválidos.',status:400,errors:{firstName:['El nombre es obligatorio.'],items:['Línea inválida.','Línea inválida.']}}
    expect(await readError(json(400,body,'application/problem+json'))).toBe('El nombre es obligatorio. Línea inválida.')
  })

  it('falls back to the title, then plain text, then the status',async()=>{
    expect(await readError(json(404,{title:'Recurso no encontrado.'}))).toBe('Recurso no encontrado.')
    expect(await readError(new Response('Texto plano',{status:400}))).toBe('Texto plano')
    expect(await readError(new Response('',{status:418}))).toBe('Error HTTP 418')
    expect(await readError(new Response('',{status:503}))).toMatch(/servidor/)
  })
})

describe('api',()=>{
  it('sends the bearer token and parses JSON',async()=>{
    const fetchMock=vi.fn(async(_url:string,_init?:RequestInit)=>json(200,[{gameId:1}]))
    vi.stubGlobal('fetch',fetchMock)

    const result=await api<Array<{gameId:number}>>('/api/games','token-123')

    expect(result).toEqual([{gameId:1}])
    const headers=fetchMock.mock.calls[0][1]?.headers as Record<string,string>
    expect(headers.Authorization).toBe('Bearer token-123')
  })

  it('returns undefined for 204 No Content',async()=>{
    vi.stubGlobal('fetch',vi.fn(async()=>new Response(null,{status:204})))
    expect(await api('/api/customers/1','t',{method:'PUT'})).toBeUndefined()
  })

  it('notifies the unauthorized handler once per 401 and throws ApiError(401)',async()=>{
    vi.stubGlobal('fetch',vi.fn(async()=>new Response('',{status:401})))
    const handler=vi.fn()
    setUnauthorizedHandler(handler)

    const error=await api('/api/games','expired').catch(e=>e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(401)
    expect(handler).toHaveBeenCalledTimes(1)
  })

  it('maps 403 to a permissions message without logging out',async()=>{
    vi.stubGlobal('fetch',vi.fn(async()=>json(403,{title:'Forbidden',status:403})))
    const handler=vi.fn()
    setUnauthorizedHandler(handler)

    await expect(api('/api/games','viewer',{method:'POST'})).rejects.toMatchObject({status:403,message:'No tienes permisos para realizar esta acción.'})
    expect(handler).not.toHaveBeenCalled()
  })

  it('reports network failures in Spanish',async()=>{
    vi.stubGlobal('fetch',vi.fn(async()=>{throw new TypeError('Failed to fetch')}))
    await expect(api('/api/games','t')).rejects.toMatchObject({status:0,message:'No se pudo conectar con el servidor de GameCore.'})
  })
})

describe('login',()=>{
  it('does not trigger the session-expired handler on wrong credentials',async()=>{
    vi.stubGlobal('fetch',vi.fn(async()=>new Response('',{status:401})))
    const handler=vi.fn()
    setUnauthorizedHandler(handler)

    await expect(login('admin@gamecore.local','bad')).rejects.toMatchObject({status:401,message:'Correo o contraseña incorrectos.'})
    expect(handler).not.toHaveBeenCalled()
  })
})
