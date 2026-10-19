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
    public class WoguaShooterCoreAudioManagerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.AudioManager);
			Utils.BeginObjectRegister(type, L, translator, 0, 10, 2, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetMusicVolume", _m_SetMusicVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetSfxVolume", _m_SetSfxVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetBgmVolume", _m_SetBgmVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayMusic", _m_PlayMusic);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "StopMusic", _m_StopMusic);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlaySfx", _m_PlaySfx);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayWeaponShot", _m_PlayWeaponShot);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayEnemyHit", _m_PlayEnemyHit);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayCornExplosion", _m_PlayCornExplosion);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayCoinCollect", _m_PlayCoinCollect);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "MusicVolume", _g_get_MusicVolume);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "SfxVolume", _g_get_SfxVolume);
            
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 1, 0);
			
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "Instance", _g_get_Instance);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new WoguaShooter.Core.AudioManager();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.AudioManager constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetMusicVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _value = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetMusicVolume( _value );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetSfxVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _value = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetSfxVolume( _value );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetBgmVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _value = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetBgmVolume( _value );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayMusic(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.AudioClip _clip = (UnityEngine.AudioClip)translator.GetObject(L, 2, typeof(UnityEngine.AudioClip));
                    
                    gen_to_be_invoked.PlayMusic( _clip );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_StopMusic(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.StopMusic(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlaySfx(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& translator.Assignable<UnityEngine.AudioClip>(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    UnityEngine.AudioClip _clip = (UnityEngine.AudioClip)translator.GetObject(L, 2, typeof(UnityEngine.AudioClip));
                    float _volumeScale = (float)LuaAPI.lua_tonumber(L, 3);
                    
                    gen_to_be_invoked.PlaySfx( _clip, _volumeScale );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& translator.Assignable<UnityEngine.AudioClip>(L, 2)) 
                {
                    UnityEngine.AudioClip _clip = (UnityEngine.AudioClip)translator.GetObject(L, 2, typeof(UnityEngine.AudioClip));
                    
                    gen_to_be_invoked.PlaySfx( _clip );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.AudioManager.PlaySfx!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayWeaponShot(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _cornCannon = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.PlayWeaponShot( _cornCannon );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayEnemyHit(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.PlayEnemyHit(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayCornExplosion(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.PlayCornExplosion(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayCoinCollect(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.PlayCoinCollect(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Instance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, WoguaShooter.Core.AudioManager.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MusicVolume(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.MusicVolume);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_SfxVolume(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.AudioManager gen_to_be_invoked = (WoguaShooter.Core.AudioManager)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.SfxVolume);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
