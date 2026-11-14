import { describe, expect, it } from 'vitest'
import { SESSION_KEY, canWrite, isExpired, readSession, saveSession, type Session } from './session'

class MemoryStorage{
  private values=new Map<string,string>()
  getItem(key:string){return this.values.get(key)??null}
  setItem(key:string,value:string){this.values.set(key,value)}
  removeItem(key:string){this.values.delete(key)}
}

function jwt(payload:Record<string,unknown>){
  const encode=(value:unknown)=>btoa(JSON.stringify(value)).replace(/=+$/,'').replace(/\+/g,'-').replace(/\//g,'_')
  return `${encode({alg:'HS256',typ:'JWT'})}.${encode(payload)}.signature`
}

const now=Date.parse('2026-11-14T12:00:00Z')

describe('readSession',()=>{
  it('returns null when nothing is stored',()=>{
    expect(readSession(new MemoryStorage(),now)).toBeNull()
  })

  it('discards corrupt JSON instead of throwing',()=>{
    const storage=new MemoryStorage()
    storage.setItem(SESSION_KEY,'{not json')
    expect(readSession(storage,now)).toBeNull()
    expect(storage.getItem(SESSION_KEY)).toBeNull()
  })

  it.each([
    ['null','null'],
    ['a string','"token"'],
    ['missing token',JSON.stringify({email:'a@b.test',role:'Admin'})],
    ['empty token',JSON.stringify({token:'',email:'a@b.test',role:'Admin'})],
    ['wrong types',JSON.stringify({token:1,email:'a@b.test',role:'Admin'})]
  ])('discards an invalid shape (%s)',(_name,raw)=>{
    const storage=new MemoryStorage()
    storage.setItem(SESSION_KEY,raw)
    expect(readSession(storage,now)).toBeNull()
  })

  it('returns a valid session',()=>{
    const storage=new MemoryStorage()
    const session:Session={token:jwt({exp:now/1000+3600}),email:'admin@gamecore.local',role:'Admin',expiresAt:'2026-11-14T20:00:00Z'}
    saveSession(storage,session)
    expect(readSession(storage,now)).toEqual(session)
  })

  it('discards an expired session using expiresAt',()=>{
    const storage=new MemoryStorage()
    saveSession(storage,{token:'a.b.c',email:'admin@gamecore.local',role:'Admin',expiresAt:'2026-11-14T11:59:59Z'})
    expect(readSession(storage,now)).toBeNull()
    expect(storage.getItem(SESSION_KEY)).toBeNull()
  })

  it('falls back to the JWT exp claim when expiresAt is missing',()=>{
    const expired={token:jwt({exp:now/1000-1}),email:'a@b.test',role:'Viewer'}
    const valid={token:jwt({exp:now/1000+60}),email:'a@b.test',role:'Viewer'}
    expect(isExpired(expired,now)).toBe(true)
    expect(isExpired(valid,now)).toBe(false)
  })

  it('survives a storage that throws',()=>{
    const broken={getItem:()=>{throw new Error('denied')},setItem:()=>{throw new Error('denied')},removeItem:()=>{throw new Error('denied')}}
    expect(readSession(broken,now)).toBeNull()
    expect(()=>saveSession(broken,{token:'t',email:'e',role:'Admin'})).not.toThrow()
  })
})

describe('canWrite',()=>{
  it('matches the API roles',()=>{
    expect(canWrite('Admin')).toBe(true)
    expect(canWrite('Manager')).toBe(true)
    expect(canWrite('Viewer')).toBe(false)
    expect(canWrite('')).toBe(false)
  })
})
