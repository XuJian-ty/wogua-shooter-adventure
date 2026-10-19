#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class WoguaShooterCoreCombatVfxWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.CombatVfx);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 5, 0, 0);
			Utils.RegisterFunc(L, Utils.CLS_IDX, "PlayEnemyHit", _m_PlayEnemyHit_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "PlayStomp", _m_PlayStomp_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "PlayGrowthSmash", _m_PlayGrowthSmash_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "PlayCornExplosion", _m_PlayCornExplosion_xlua_st_);
            
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "WoguaShooter.Core.CombatVfx does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayEnemyHit_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 2&& translator.Assignable<UnityEngine.Vector3>(L, 1)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    float _duration = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    WoguaShooter.Core.CombatVfx.PlayEnemyHit( _position, _duration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1&& translator.Assignable<UnityEngine.Vector3>(L, 1)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    
                    WoguaShooter.Core.CombatVfx.PlayEnemyHit( _position );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.CombatVfx.PlayEnemyHit!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayStomp_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 2&& translator.Assignable<UnityEngine.Vector3>(L, 1)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    float _duration = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    WoguaShooter.Core.CombatVfx.PlayStomp( _position, _duration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1&& translator.Assignable<UnityEngine.Vector3>(L, 1)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    
                    WoguaShooter.Core.CombatVfx.PlayStomp( _position );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.CombatVfx.PlayStomp!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayGrowthSmash_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 2&& translator.Assignable<UnityEngine.Vector3>(L, 1)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    float _duration = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    WoguaShooter.Core.CombatVfx.PlayGrowthSmash( _position, _duration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1&& translator.Assignable<UnityEngine.Vector3>(L, 1)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    
                    WoguaShooter.Core.CombatVfx.PlayGrowthSmash( _position );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.CombatVfx.PlayGrowthSmash!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayCornExplosion_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 4&& translator.Assignable<UnityEngine.Vector3>(L, 1)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)&& translator.Assignable<UnityEngine.Sprite[]>(L, 4)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    float _radius = (float)LuaAPI.lua_tonumber(L, 2);
                    float _duration = (float)LuaAPI.lua_tonumber(L, 3);
                    UnityEngine.Sprite[] _explosionFrames = (UnityEngine.Sprite[])translator.GetObject(L, 4, typeof(UnityEngine.Sprite[]));
                    
                    WoguaShooter.Core.CombatVfx.PlayCornExplosion( _position, _radius, _duration, _explosionFrames );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 3&& translator.Assignable<UnityEngine.Vector3>(L, 1)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    float _radius = (float)LuaAPI.lua_tonumber(L, 2);
                    float _duration = (float)LuaAPI.lua_tonumber(L, 3);
                    
                    WoguaShooter.Core.CombatVfx.PlayCornExplosion( _position, _radius, _duration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& translator.Assignable<UnityEngine.Vector3>(L, 1)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    UnityEngine.Vector3 _position;translator.Get(L, 1, out _position);
                    float _radius = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    WoguaShooter.Core.CombatVfx.PlayCornExplosion( _position, _radius );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.CombatVfx.PlayCornExplosion!");
            
        }
        
        
        
        
        
        
		
		
		
		
    }
}
